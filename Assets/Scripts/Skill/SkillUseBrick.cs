using System;
using TopXTop.Character;
using TopXTop.Combat;

namespace TopXTop.Skill
{
    /// <summary>
    /// 한 캐릭터의 한 스킬에 대한 사용 검사와 다음 사용 시각을 소유합니다.
    /// 요청마다 받은 대상을 공격하며, 대상 교체와 무관하게 쿨타임을 유지합니다.
    /// </summary>
    public sealed class SkillUseBrick
    {
        /// <summary>공격자의 현재 능력치로 피해를 한 번 적용하는 효과입니다.</summary>
        private readonly AttackBrick attack;

        /// <summary>성공 후 기다릴 스킬별 쿨타임이며 단위는 밀리초입니다.</summary>
        private readonly int cooldownMs;

        /// <summary>SO에서 읽어 전달한 위력 천분율입니다. 1000은 1배입니다.</summary>
        private readonly int powerPermille;

        /// <summary>다음 사용 가능 시각입니다. 요청과 같은 기준의 밀리초이며 최초 값은 0입니다.</summary>
        public long NextUseAtMs { get; private set; }

        /// <summary>스킬별 고정 수치와 공격 효과를 받습니다. 같은 SO도 실행 객체는 별개입니다.</summary>
        /// <param name="attack">현재 공격자 상태를 읽는 1회 공격 효과입니다.</param>
        /// <param name="cooldownMs">1부터 int.MaxValue까지의 쿨타임 밀리초입니다.</param>
        /// <param name="powerPermille">0부터 int.MaxValue까지의 위력 천분율입니다.</param>
        public SkillUseBrick(
            AttackBrick attack,
            int cooldownMs,
            int powerPermille)
        {
            if (cooldownMs < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownMs));
            }

            if (powerPermille < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(powerPermille));
            }

            this.attack = attack ?? throw new ArgumentNullException(nameof(attack));
            this.cooldownMs = cooldownMs;
            this.powerPermille = powerPermille;
        }

        /// <summary>피해와 쿨타임을 바꾸지 않고 예약 시각의 스킬 사용 가능 여부를 확인합니다.</summary>
        public bool CanUse(long atMs, CharacterState target)
        {
            return atMs >= NextUseAtMs && atMs <= long.MaxValue - cooldownMs && attack.CanAttack(target);
        }

        /// <summary>피해량이 필요 없는 호출자에게 스킬 실행 성공 여부를 제공합니다.</summary>
        /// <param name="nowMs">호출자가 공급한 게임 시각입니다.</param>
        /// <param name="target">이번 공격 대상입니다.</param>
        public bool TryUse(long nowMs, CharacterState target)
        {
            return TryUse(nowMs, target, out _);
        }

        /// <summary>
        /// 요청 시각에 사용 가능하면 최대 한 번 공격합니다. 첫 유효 요청은 즉시 성공할 수 있습니다.
        /// 0피해 성공도 쿨타임을 소비하며, 대상·생존·쿨타임 실패는 다음 시각을 바꾸지 않습니다.
        /// </summary>
        /// <param name="nowMs">Unity가 아닌 호출 측에서 공급한 0 이상의 정수 밀리초 시각입니다.</param>
        /// <param name="target">현재 요청의 대상입니다. null·사망 대상은 공격하지 않습니다.</param>
        /// <returns>공격 효과가 한 번 성공했으면 true, 사용할 수 없으면 false입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">음수이거나 다음 시각이 long 범위를 넘는 요청입니다.</exception>
        /// <param name="appliedDamage">실제 체력 감소량입니다. 실패 또는 피해 없는 성공이면 0입니다.</param>
        public bool TryUse(long nowMs, CharacterState target, out int appliedDamage)
        {
            appliedDamage = 0;
            if (nowMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nowMs));
            }

            if (nowMs < NextUseAtMs)
            {
                return false;
            }

            if (nowMs > long.MaxValue - cooldownMs)
            {
                throw new ArgumentOutOfRangeException(nameof(nowMs));
            }

            if (!attack.TryAttack(target, powerPermille, out appliedDamage))
            {
                return false;
            }

            // 실제 성공한 시각에서 다음 시각을 잡습니다. 밀린 공격을 반복 실행하지 않습니다.
            NextUseAtMs = nowMs + cooldownMs;
            return true;
        }
    }
}
