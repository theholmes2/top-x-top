using TopXTop.Character;

namespace TopXTop.Enemy
{
    /// <summary>
    /// 적 개체 하나의 현재 능력치입니다. 공통 상태와 증감 규칙은 부모에서 상속합니다.
    /// 현재는 적 전용 추가 수치가 없으며, 보상·특수행동은 이 상태에 넣지 않습니다.
    /// </summary>
    public sealed class EnemyState : CharacterState
    {
        /// <summary>적 SO에서 읽은 최대 HP·공격력·방어력으로 초기화합니다.</summary>
        public EnemyState(int maxHp, int attack, int defense)
            : base(maxHp, attack, defense)
        {
        }
    }
}
