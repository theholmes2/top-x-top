using TopXTop.Character;

namespace TopXTop.Player
{
    /// <summary>
    /// 공통 능력치를 상속하고 플레이어 전용 힘·민첩·체력·재생 설정을 보관합니다.
    /// 현재 상태의 증감과 성장에 따른 능력치 계산은 구분합니다.
    /// </summary>
    public sealed class PlayerState : CharacterState
    {
        /// <summary>현재 힘입니다. 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int Strength { get; private set; }

        /// <summary>현재 민첩입니다. 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int Dexterity { get; private set; }

        /// <summary>현재 체력 스탯입니다. 최대 HP와 별개이며 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int Vitality { get; private set; }

        /// <summary>체력 재생 한 번의 회복량입니다. 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int RegenerationAmount { get; private set; }

        /// <summary>체력 재생 간격입니다. 단위는 밀리초이며 범위는 1부터 int.MaxValue까지입니다.</summary>
        public int RegenerationIntervalMs { get; private set; }

        /// <summary>
        /// SO에서 읽은 기본 수치를 받아 공통 능력치는 부모가, 전용 수치는 자식이 초기화합니다.
        /// Unity 객체를 읽는 일은 이후 작성할 Glue가 담당합니다.
        /// </summary>
        public PlayerState(
            int maxHp,
            int attack,
            int defense,
            int strength,
            int dexterity,
            int vitality,
            int regenerationAmount,
            int regenerationIntervalMs)
            : base(maxHp, attack, defense)
        {
            Strength = ClampStat(strength);
            Dexterity = ClampStat(dexterity);
            Vitality = ClampStat(vitality);
            RegenerationAmount = ClampStat(regenerationAmount);
            RegenerationIntervalMs = ClampStat(regenerationIntervalMs, 1);

            // TODO: 서버 성장 복원은 이후 검토할 별도 단계입니다.
            // 기본값으로 초기화한 상태에 서버가 확정한 성장·장비 정보를 한 번 적용합니다.
            // 이미 성장한 현재 값에 같은 서버 누적을 반복해서 더하지 않습니다.
            // 서버 조회 실패와 신규 데이터 부재를 구분하고 실패 시 기본값을 서버에 저장하지 않습니다.
            // 현재 HP까지 복원할지는 별도 정책으로 결정하며 여기서는 최대 HP로 시작합니다.
        }

        /// <summary>힘을 증감합니다. 공격력 파생 계산은 이후 성장 규칙에서 다룹니다.</summary>
        public void ChangeStrength(int amount)
        {
            Strength = ClampStat((long)Strength + amount);
        }

        /// <summary>민첩을 증감합니다. 공격 간격 계산은 이후 스킬 규칙에서 다룹니다.</summary>
        public void ChangeDexterity(int amount)
        {
            Dexterity = ClampStat((long)Dexterity + amount);
        }

        /// <summary>체력 스탯을 증감합니다. 최대 HP 파생 계산은 이후 성장 규칙에서 다룹니다.</summary>
        public void ChangeVitality(int amount)
        {
            Vitality = ClampStat((long)Vitality + amount);
        }

        /// <summary>체력 재생량을 증감합니다. 실제 회복 실행은 하지 않습니다.</summary>
        public void ChangeRegenerationAmount(int amount)
        {
            RegenerationAmount = ClampStat((long)RegenerationAmount + amount);
        }

        /// <summary>체력 재생 간격을 밀리초 단위로 증감하며 최소 1ms로 제한합니다.</summary>
        public void ChangeRegenerationIntervalMs(int amount)
        {
            RegenerationIntervalMs = ClampStat((long)RegenerationIntervalMs + amount, 1);
        }
    }
}
