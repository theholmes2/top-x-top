using System;
using TopXTop.Enemy;

namespace TopXTop.Combat
{
    /// <summary>현재 전투 Scope에서 확정된 양수 피해를 소비자에게 동기 전달합니다.</summary>
    public sealed class CombatHitBus : IDisposable
    {
        /// <summary>피격된 대여의 State와 실제 감소한 체력을 전달합니다.</summary>
        public event Action<EnemyState, int> Hit;

        /// <summary>양수 피해가 확정됐을 때 한 번 발행합니다.</summary>
        /// <param name="target">피격 당시의 대여 State입니다.</param>
        /// <param name="appliedDamage">실제로 감소한 체력입니다.</param>
        public void Publish(EnemyState target, int appliedDamage)
        {
            if (target != null && appliedDamage > 0)
            {
                Hit?.Invoke(target, appliedDamage);
            }
        }

        /// <summary>전투 Scope가 끝날 때 소비자 참조를 해제합니다.</summary>
        public void Dispose()
        {
            Hit = null;
        }
    }
}
