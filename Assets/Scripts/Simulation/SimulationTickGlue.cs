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

        /// <summary>적 이동과 같은 프레임 거리를 받는 배경입니다.</summary>
        private readonly BackgroundScrollGlue background;

        /// <summary>배경과 이동 중인 적이 공유하는 초당 월드 이동 속도입니다.</summary>
        private readonly float moveSpeed;

        /// <summary>새 전방 대상의 달리기와 대시 구간을 결정합니다.</summary>
        private readonly ApproachBrick approach;

        /// <summary>대시 중인 자세를 표시하고 수명을 정리합니다.</summary>
        private readonly DashGhostTrailGlue ghosts;

        /// <summary>접근과 연속공격의 대상인 현재 대여입니다.</summary>
        private EnemyState frontLease;

        /// <summary>이번 대여의 첫 접근 구간입니다.</summary>
        private ApproachBrick.Plan approachPlan;

        /// <summary>새 대상에 접근을 시작한 뒤 흐른 이동 시간입니다.</summary>
        private double approachElapsed;

        /// <summary>현재 대상에 대한 달리기·대시 계획을 계산해야 하는지 표시합니다.</summary>
        private bool approachPending;

        /// <summary>이번 프레임에 대시 잔상을 남길지 표시합니다.</summary>
        private bool sampleDash;

        /// <summary>현재 대여의 예약 타격과 선딜 시작 시각입니다. -1은 미예약입니다.</summary>
        private long strikeAtMs = -1;
        private long windupStartedAtMs = -1;

        /// <summary>최소 한 프레임에 가까운 선딜 시간과 공통 후딜 시간입니다.</summary>
        private const int MinimumWindupMs = 20;
        private readonly int attackRecoveryMs;
        private long recoveryUntilMs;
        private int recoveryStage;

        /// <summary>다음 성공에서 재생할 연속공격 번호입니다.</summary>
        private int nextAttack = 1;

        /// <summary>공격 성공 시 직접 재생 요청을 받을 플레이어 외형입니다.</summary>
        private readonly CharacterAnimationGlue playerAnimation;

        /// <summary>확정된 실제 피해를 방송하는 현재 전투 Scope의 버스입니다.</summary>
        private readonly CombatHitBus hitBus;

        /// <summary>Unity 초 단위 게임 시각을 밀리초로 바꾸는 계수입니다.</summary>
        private const long MillisecondsPerSecond = 1000;

        /// <summary>마지막으로 Animator에 알린 배경 이동 상태입니다.</summary>
        private bool playerMoving;

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
            BackgroundScrollGlue background,
            float moveSpeed,
            ApproachBrick approach = null,
            DashGhostTrailGlue ghosts = null,
            int attackRecoveryMs = 400)
        {
            this.hitBus = hitBus ?? throw new ArgumentNullException(nameof(hitBus));
            this.playerAnimation = playerAnimation;
            this.frontPoint = frontPoint != null ? frontPoint : throw new ArgumentNullException(nameof(frontPoint));
            this.backPoint = backPoint != null ? backPoint : throw new ArgumentNullException(nameof(backPoint));
            this.background = background;
            this.moveSpeed = moveSpeed;
            this.approach = approach;
            this.ghosts = ghosts;
            this.attackRecoveryMs = Math.Max(0, attackRecoveryMs);
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

        /// <summary>공급한 게임 시각으로 이동과 스폰을 진행하고 예약한 기본공격을 최대 한 번 확정합니다.</summary>
        /// <param name="nowMs">timeScale·일시정지 영향을 받는 게임 시각 밀리초입니다. 서버 UTC가 아닙니다.</param>
        /// <param name="deltaSeconds">이번 프레임의 게임 시간 간격입니다.</param>
        public void TickAt(long nowMs, float deltaSeconds = 0f)
        {
            if (disposed || !started || spawner.IsDisposed || spawnPoint == null)
            {
                return;
            }

            bool recovering = UpdateRecovery(nowMs);
            float movementSeconds = Mathf.Clamp(
                (float)((nowMs - (double)recoveryUntilMs) / MillisecondsPerSecond), 0f, deltaSeconds);
            EnemyGlue leading = spawner.GetFirstLivingEnemy();
            SetFrontLease(leading);

            float distance = GetMovementDistance(leading, nowMs, movementSeconds);
            float travelled = spawner.AdvanceMovement(distance, nowMs, deltaSeconds);
            if (travelled > 0f && background != null)
            {
                background.Advance(travelled);
            }

            bool moving = travelled > 0f;
            if (playerMoving != moving)
            {
                playerMoving = moving;
                if (playerAnimation != null)
                {
                    playerAnimation.SetMoving(moving);
                }
            }

            if (spawnSchedule.TryTakeOpportunity(nowMs))
            {
                if (spawner.TrySpawnNext(spawnPoint.position, out EnemyGlue spawned) && leading == null)
                {
                    SetFrontLease(spawned);
                }
            }

            bool compressedWindup = !recovering && AdvanceStrike(nowMs);
            bool leaveGhost = compressedWindup || (sampleDash && travelled > 0f);
            SpriteRenderer ghostSource = leaveGhost && playerAnimation != null ? playerAnimation.Body : null;
            ghosts?.Advance(travelled, deltaSeconds, ghostSource, compressedWindup, travelled <= 0f);
        }

        private bool UpdateRecovery(long nowMs)
        {
            if (recoveryStage == 0)
            {
                return false;
            }

            if (nowMs < recoveryUntilMs)
            {
                float progress = 1f - (recoveryUntilMs - nowMs) / (float)attackRecoveryMs;
                if (playerAnimation != null)
                {
                    playerAnimation.ShowRecovery(recoveryStage, progress);
                }

                return true;
            }

            recoveryStage = 0;
            if (playerAnimation != null)
            {
                playerAnimation.ResetToIdle();
            }

            return false;
        }

        private bool AdvanceStrike(long nowMs)
        {
            EnemyGlue target = spawner.GetCombatTarget();
            int contactMs = CharacterAnimationGlue.GetContactMs(nextAttack);

            if (strikeAtMs < 0 && target != null)
            {
                long earliest = Math.Max(basicAttack.NextUseAtMs, recoveryUntilMs);
                // 첫 정지 공격에는 재생 시간을 줍니다. 이후에는 쿨타임과 후딜이 정한 시각을 사용합니다.
                long desired = basicAttack.NextUseAtMs == 0 ? AddTime(nowMs, contactMs) : earliest;
                ReserveStrike(Math.Max(nowMs, desired));
            }

            if (strikeAtMs < 0)
            {
                return false;
            }

            if (nowMs >= strikeAtMs)
            {
                if (target == null || !ReferenceEquals(target.State, frontLease) ||
                    !basicAttack.CanUse(nowMs, frontLease))
                {
                    CancelStrike();
                    return false;
                }

                CommitStrike(target, nowMs);
                return false;
            }

            long remainingMs = strikeAtMs - nowMs;
            if (windupStartedAtMs < 0)
            {
                if (target == null && (!sampleDash || !approach.CanStartSlash(approachPlan, approachElapsed)))
                {
                    return false;
                }

                if (remainingMs > contactMs || remainingMs < MinimumWindupMs)
                {
                    return false;
                }

                windupStartedAtMs = nowMs;
            }

            long windowMs = strikeAtMs - windupStartedAtMs;
            float progress = 1f - remainingMs / (float)windowMs;
            if (playerAnimation != null)
            {
                playerAnimation.ShowWindup(nextAttack, progress);
            }

            // 한 프레임 늦게 시작한 원속도 선딜에는 잔상을 추가하지 않습니다.
            return windowMs < contactMs - MinimumWindupMs;
        }

        private void ReserveStrike(long atMs)
        {
            if (basicAttack.CanUse(atMs, frontLease))
            {
                strikeAtMs = atMs;
            }
        }

        private void CommitStrike(EnemyGlue target, long nowMs)
        {
            int stage = nextAttack;
            EnemyState state = frontLease;
            target.FinishSpawnFade();
            if (playerAnimation != null)
            {
                playerAnimation.ShowRecovery(stage, 0f);
            }

            if (!basicAttack.TryUse(nowMs, state, out int appliedDamage))
            {
                CancelStrike(true);
                return;
            }

            strikeAtMs = -1;
            windupStartedAtMs = -1;
            nextAttack = stage == 3 ? 1 : stage + 1;
            recoveryStage = stage;
            recoveryUntilMs = AddTime(nowMs, attackRecoveryMs);

            try
            {
                if (appliedDamage > 0)
                {
                    hitBus.Publish(state, appliedDamage);
                }
            }
            finally
            {
                if (!state.IsAlive && spawner.TryBeginDeath(target, nowMs) && spawner.ActiveCount == 0)
                {
                    spawner.TrySpawnNext(spawnPoint.position, out _);
                }
            }
        }

        private void CancelStrike(bool resetPose = false)
        {
            if ((resetPose || windupStartedAtMs >= 0) && playerAnimation != null)
            {
                playerAnimation.ResetToIdle();
            }

            strikeAtMs = -1;
            windupStartedAtMs = -1;
        }

        private void SetFrontLease(EnemyGlue leading)
        {
            EnemyState lease = leading == null ? null : leading.State;
            if (ReferenceEquals(frontLease, lease))
            {
                return;
            }

            CancelStrike();
            frontLease = lease;
            nextAttack = 1;
            approachElapsed = 0d;
            ghosts?.ResetSampling();
            approachPlan = default;
            approachPending = lease != null;
        }

        private float GetMovementDistance(EnemyGlue leading, long nowMs, float seconds)
        {
            sampleDash = false;
            if (seconds <= 0f || (windupStartedAtMs >= 0 && spawner.GetCombatTarget() != null))
            {
                return 0f;
            }

            float remaining = leading == null ? 0f : Mathf.Max(0f, leading.transform.position.x - frontPoint.position.x);
            if (approachPending)
            {
                approachPending = false;
                double cooldownSeconds = (basicAttack.NextUseAtMs - (double)nowMs) /
                    MillisecondsPerSecond + seconds;
                approachPlan = approach == null ? default : approach.Create(remaining, cooldownSeconds);
                if (approachPlan.DashSeconds > 0d)
                {
                    double untilHit = Math.Max(0d, approachPlan.HitSeconds - seconds);
                    long atMs = AddTime(nowMs, (long)Math.Ceiling(untilHit * MillisecondsPerSecond));
                    ReserveStrike(Math.Max(atMs, basicAttack.NextUseAtMs));
                }
            }

            if (remaining > 0f && approachPlan.DashSeconds > 0d && approachElapsed < approachPlan.ArrivalSeconds)
            {
                double before = approachElapsed;
                approachElapsed += seconds;
                sampleDash = approachElapsed > approachPlan.RunSeconds && approachElapsed < approachPlan.ArrivalSeconds;
                if (approachElapsed >= approachPlan.ArrivalSeconds)
                {
                    return remaining;
                }

                return (float)(approach.DistanceAt(approachPlan, approachElapsed) - approach.DistanceAt(approachPlan, before));
            }

            return moveSpeed * seconds;
        }

        private static long AddTime(long nowMs, long durationMs)
        {
            return nowMs > long.MaxValue - durationMs ? long.MaxValue : nowMs + durationMs;
        }

        /// <summary>Scope 종료 시 중앙 처리만 중단합니다. 풀·스폰은 컨테이너가 각각 정리합니다.</summary>
        public void Dispose()
        {
            CancelStrike();
            frontLease = null;
            recoveryStage = 0;
            disposed = true;
        }

        /// <summary>스폰과 공격이 공유하는 Unity 게임 시각을 밀리초로 읽습니다.</summary>
        private static long ReadGameTimeMs()
        {
            return (long)(Time.timeAsDouble * MillisecondsPerSecond);
        }
    }
}
