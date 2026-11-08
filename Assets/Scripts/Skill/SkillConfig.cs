using UnityEngine;

namespace TopXTop.Skill
{
    /// <summary>
    /// 기본공격과 강타처럼 같은 공격 효과에서 사용하는 스킬별 고정 정의입니다.
    /// 사용 시각·남은 쿨타임·사용자별 레벨은 저장하지 않습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillConfig",
        menuName = "TopXTop/Skill/스킬 정의")]
    public sealed class SkillConfig : ScriptableObject
    {
        /// <summary>표시명이 바뀌어도 유지하는 안정적인 스킬 식별자입니다.</summary>
        [SerializeField]
        [Tooltip("같은 스킬의 ID는 유지합니다. 공격 효과를 이름별로 분기하지 않습니다.")]
        private string skillId = "basic_attack";

        /// <summary>화면에서 사용할 스킬 표시명입니다.</summary>
        [SerializeField]
        private string displayName = "기본공격";

        /// <summary>스킬 기본 쿨타임 정의입니다. 단위는 밀리초이며 최소 1ms입니다.</summary>
        [SerializeField, Min(1)]
        [Tooltip("1000ms는 1초입니다. 사용 시점 검사는 SkillUseBrick이 맡습니다.")]
        private int baseCooldownMs = 1000;

        /// <summary>공격 위력의 천분율입니다. 범위는 0~int.MaxValue, 1000은 1배입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("1000=1배, 2000=2배입니다. 공격력에 적용한 뒤 소수점 이하를 버리고 방어력을 뺍니다.")]
        private int powerPermille = 1000;

        /// <summary>스킬의 안정 식별자를 제공합니다.</summary>
        public string SkillId => skillId;

        /// <summary>스킬 표시명을 제공합니다.</summary>
        public string DisplayName => displayName;

        /// <summary>기본 쿨타임 정의를 밀리초로 제공합니다. 실제 쿨타임 실행 상태가 아닙니다.</summary>
        public int BaseCooldownMs => baseCooldownMs;

        /// <summary>1회 공격에 전달할 위력 천분율을 제공합니다.</summary>
        public int PowerPermille => powerPermille;
    }
}
