using UnityEngine;

namespace TopXTop.Character
{
    /// <summary>확정된 전투 결과에 따라 SpriteRenderer를 구동하는 Animator에 재생을 요청합니다.</summary>
    public sealed class CharacterAnimationGlue : MonoBehaviour
    {
        /// <summary>Inspector에서 Controller와 연결한 Animator입니다.</summary>
        [SerializeField]
        private Animator animator;

        /// <summary>대여를 시작할 때 표시할 기본 상태입니다.</summary>
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

        /// <summary>플레이어 공격 연출 상태입니다.</summary>
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");

        /// <summary>적 피격 연출 상태입니다.</summary>
        private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");

        /// <summary>사망 연출을 끝까지 표시할 비루프 상태입니다.</summary>
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

        /// <summary>사망 모션을 시작하고 첫 프레임을 평가합니다.</summary>
        public bool TryPlayDeath()
        {
            if (!Application.isPlaying || !PlayFromStart(DeathState))
            {
                return false;
            }

            animator.Update(0f);
            return true;
        }

        /// <summary>사망 모션이 끝났거나 더 이상 재생할 수 없는지 확인합니다.</summary>
        public bool IsDeathFinished()
        {
            if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized ||
                animator.runtimeAnimatorController == null)
            {
                return true;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.fullPathHash != DeathState || state.normalizedTime >= 1f;
        }

        /// <summary>공격 모션을 첫 프레임부터 재생합니다.</summary>
        public void PlayAttack()
        {
            PlayFromStart(AttackState);
        }

        /// <summary>피격 모션을 첫 프레임부터 재생합니다.</summary>
        public void PlayHurt()
        {
            PlayFromStart(HurtState);
        }

        /// <summary>활성화 직후 Idle 첫 프레임을 표시합니다.</summary>
        public void ResetToIdle()
        {
            if (PlayFromStart(IdleState))
            {
                animator.Update(0f);
            }
        }

        /// <summary>연결되어 있고 활성화된 Animator의 해당 상태를 처음부터 재생합니다.</summary>
        /// <param name="state">기본 레이어 상태의 전체 경로 해시입니다.</param>
        private bool PlayFromStart(int state)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (!animator.isInitialized)
            {
                animator.Rebind();
            }

            if (!animator.isInitialized || !animator.HasState(0, state))
            {
                return false;
            }

            animator.Play(state, 0, 0f);
            return true;
        }
    }
}
