using System;
using TopXTop.Character;
using TopXTop.Combat;
using TopXTop.Difficulty;
using TopXTop.Enemy;
using TopXTop.Player;
using TopXTop.Skill;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TopXTop.Simulation
{
    /// <summary>현재 한 게임 Scope에서 플레이어·기본공격·풀·스폰·중앙 Tick을 조립합니다.</summary>
    public sealed class CombatCompositionRoot : LifetimeScope
    {
        /// <summary>플레이어 생성에 사용할 기본 능력치 정의입니다.</summary>
        [SerializeField]
        private PlayerStatsConfig playerConfig;

        /// <summary>게임을 시작할 난이도와 등장 프리팹·상한 정의입니다.</summary>
        [SerializeField]
        private DifficultyConfig startingDifficulty;

        /// <summary>현재 기본공격의 위력과 쿨타임 고정 정의입니다.</summary>
        [SerializeField]
        private SkillConfig basicAttackConfig;

        /// <summary>적들이 겹쳐서 등장할 씬의 우측 지점입니다.</summary>
        [SerializeField]
        private Transform enemySpawnPoint;

        /// <summary>0번 전투 자리를 표시하는 씬 위치입니다.</summary>
        [SerializeField]
        private Transform frontPoint;

        /// <summary>FIFO 줄의 뒤쪽 경계를 표시하는 씬 위치입니다.</summary>
        [SerializeField]
        private Transform backPoint;

        /// <summary>숲 반복 구간을 이동시키는 씬 연결부입니다.</summary>
        [SerializeField]
        private BackgroundScrollGlue background;

        /// <summary>배경과 몬스터가 공유하는 초당 월드 이동 거리입니다.</summary>
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 0.9f;

        /// <summary>대시 중 배경과 적이 공유하는 임시 속도입니다.</summary>
        [SerializeField, Min(0.01f)]
        private float dashSpeed = 4f;

        /// <summary>대시를 표시할 수 있는 최소·최대 이동 거리입니다.</summary>
        [SerializeField]
        private Vector2 dashDistance = new Vector2(0.24f, 0.9f);

        /// <summary>대시 끝에 겹칠 베기 연출의 최소·최대 게임 시간입니다.</summary>
        [SerializeField]
        private Vector2 slashSeconds = new Vector2(0.045f, 0.12f);

        /// <summary>모든 기본공격의 접촉 직후 이동과 다음 공격을 막는 게임 시간입니다.</summary>
        [SerializeField, Min(0)]
        private int attackRecoveryMs = 400;

        /// <summary>씬에서 공격 성공 연출을 받을 플레이어 외형입니다.</summary>
        [SerializeField]
        private CharacterAnimationGlue playerAnimation;

        /// <summary>스폰 보충 시도 간격이며 게임 시각 기준 1초입니다.</summary>
        private const int SpawnIntervalMs = 1000;

        /// <summary>Inspector 참조를 검증하고 컨테이너가 서비스 생성·수명을 소유하도록 등록합니다.</summary>
        /// <param name="builder">현재 게임 Scope의 의존성을 조립할 빌더입니다.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            if (!playerConfig || !startingDifficulty || !basicAttackConfig || !enemySpawnPoint)
            {
                throw new InvalidOperationException("Player/시작 난이도/기본공격 Config와 적 스폰 위치 참조가 필요합니다.");
            }

            if (!frontPoint || !backPoint || !background ||
                backPoint.position.x < frontPoint.position.x || enemySpawnPoint.position.x <= backPoint.position.x ||
                !(moveSpeed > 0f) || float.IsInfinity(moveSpeed))
            {
                throw new InvalidOperationException("전투 앞칸·뒤쪽 경계·우측 스폰 지점·배경과 양수 이동 속도를 지정해야 합니다.");
            }

            if (!(dashSpeed > moveSpeed) || float.IsInfinity(dashSpeed) ||
                !(dashDistance.x > 0f && dashDistance.y >= dashDistance.x) ||
                !(slashSeconds.x > 0f && slashSeconds.y >= slashSeconds.x))
            {
                throw new InvalidOperationException("대시 속도와 최소·최대 거리, 베기 시간을 확인해야 합니다.");
            }

            if (!startingDifficulty.TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            var player = new PlayerState(
                playerConfig.BaseMaxHp,
                playerConfig.BaseAttack,
                playerConfig.BaseDefense,
                playerConfig.BaseStrength,
                playerConfig.BaseDexterity,
                playerConfig.BaseVitality,
                playerConfig.BaseRegenerationAmount,
                playerConfig.RegenerationIntervalMs);

            var basicAttack = new SkillUseBrick(
                new AttackBrick(player),
                basicAttackConfig.BaseCooldownMs,
                basicAttackConfig.PowerPermille);

            var approach = new ApproachBrick(moveSpeed, dashSpeed, dashDistance.x, dashDistance.y,
                slashSeconds.x, slashSeconds.y);

            builder.RegisterInstance(player);
            builder.RegisterInstance(approach);
            builder.Register<DashGhostTrailGlue>(Lifetime.Singleton);
            builder.RegisterInstance(basicAttack);

            // Singleton은 이 컨테이너 안에서 공유하며 실제 생성과 IDisposable 추적도 컨테이너가 맡습니다.
            builder.Register<CombatHitBus>(Lifetime.Singleton);
            builder.Register<EnemyPoolGlue>(Lifetime.Singleton);
            builder.Register<EnemySpawnGlue>(Lifetime.Singleton);
            builder.Register<SpawnScheduleBrick>(Lifetime.Singleton)
                .WithParameter(SpawnIntervalMs);

            // 에셋·씬 참조는 Inspector에서만 공급하고 서비스는 생성자 DI로 받습니다.
            builder.RegisterEntryPoint<SimulationTickGlue>(Lifetime.Scoped)
                .WithParameter(startingDifficulty)
                .WithParameter("spawnPoint", enemySpawnPoint)
                .WithParameter("frontPoint", frontPoint)
                .WithParameter("backPoint", backPoint)
                .WithParameter(background)
                .WithParameter(moveSpeed)
                .WithParameter("attackRecoveryMs", attackRecoveryMs)
                .WithParameter<CharacterAnimationGlue>(playerAnimation);
        }
    }
}
