using UnityEngine;

namespace TopXTop.Enemy
{
    /// <summary>
    /// 적 종류별 기본 능력치입니다.
    /// 현재 체력, 처치 보상, 행동 규칙은 저장하지 않습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "EnemyStatsConfig",
        menuName = "TopXTop/Enemy/기본 능력치")]
    public sealed class EnemyStatsConfig : ScriptableObject
    {
        /// <summary>적이 생성될 때 사용할 기본 최대 체력입니다.</summary>
        [SerializeField, Min(1)]
        [Tooltip("적 종류의 기본 최대 HP입니다. 현재 HP와 구분합니다.")]
        private int baseMaxHp = 45;

        /// <summary>적 종류의 기본 공격력입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("공격 행동이 사용할 기본 공격력입니다.")]
        private int baseAttack = 7;

        /// <summary>적 종류의 기본 방어력입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("피해 계산이 사용할 기본 방어력입니다.")]
        private int baseDefense = 0;

        /// <summary>적의 기본 최대 체력을 제공합니다.</summary>
        public int BaseMaxHp => baseMaxHp;

        /// <summary>적의 기본 공격력을 제공합니다.</summary>
        public int BaseAttack => baseAttack;

        /// <summary>적의 기본 방어력을 제공합니다.</summary>
        public int BaseDefense => baseDefense;
    }
}
