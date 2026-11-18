using UnityEngine;

namespace TopXTop.Character
{
    /// <summary>확정된 전투 결과에 따라 SpriteRenderer를 구동하는 Animator에 재생을 요청합니다.</summary>
    public sealed class CharacterAnimationGlue : MonoBehaviour
    {
        /// <summary>Inspector에서 Controller와 연결한 Animator입니다.</summary>
        [SerializeField]
        private Animator animator;

        /// <summary>기본공격 첫 번째 모션을 샘플링할 클립입니다.</summary>
        [SerializeField]
        private AnimationClip attackOneClip;

        /// <summary>기본공격 두 번째 모션을 샘플링할 클립입니다.</summary>
        [SerializeField]
        private AnimationClip attackTwoClip;

        /// <summary>기본공격 세 번째 모션을 샘플링할 클립입니다.</summary>
        [SerializeField]
        private AnimationClip attackThreeClip;

        /// <summary>잔상에서 복제할 플레이어 본체입니다.</summary>
        [SerializeField]
        private SpriteRenderer body;

        /// <summary>잔상용 스프라이트 렌더러입니다.</summary>
        public SpriteRenderer Body => body;

        /// <summary>대여를 시작할 때 표시할 기본 상태입니다.</summary>
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

        /// <summary>플레이어 공격 연출 상태입니다.</summary>
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");

        /// <summary>기본공격 연계의 두 번째 모션입니다.</summary>
        private static readonly int AttackTwoState = Animator.StringToHash("Base Layer.Attack2");

        /// <summary>기본공격 연계의 세 번째 모션입니다.</summary>
        private static readonly int AttackThreeState = Animator.StringToHash("Base Layer.Attack3");

        /// <summary>중앙 Tick으로 공격 자세를 고정할 Animator 속도 배율입니다.</summary>
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");

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

        /// <summary>플레이어가 배경 이동 중 표시할 상태입니다.</summary>
        private static readonly int RunState = Animator.StringToHash("Base Layer.Run");

        /// <summary>공격을 끝낸 뒤 돌아갈 상태를 선택하는 Animator 매개변수입니다.</summary>
        private static readonly int MovingParameter = Animator.StringToHash("Moving");

        /// <summary>배경 이동 여부를 Animator에 전달합니다. 현재 재생 중인 상태는 직접 바꾸지 않습니다.</summary>
        /// <param name="moving">이번 프레임에 배경이 이동했는지 여부입니다.</param>
        public void SetMoving(bool moving)
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null &&
                animator.HasState(0, RunState))
            {
                animator.SetBool(MovingParameter, moving);
            }
        }

        /// <summary>클립 원속도에서 검이 접촉하는 게임 시간입니다. Animator 유무와 관계없이 동일합니다.</summary>
        public static int GetContactMs(int stage)
        {
            return stage == 2 ? 300 : 200;
        }

        /// <summary>현재 예약의 선딜 진행도에 해당하는 자세를 즉시 표시합니다.</summary>
        public void ShowWindup(int stage, float progress)
        {
            SampleAttack(stage, progress, false);
        }

        /// <summary>접촉 순간부터 공통 후딜 끝까지 진행도에 해당하는 자세를 즉시 표시합니다.</summary>
        public void ShowRecovery(int stage, float progress)
        {
            SampleAttack(stage, progress, true);
        }

        private void SampleAttack(int stage, float progress, bool recovery)
        {
            AnimationClip clip = stage == 2 ? attackTwoClip : stage == 3 ? attackThreeClip : attackOneClip;
            if (clip == null || clip.length <= 0f)
            {
                return;
            }

            int state = stage == 2 ? AttackTwoState : stage == 3 ? AttackThreeState : AttackState;
            float contact = Mathf.Min(GetContactMs(stage) / 1000f, clip.length);
            // 키 경계에서 부동소수점 오차로 앞 스프라이트가 선택되지 않도록 합니다.
            float seconds = recovery
                ? Mathf.Lerp(contact + 0.0001f, clip.length, progress)
                : Mathf.Min(contact * progress, contact - 0.0001f);
            if (PlayFromStart(state, Mathf.Clamp(seconds / clip.length, 0f, 0.9999f)))
            {
                animator.SetFloat(AttackSpeed, 0f);
                animator.Update(0f);
            }
        }

        /// <summary>피격 모션을 첫 프레임부터 재생합니다.</summary>
        public void PlayHurt()
        {
            if (PlayFromStart(HurtState))
            {
                animator.Update(0f);
            }
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
        /// <param name="normalizedTime">해당 상태에서 표시할 클립 진행 비율입니다.</param>
        private bool PlayFromStart(int state, float normalizedTime = 0f)
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

            animator.Play(state, 0, normalizedTime);
            return true;
        }
    }
}
