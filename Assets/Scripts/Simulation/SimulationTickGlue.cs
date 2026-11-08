using System;
using TopXTop.Character;
using TopXTop.Combat;
using TopXTop.Difficulty;
using TopXTop.Enemy;
using TopXTop.Skill;
using UnityEngine;
using VContainer.Unity;

namespace TopXTop.Simulation
{
    /// <summary>하나의 게임 시각으로 스폰 기회·대상 선택·공격·사망 반환을 순서대로 연결합니다.</summary>
    public sealed class SimulationTickGlue : IStartable, ITickable, IDisposable
    {
        /// <summary>대상이 바뀌어도 유지할 플레이어 기본공격 실행 상태입니다.</summary>
        private readonly SkillUseBrick basicAttack;

        /// <summary>등장 순서·상한·반환을 관리하는 공통 스폰 담당입니다.</summary>
        private readonly EnemySpawnGlue spawner;

        /// <summary>포화 여부와 무관하게 1초마다 한 번만 기회를 주는 순수 시간 판정입니다.</summary>
        private readonly SpawnScheduleBrick spawnSchedule;

        /// <summary>이 Scope 시작 시 적용할 난이도 정의입니다.</summary>
        private readonly DifficultyConfig startingDifficulty;

        /// <summary>적을 등장시킬 씬의 우측 지점입니다.</summary>
        private readonly Transform spawnPoint;

        /// <summary>스폰과 분리된 0번 전투 위치입니다.</summary>
        private readonly Transform frontPoint;

        /// <summary>난이도 전체 상한으로 칸 간격을 구할 뒤쪽 경계입니다.</summary>
        private readonly Transform backPoint;

        /// <summary>적이 사망 모션을 표시할 독립 위치입니다.</summary>
        private readonly Transform deathPoint;

        /// <summary>적 이동과 같은 프레임 거리를 받는 배경입니다.</summary>
        private readonly BackgroundScrollGlue background;

        /// <summary>배경과 이동 중인 적이 공유하는 초당 월드 이동 속도입니다.</summary>
        private readonly float moveSpeed;

        /// <summary>공격 성공 시 직접 재생 요청을 받을 플레이어 외형입니다.</summary>
        private readonly CharacterAnimationGlue playerAnimation;

        /// <summary>확정된 실제 피해를 방송하는 현재 전투 Scope의 버스입니다.</summary>
        private readonly CombatHitBus hitBus;

        /// <summary>Unity 초 단위 게임 시각을 밀리초로 바꾸는 계수입니다.</summary>
        private const long MillisecondsPerSecond = 1000;

        /// <summary>시작 중복과 시작 전 Tick을 방지하는 상태입니다.</summary>
        private bool started;

        /// <summary>Scope 종료 후 호출을 차단하는 상태입니다.</summary>
        private bool disposed;

        /// <summary>Root에서 조립한 서비스와 Inspector 참조를 받습니다.</summary>
        /// <param name="basicAttack">동일 캐릭터가 유지할 스킬 실행 객체입니다.</param>
        /// <param name="spawner">활성 적과 풀 반환을 관리하는 서비스입니다.</param>
        /// <param name="spawnSchedule">스폰 시도의 시간 간격을 판정하는 객체입니다.</param>
        /// <param name="startingDifficulty">시작 난이도 에셋입니다.</param>
        /// <param name="spawnPoint">등장 위치를 제공하는 씬 참조입니다.</param>
        /// <param name="hitBus">확정 피해 이벤트 버스입니다.</param>
        /// <param name="playerAnimation">플레이어 공격 모션을 재생하는 씬 참조입니다.</param>
        public SimulationTickGlue(
            SkillUseBrick basicAttack,
            EnemySpawnGlue spawner,
            SpawnScheduleBrick spawnSchedule,
            DifficultyConfig startingDifficulty,
            Transform spawnPoint,
            CombatHitBus hitBus,
            CharacterAnimationGlue playerAnimation,
            Transform frontPoint,
            Transform backPoint,
            Transform deathPoint,
            BackgroundScrollGlue background,
            float moveSpeed)
        {
            this.hitBus = hitBus ?? throw new ArgumentNullException(nameof(hitBus));
            this.playerAnimation = playerAnimation;
            this.frontPoint = frontPoint != null ? frontPoint : throw new ArgumentNullException(nameof(frontPoint));
            this.backPoint = backPoint != null ? backPoint : throw new ArgumentNullException(nameof(backPoint));
            this.deathPoint = deathPoint != null ? deathPoint : throw new ArgumentNullException(nameof(deathPoint));
            this.background = background;
            this.moveSpeed = moveSpeed;
            this.basicAttack = basicAttack ?? throw new ArgumentNullException(nameof(basicAttack));
            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            this.spawnSchedule = spawnSchedule ?? throw new ArgumentNullException(nameof(spawnSchedule));
            this.startingDifficulty = startingDifficulty != null
                ? startingDifficulty
                : throw new ArgumentNullException(nameof(startingDifficulty));
            this.spawnPoint = spawnPoint != null
                ? spawnPoint
                : throw new ArgumentNullException(nameof(spawnPoint));
        }

