using System;

namespace TopXTop.Character
{
    /// <summary>
    /// 플레이어와 적이 함께 사용하는 현재 능력치와 증감 규칙입니다.
    /// 각 자식 인스턴스는 서로 독립적인 상태를 소유합니다.
    /// </summary>
    public abstract class CharacterState
    {
        /// <summary>현재 HP입니다. 범위는 0부터 MaxHp까지입니다.</summary>
        public int CurrentHp { get; private set; }

        /// <summary>최대 HP입니다. 범위는 1부터 int.MaxValue까지입니다.</summary>
        public int MaxHp { get; private set; }

        /// <summary>현재 공격력입니다. 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int Attack { get; private set; }

        /// <summary>현재 방어력입니다. 범위는 0부터 int.MaxValue까지입니다.</summary>
        public int Defense { get; private set; }

        /// <summary>현재 HP가 남아 있는지 제공합니다. 별도 생존 값을 중복 저장하지 않습니다.</summary>
        public bool IsAlive => CurrentHp > 0;

        /// <summary>자식이 전달한 공통 기본 능력치를 적용하고 최대 체력으로 시작합니다.</summary>
        protected CharacterState(int maxHp, int attack, int defense)
        {
            MaxHp = ClampStat(maxHp, 1);
            CurrentHp = MaxHp;
            Attack = ClampStat(attack);
            Defense = ClampStat(defense);
        }

        /// <summary>현재 HP를 증감합니다. 양수는 회복, 음수는 감소이며 0~MaxHp로 제한합니다.</summary>
        public void ChangeHp(int amount)
        {
            CurrentHp = (int)Math.Max(0L, Math.Min(MaxHp, (long)CurrentHp + amount));
        }

        /// <summary>
        /// 최대 HP를 증감합니다. 증가 시 실제 증가량만큼 현재 HP도 올립니다.
        /// 감소 시 현재 HP는 새 최대 HP를 초과하는 경우에만 낮춥니다.
        /// </summary>
        public void ChangeMaxHp(int amount)
        {
            int previousMaxHp = MaxHp;
            MaxHp = ClampStat((long)MaxHp + amount, 1);

            if (MaxHp > previousMaxHp)
            {
                ChangeHp(MaxHp - previousMaxHp);
            }
            else
            {
                CurrentHp = Math.Min(CurrentHp, MaxHp);
            }
        }

        /// <summary>공격력을 증감합니다. 음수 amount로 감소시키며 유효 범위로 제한합니다.</summary>
        public void ChangeAttack(int amount)
        {
            Attack = ClampStat((long)Attack + amount);
        }

        /// <summary>방어력을 증감합니다. 음수 amount로 감소시키며 유효 범위로 제한합니다.</summary>
        public void ChangeDefense(int amount)
        {
            Defense = ClampStat((long)Defense + amount);
        }

        /// <summary>부모와 자식의 수치를 최소값부터 int 표현 상한까지 제한합니다.</summary>
        protected static int ClampStat(long value, int minimum = 0)
        {
            // 확정된 밸런스 상한이 없으므로 현재는 int 표현 범위를 사용합니다.
            // 합산은 long으로 수행하여 제한 전에 int 오버플로가 발생하지 않게 합니다.
            return (int)Math.Max(minimum, Math.Min(int.MaxValue, value));
        }
    }
}
