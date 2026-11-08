using System;

namespace TopXTop.Enemy
{
    /// <summary>최초 즉시, 이후 간격마다 최대 한 번의 스폰 기회를 제공하는 순수 시간 판정입니다.</summary>
    public sealed class SpawnScheduleBrick
    {
        /// <summary>스폰 시도 사이의 간격이며 단위는 밀리초입니다.</summary>
        private readonly int intervalMs;

        /// <summary>다음 스폰 시도 가능 시각입니다. 최초 값 0은 첫 요청 즉시를 뜻합니다.</summary>
        public long NextSpawnAtMs { get; private set; }

        /// <summary>게임 시각과 동일한 기준으로 적용할 스폰 간격을 받습니다.</summary>
        /// <param name="intervalMs">1 이상의 간격 밀리초입니다.</param>
        public SpawnScheduleBrick(int intervalMs)
        {
            if (intervalMs < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs));
            }

            this.intervalMs = intervalMs;
        }

        /// <summary>기회가 되면 다음 시도를 현재 시각 기준으로 미룹니다. 포화·지연분은 누적하지 않습니다.</summary>
        /// <param name="nowMs">호출자가 공급한 0 이상의 게임 시각 밀리초입니다.</param>
        public bool TryTakeOpportunity(long nowMs)
        {
            if (nowMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nowMs));
            }

            if (nowMs < NextSpawnAtMs)
            {
                return false;
            }

            if (nowMs > long.MaxValue - intervalMs)
            {
                throw new ArgumentOutOfRangeException(nameof(nowMs));
            }

            NextSpawnAtMs = nowMs + intervalMs;
            return true;
        }
    }
}