        /// <summary>VContainer 시작 시 난이도를 설정하고 즉시 한 번의 스폰·공격 기회를 처리합니다.</summary>
        public void Start()
        {
            if (disposed || started)
            {
                return;
            }

            spawner.Configure(startingDifficulty, frontPoint.position.x, backPoint.position.x);
            started = true;
            TickAt(ReadGameTimeMs());
        }

        /// <summary>중앙 PlayerLoop에서 동일한 게임 시각을 읽어 프레임 처리를 요청합니다.</summary>
        public void Tick()
        {
            TickAt(ReadGameTimeMs(), Time.deltaTime);
        }

        /// <summary>공급한 게임 시각으로 스폰→대상 선택→기본공격→사망 반환을 한 번만 처리합니다.</summary>
        /// <param name="nowMs">timeScale·일시정지 영향을 받는 게임 시각 밀리초입니다. 서버 UTC가 아닙니다.</param>
        /// <param name="deltaSeconds">이번 프레임의 게임 시간 간격입니다.</param>
        public void TickAt(long nowMs, float deltaSeconds = 0f)
        {
            if (disposed || !started || spawner.IsDisposed || spawnPoint == null)
            {
                return;
            }

            float distance = moveSpeed * deltaSeconds;
            float travelled = spawner.AdvanceMovement(distance, nowMs);
            if (background != null && travelled > 0f)
            {
                background.Advance(travelled);
            }

            if (spawnSchedule.TryTakeOpportunity(nowMs))
            {
                spawner.TrySpawnNext(spawnPoint.position, out _);
            }

            EnemyGlue target = spawner.GetCombatTarget();
            if (target == null)
            {
                return;
            }

            // 스킬은 대상을 보관하지 않습니다. 반환 후 다음 프레임은 목록에서 새로 선택합니다.
            EnemyState state = target.State;
            if (!basicAttack.TryUse(nowMs, state, out int appliedDamage))
            {
                return;
            }

            // HP와 쿨타임이 확정된 뒤에만 연출을 요청합니다.
            try
            {
                if (playerAnimation != null)
                {
                    playerAnimation.PlayAttack();
                }

                if (appliedDamage > 0)
                {
                    hitBus.Publish(state, appliedDamage);
                }
            }
            finally
            {
                if (!state.IsAlive && spawner.TryBeginDeath(target, deathPoint.position, nowMs))
                {
                    spawner.TrySpawnNext(spawnPoint.position, out _);
                }
            }
        }

        /// <summary>Scope 종료 시 중앙 처리만 중단합니다. 풀·스폰은 컨테이너가 각각 정리합니다.</summary>
        public void Dispose()
        {
            disposed = true;
        }

        /// <summary>스폰과 공격이 공유하는 Unity 게임 시각을 밀리초로 읽습니다.</summary>
        private static long ReadGameTimeMs()
        {
            return (long)(Time.timeAsDouble * MillisecondsPerSecond);
        }
    }
}
