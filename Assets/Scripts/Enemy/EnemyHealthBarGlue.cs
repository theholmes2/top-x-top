using System;
using R3;
using UnityEngine;

namespace TopXTop.Enemy
{
    /// <summary>대여마다 새 체력 ViewModel을 구독하고 월드 체력바에 표시합니다.</summary>
    public sealed class EnemyHealthBarGlue : MonoBehaviour
    {
        /// <summary>왼쪽부터 체력 비율만큼 표시되는 채움 영역입니다.</summary>
        [SerializeField]
        private RectTransform fill;

        /// <summary>등장 페이드와 함께 체력바 전체의 투명도를 조정합니다.</summary>
        [SerializeField]
        private CanvasGroup opacity;

        /// <summary>이번 대여의 체력 원본을 구독하는 ViewModel입니다.</summary>
        private EnemyHealthViewModel viewModel;

        /// <summary>이번 대여의 화면 구독입니다.</summary>
        private IDisposable subscription;

        /// <summary>새 대여의 체력을 즉시 표시합니다.</summary>
        public void Bind(EnemyState state)
        {
            Unbind();
            viewModel = new EnemyHealthViewModel(state);
            subscription = viewModel.Values.Subscribe(Render);
        }

        /// <summary>반환 전에 구독을 끊고 다음 대여를 위해 표시를 초기화합니다.</summary>
        public void Unbind()
        {
            subscription?.Dispose();
            subscription = null;
            viewModel?.Dispose();
            viewModel = null;
            SetOpacity(1f);
        }

        /// <summary>적 본체의 등장 투명도를 체력바에 반영합니다.</summary>
        public void SetOpacity(float alpha)
        {
            if (opacity != null)
            {
                opacity.alpha = alpha;
            }
        }

        /// <summary>적의 앞뒤 정렬에 맞춰 체력바도 같은 순서를 사용합니다.</summary>
        public void SetDisplayOrder(int order)
        {
            if (opacity != null)
            {
                opacity.GetComponent<Canvas>().sortingOrder = Mathf.Min(short.MaxValue, 20 + order);
            }
        }

        /// <summary>체력 비율을 왼쪽 기준으로 표시하고 사망 시 숨깁니다.</summary>
        private void Render((int Current, int Max) value)
        {
            if (fill == null || opacity == null)
            {
                return;
            }

            fill.anchorMax = new Vector2((float)value.Current / value.Max, 1f);
            opacity.gameObject.SetActive(value.Current > 0);
        }

        /// <summary>씬 파괴 시에도 현재 대여의 구독을 정리합니다.</summary>
        private void OnDestroy()
        {
            Unbind();
        }
    }
}
