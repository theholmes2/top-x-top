using System;
using R3;
using TopXTop.Character;

namespace TopXTop.Enemy
{
    /// <summary>대여 중인 상태의 체력 한 쌍을 R3로 전달하는 Unity 비의존 ViewModel입니다.</summary>
    public sealed class EnemyHealthViewModel : IDisposable
    {
        /// <summary>현재 대여의 상태 원본입니다.</summary>
        private readonly CharacterState state;

        /// <summary>초기 체력과 이후 확정된 체력을 구독자에게 제공합니다.</summary>
        private readonly ReactiveProperty<(int Current, int Max)> hp;

        /// <summary>현재·최대 체력의 읽기 전용 스트림입니다.</summary>
        public Observable<(int Current, int Max)> Values => hp;

        /// <summary>새 대여 상태의 초기 체력을 읽고 변경 통지를 연결합니다.</summary>
        public EnemyHealthViewModel(CharacterState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            hp = new ReactiveProperty<(int Current, int Max)>((state.CurrentHp, state.MaxHp));
            state.HpChanged += OnHpChanged;
        }

        /// <summary>상태가 확정한 체력 한 쌍을 발행합니다.</summary>
        private void OnHpChanged(int current, int max)
        {
            hp.Value = (current, max);
        }

        /// <summary>대여 종료에 따라 상태와 R3 스트림을 정리합니다.</summary>
        public void Dispose()
        {
            state.HpChanged -= OnHpChanged;
            hp.Dispose();
        }
    }
}
