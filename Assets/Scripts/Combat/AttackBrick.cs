using System;
using TopXTop.Character;

namespace TopXTop.Combat
{
    /// <summary>
    /// 전달받은 스킬 위력으로 대상에게 단일 공격을 한 번 적용합니다.
    /// 자동사용 일정·입력·예약·화면·서버 정산은 담당하지 않습니다.
    /// </summary>
    public sealed class AttackBrick
    {
        /// <summary>스킬 위력의 천분율 기준입니다. 1000은 공격력 1배입니다.</summary>
        private const int PowerScale = 1000;

        /// <summary>공격력과 생존 여부를 읽는 공격자 상태 참조입니다.</summary>
        private readonly CharacterState attacker;

        /// <summary>한 번의 공격에 사용할 공격자 상태를 받습니다.</summary>
        /// <param name="attacker">공격력과 생존 상태를 제공하는 공격자입니다.</param>
        public AttackBrick(CharacterState attacker)
        {
            this.attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        }

        /// <summary>
        /// 공격자와 대상이 살아 있는 서로 다른 개체라면 공격을 한 번 적용합니다.
        /// 방어력 때문에 피해가 0이어도 공격은 실행한 것으로 처리합니다.
        /// </summary>
        /// <param name="target">이번 요청에서 공격할 대상입니다. null이면 실행하지 않습니다.</param>
        /// <param name="powerPermille">0부터 int.MaxValue까지의 위력 천분율입니다. 1000은 1배입니다.</param>
        /// <param name="appliedDamage">초과 피해를 제외하고 실제로 감소한 HP입니다. 실패 시 0입니다.</param>
        /// <returns>공격이 실행되었으면 true, 유효하지 않은 대상이나 사망 상태라면 false입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">위력이 음수이면 상태 변경 없이 거절합니다.</exception>
        public bool TryAttack(
            CharacterState target,
            int powerPermille,
            out int appliedDamage)
        {
            appliedDamage = 0;

            if (powerPermille < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(powerPermille));
            }

            if (target == null || ReferenceEquals(attacker, target))
            {
                return false;
            }

            if (!attacker.IsAlive || !target.IsAlive)
            {
                return false;
            }

            // int 수치 두 개의 곱은 long에 들어갑니다. 양수 정수 나눗셈으로 소수점 이하는 버립니다.
            long scaledAttack = (long)attacker.Attack * powerPermille / PowerScale;

            // 방어력을 뺀 뒤 0~int.MaxValue로 제한합니다. HP 상한도 int이므로 초과 피해는 사라집니다.
            int damage = (int)Math.Min(
                int.MaxValue,
                Math.Max(0L, scaledAttack - target.Defense));

            // 실제 감소량을 계산하기 위해 피해 적용 전 HP를 보관합니다.
            int previousHp = target.CurrentHp;
            target.ChangeHp(-damage);
            appliedDamage = previousHp - target.CurrentHp;

            return true;
        }
    }
}
