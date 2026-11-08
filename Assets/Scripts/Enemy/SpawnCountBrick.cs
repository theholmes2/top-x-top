using System;
using System.Collections.Generic;

namespace TopXTop.Enemy
{
    /// <summary>난이도 전체·종류별 동시 개체 수가 각 상한을 넘지 않도록 순수 정수로 판정합니다.</summary>
    public sealed class SpawnCountBrick
    {
        /// <summary>구성 시 복사한 종류별 최대 동시 개체 수입니다.</summary>
        private readonly int[] limits;

        /// <summary>성공한 스폰 요청이 점유하고 있는 종류별 개체 수입니다.</summary>
        private readonly int[] counts;

        /// <summary>난이도 전체 동시 스폰 상한입니다. 종류별 합계보다 커도 허용합니다.</summary>
        private readonly int maxTotalCount;

        /// <summary>모든 종류를 합친 현재 점유 수입니다.</summary>
        public int TotalCount { get; private set; }

        /// <summary>난이도 전체의 동시 상한이 찼는지 제공합니다.</summary>
        public bool IsTotalFull => TotalCount >= maxTotalCount;

        /// <summary>종류 순서에 대응하는 상한을 복사해 독립된 계수 상태를 만듭니다.</summary>
        /// <param name="maxCounts">각 종류의 1 이상인 동시 개체 수 상한입니다.</param>
        /// <param name="maxTotalCount">난이도 전체의 1 이상인 동시 상한입니다.</param>
        public SpawnCountBrick(IReadOnlyList<int> maxCounts, int maxTotalCount)
        {
            if (maxCounts == null)
            {
                throw new ArgumentNullException(nameof(maxCounts));
            }

            if (maxTotalCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTotalCount));
            }

            this.maxTotalCount = maxTotalCount;
            limits = new int[maxCounts.Count];
            counts = new int[maxCounts.Count];

            for (int i = 0; i < maxCounts.Count; i++)
            {
                if (maxCounts[i] < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxCounts));
                }

                limits[i] = maxCounts[i];
            }
        }

        /// <summary>상한 아래인 경우에만 한 자리 점유에 성공합니다.</summary>
        /// <param name="kindIndex">난이도 목록에서 적 종류의 순서입니다.</param>
        public bool TryAcquire(int kindIndex)
        {
            if (TotalCount >= maxTotalCount || counts[kindIndex] >= limits[kindIndex])
            {
                return false;
            }

            counts[kindIndex]++;
            TotalCount++;
            return true;
        }

        /// <summary>점유된 자리 하나를 해제하며 중복 해제는 거부합니다.</summary>
        /// <param name="kindIndex">반환한 적 종류의 순서입니다.</param>
        public bool TryRelease(int kindIndex)
        {
            if (counts[kindIndex] == 0)
            {
                return false;
            }

            counts[kindIndex]--;
            TotalCount--;
            return true;
        }

        /// <summary>해당 종류의 동시 상한을 제공합니다. 순환 스폰의 수량 가중치로도 사용합니다.</summary>
        /// <param name="kindIndex">조회할 종류의 순서입니다.</param>
        public int GetLimit(int kindIndex)
        {
            return limits[kindIndex];
        }

        /// <summary>해당 종류의 현재 점유 수를 제공합니다.</summary>
        /// <param name="kindIndex">조회할 적 종류의 순서입니다.</param>
        public int GetCount(int kindIndex)
        {
            return counts[kindIndex];
        }
    }
}
