using UnityEngine;

namespace TopXTop.Simulation
{
    /// <summary>같은 폭의 숲 구간을 적과 같은 프레임 이동량으로 순환시킵니다.</summary>
    public sealed class BackgroundScrollGlue : MonoBehaviour
    {
        /// <summary>왼쪽부터 배치한 동일한 배경 조각입니다.</summary>
        [SerializeField]
        private Transform[] sections;

        /// <summary>조각 한 개의 월드 가로 길이입니다.</summary>
        [SerializeField, Min(0.01f)]
        private float sectionWidth;

        /// <summary>조각의 오른쪽 끝이 이 값을 지나면 오른쪽으로 순환합니다.</summary>
        [SerializeField]
        private float wrapLeftX;

        /// <summary>전체 반복 폭 안으로 제한한 이동 위상입니다.</summary>
        private float phase;

        /// <summary>중앙 Tick이 공급한 거리만큼 배경을 이동합니다.</summary>
        /// <param name="distance">이번 프레임에 왼쪽으로 이동할 월드 거리입니다.</param>
        public void Advance(float distance)
        {
            if (distance <= 0f || sectionWidth <= 0f || sections == null || sections.Length < 2)
            {
                return;
            }

            float loopWidth = sectionWidth * sections.Length;
            phase = Mathf.Repeat(phase + distance, loopWidth);
            for (int i = 0; i < sections.Length; i++)
            {
                Transform section = sections[i];
                if (section == null)
                {
                    continue;
                }

                Vector3 position = section.position;
                float offset = i * sectionWidth - phase;
                if (offset <= -sectionWidth)
                {
                    offset += loopWidth;
                }

                position.x = wrapLeftX + offset;
                section.position = position;
            }
        }
    }
}
