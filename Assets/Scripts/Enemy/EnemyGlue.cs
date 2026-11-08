using System;
using TopXTop.Character;
using TopXTop.Combat;
using VContainer;
using UnityEngine;

namespace TopXTop.Enemy
{
    /// <summary>종류별 프리팹의 정의를 읽어 대여마다 새 적 상태를 연결합니다.</summary>
    public sealed class EnemyGlue : MonoBehaviour
    {
        /// <summary>이 프리팹 종류의 기본 능력치 원본입니다.</summary>
        [SerializeField]
        private EnemyStatsConfig statsConfig;

        /// <summary>이 적이 지원하는 피격 연출 연결부입니다.</summary>
        [SerializeField]
        private CharacterAnimationGlue animationGlue;

        /// <summary>현재 전투 Scope의 피격 신호를 받는 버스입니다.</summary>
        private CombatHitBus hitBus;

        /// <summary>현재 대여에 연결된 상태입니다. 반환 후에는 null입니다.</summary>
        private EnemyState state;

        /// <summary>프리팹에 미리 지정한 기본 능력치 정의를 제공합니다.</summary>
        public EnemyStatsConfig StatsConfig => statsConfig;

        /// <summary>현재 전투와 공유할 상태입니다. 초기화 전과 반환 후에는 null입니다.</summary>
        public EnemyState State => state;

        /// <summary>풀에서 최초 인스턴스 생성 시 현재 전투의 이벤트 버스를 주입받습니다.</summary>
        /// <param name="bus">전투 Scope가 소유하는 피격 이벤트 버스입니다.</param>
        [Inject]
        private void Construct(CombatHitBus bus)
        {
            hitBus = bus;
        }

        /// <summary>스폰 담당이 활성화한 직후 이번 대여를 Idle부터 표시합니다.</summary>
        public void ShowIdle()
        {
            if (animationGlue != null)
            {
                animationGlue.ResetToIdle();
            }
        }

        /// <summary>지원하는 적에 한해 사망 연출을 시작합니다.</summary>
        public bool TryPlayDeath()
        {
            return animationGlue != null && animationGlue.TryPlayDeath();
        }

        /// <summary>현재 사망 연출을 마쳤는지 확인합니다.</summary>
        public bool IsDeathFinished()
        {
            return animationGlue == null || animationGlue.IsDeathFinished();
        }

        /// <summary>풀 대여 시 정의를 읽어 기본 체력의 새로운 개체 상태를 생성합니다.</summary>
        public void InitializeState()
        {
            if (statsConfig == null)
            {
                throw new InvalidOperationException($"적 '{name}'의 StatsConfig 참조가 필요합니다.");
            }

            BindState(new EnemyState(
                statsConfig.BaseMaxHp,
                statsConfig.BaseAttack,
                statsConfig.BaseDefense));
        }

        /// <summary>외부 전투와 공유할 적 상태를 연결합니다.</summary>
        /// <param name="enemyState">이 개체에 연결할 상태입니다.</param>
        public void BindState(EnemyState enemyState)
        {
            if (enemyState == null)
            {
                throw new ArgumentNullException(nameof(enemyState));
            }

            UnbindState();
            state = enemyState;
            if (animationGlue != null && hitBus != null)
            {
                hitBus.Hit += OnHit;
            }
        }

        /// <summary>겹친 적의 표시 순서를 Unity가 지원하는 정수 범위 안에서 지정합니다.</summary>
        /// <param name="order">현재 활성 목록에서 계산한 표시 순서입니다.</param>
        public void SetDisplayOrder(int order)
        {
            var body = GetComponentInChildren<SpriteRenderer>(true);
            if (body == null)
            {
                throw new InvalidOperationException($"적 '{name}'의 Body SpriteRenderer가 필요합니다.");
            }

            body.sortingOrder = Mathf.Clamp(order, short.MinValue, short.MaxValue);
        }

        /// <summary>이번 대여의 피격 구독과 State 참조를 해제합니다.</summary>
        public void UnbindState()
        {
            if (hitBus != null)
            {
                hitBus.Hit -= OnHit;
            }

            state = null;
        }

        /// <summary>현재 대여에 해당하는 생존 적만 피격 모션을 재생합니다.</summary>
        private void OnHit(EnemyState target, int appliedDamage)
        {
            if (ReferenceEquals(state, target) && target.IsAlive && animationGlue != null)
            {
                animationGlue.PlayHurt();
            }
        }

        /// <summary>씬에서 개체가 먼저 파괴되더라도 현재 대여를 정리합니다.</summary>
        private void OnDestroy()
        {
            UnbindState();
        }
    }
}
