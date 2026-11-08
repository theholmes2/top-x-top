using UnityEngine;

namespace TopXTop.Player
{
    /// <summary>
    /// 플레이어 생성 시 사용할 기본 능력치입니다.
    /// 현재 체력이나 플레이 중 성장 결과를 저장하지 않습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerStatsConfig",
        menuName = "TopXTop/Player/기본 능력치")]
    public sealed class PlayerStatsConfig : ScriptableObject
    {
        /// <summary>성장과 장비 보너스를 적용하기 전의 기본 최대 체력입니다.</summary>
        [SerializeField, Min(1)]
        [Tooltip("성장과 장비 보너스를 적용하기 전의 최대 HP입니다.")]
        private int baseMaxHp = 60;

        /// <summary>성장과 장비 보너스를 적용하기 전의 기본 공격력입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("성장과 장비 보너스를 적용하기 전의 공격력입니다.")]
        private int baseAttack = 8;

        /// <summary>성장과 장비 보너스를 적용하기 전의 기본 방어력입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("성장과 장비 보너스를 적용하기 전의 방어력입니다.")]
        private int baseDefense = 0;

        /// <summary>플레이어가 처음 시작할 때의 기본 힘입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("스탯 포인트를 투자하기 전의 힘입니다.")]
        private int baseStrength = 0;

        /// <summary>플레이어가 처음 시작할 때의 기본 민첩입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("스탯 포인트를 투자하기 전의 민첩입니다.")]
        private int baseDexterity = 0;

        /// <summary>플레이어가 처음 시작할 때의 기본 체력 스탯입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("최대 HP와 구분되는 체력 스탯의 초기값입니다.")]
        private int baseVitality = 0;

        /// <summary>체력 재생 한 번에 회복할 기본 HP 양입니다.</summary>
        [SerializeField, Min(0)]
        [Tooltip("기존 프로젝트의 테스트 수치이며 이후 밸런스를 조절합니다.")]
        private int baseRegenerationAmount = 100;

        /// <summary>기본 체력 재생이 발생하는 간격이며 단위는 밀리초입니다.</summary>
        [SerializeField, Min(1)]
        [Tooltip("500ms는 0.5초입니다. 공격 주기와는 별개입니다.")]
        private int regenerationIntervalMs = 500;

        /// <summary>기본 최대 체력을 제공합니다.</summary>
        public int BaseMaxHp => baseMaxHp;

        /// <summary>기본 공격력을 제공합니다.</summary>
        public int BaseAttack => baseAttack;

        /// <summary>기본 방어력을 제공합니다.</summary>
        public int BaseDefense => baseDefense;

        /// <summary>기본 힘을 제공합니다.</summary>
        public int BaseStrength => baseStrength;

        /// <summary>기본 민첩을 제공합니다.</summary>
        public int BaseDexterity => baseDexterity;

        /// <summary>기본 체력 스탯을 제공합니다.</summary>
        public int BaseVitality => baseVitality;

        /// <summary>체력 재생 한 번의 기본 회복량을 제공합니다.</summary>
        public int BaseRegenerationAmount => baseRegenerationAmount;

        /// <summary>기본 체력 재생 간격을 밀리초로 제공합니다.</summary>
        public int RegenerationIntervalMs => regenerationIntervalMs;
    }
}
