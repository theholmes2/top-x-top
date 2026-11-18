using System;
using System.Collections.Generic;
using TopXTop.Difficulty;
using UnityEngine;

namespace TopXTop.Enemy
{
    /// <summary>상한과 종류별 순환 순서를 지켜 등장시키고 먼저 등장한 적부터 추적합니다.</summary>
    public sealed class EnemySpawnGlue : IDisposable
    {
        /// <summary>활성 목록 한 칸에 실제 개체와 종류 순서를 함께 보관합니다.</summary>
        private readonly struct ActiveEnemy
        {
            /// <summary>이번 등장에 해당하는 대여 개체입니다.</summary>
            public readonly EnemyGlue Instance;

            /// <summary>종류별 점유 해제에 사용할 종류 번호입니다.</summary>
            public readonly int KindIndex;

            /// <summary>대여 개체와 종류를 하나의 목록 항목으로 묶습니다.</summary>
            /// <param name="instance">대여된 적 개체입니다.</param>
            /// <param name="kindIndex">난이도 목록의 종류 순서입니다.</param>
            public ActiveEnemy(EnemyGlue instance, int kindIndex)
            {
                Instance = instance;
                KindIndex = kindIndex;
            }
        }

        /// <summary>배경과 함께 이동하며 사망 연출을 마칠 때까지 유지할 대여입니다.</summary>
        private readonly struct DyingEnemy
        {
            /// <summary>사망 연출을 표시하는 적입니다.</summary>
            public readonly EnemyGlue Instance;

            /// <summary>이 대여와 이후 대여를 구분하는 State입니다.</summary>
            public readonly EnemyState State;

            /// <summary>Animator 완료를 기다리는 게임 시각 상한입니다.</summary>
            public readonly long AnimationDeadlineMs;

            /// <summary>연출 완료를 기다리는 대여 정보를 기록합니다.</summary>
            public DyingEnemy(EnemyGlue instance, EnemyState state, long animationDeadlineMs)
            {
                Instance = instance;
                State = state;
                AnimationDeadlineMs = animationDeadlineMs;
            }
        }

        /// <summary>Animator 완료가 감지되지 않을 때 적용할 게임 시각 제한입니다.</summary>
        private const int DeathTimeoutMs = 2000;

        /// <summary>프리팹별 생성·재사용·소유권 정리를 담당하는 공유 풀입니다.</summary>
        private readonly EnemyPoolGlue pool;

        /// <summary>난이도 설정에서 복사한 종류별 원본 프리팹입니다.</summary>
        private readonly List<EnemyGlue> prefabs = new List<EnemyGlue>();

        /// <summary>등장 성공 순서를 보장하는 활성 개체 목록입니다. 재대여는 맨 뒤에 추가합니다.</summary>
        private readonly List<ActiveEnemy> active = new List<ActiveEnemy>();

        /// <summary>스폰 점유를 해제한 뒤 이동과 사망 연출을 처리하는 대여 목록입니다.</summary>
        private readonly List<DyingEnemy> dying = new List<DyingEnemy>();

        /// <summary>0번 전투 자리의 월드 X입니다.</summary>
        private float frontX;

        /// <summary>난이도의 최대 스폰 수로 고정한 칸 간격입니다.</summary>
        private float slotSpacing;

        /// <summary>전체·종류별 동시 상한과 점유를 판정하는 순수 로직입니다.</summary>
        private SpawnCountBrick counts;

        /// <summary>다음 순환 요청에서 시도할 종류의 순서입니다.</summary>
        private int nextKindIndex;

        /// <summary>현재 종류의 순환 차례에 남아 있는 수량입니다.</summary>
        private int remainingInTurn;

        /// <summary>종료 후 스폰을 막고 중복 정리를 방지하는 상태입니다.</summary>
        private bool disposed;

        /// <summary>현재 전투 줄에서 스폰 점유를 차지하는 개체 수입니다.</summary>
        public int ActiveCount => active.Count;

        /// <summary>이 스폰 담당의 소유권이 끝났는지 제공합니다.</summary>
        public bool IsDisposed => disposed;

        /// <summary>VContainer에서 공통 풀을 생성자로 주입받습니다.</summary>
        /// <param name="pool">대여·반환에 사용할 풀입니다.</param>
        public EnemySpawnGlue(EnemyPoolGlue pool)
        {
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        /// <summary>유효한 난이도 참조와 상한을 복사합니다. 활성 개체가 있으면 변경을 거부합니다.</summary>
        /// <param name="config">시작할 난이도 정의입니다.</param>
        /// <param name="frontX">전투 줄 앞칸의 월드 X입니다.</param>
        /// <param name="backX">전투 줄 뒤쪽 경계의 월드 X입니다.</param>
        public void Configure(DifficultyConfig config, float frontX = 0f, float backX = 0f)
        {
            ThrowIfDisposed();
            if (active.Count > 0 || dying.Count > 0)
            {
                throw new InvalidOperationException("기존 스폰 개체를 먼저 반환해야 난이도를 변경할 수 있습니다.");
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(config));
            }

            this.frontX = frontX;
            slotSpacing = config.MaxTotalSpawnCount > 1
                ? (backX - frontX) / (config.MaxTotalSpawnCount - 1)
                : 0f;

            prefabs.Clear();
            var limits = new List<int>();
            foreach (EnemySpawnEntry entry in config.Enemies)
            {
                prefabs.Add(entry.Prefab);
                limits.Add(entry.MaxSpawnCount);
            }

            counts = new SpawnCountBrick(limits, config.MaxTotalSpawnCount);
            nextKindIndex = 0;
            remainingInTurn = counts.GetLimit(0);
        }

        /// <summary>수량 가중 순환에서 가능한 종류를 찾아 전체에서 최대 한 마리만 등장시킵니다.</summary>
        /// <param name="position">등장할 적의 월드 위치입니다.</param>
        /// <param name="instance">성공 시 등장한 개체이며, 포화 시 null입니다.</param>
        public bool TrySpawnNext(Vector3 position, out EnemyGlue instance)
        {
            ThrowIfDisposed();
            EnsureConfigured();
            instance = null;

            // 전체 포화는 차례를 소비하지 않습니다. 빈자리가 생기면 남은 순환 수량을 이어갑니다.
            if (counts.IsTotalFull)
            {
                return false;
            }

            // 종류 수만큼만 탐색합니다. 모든 상한이 차도 반드시 유한하게 끝납니다.
            for (int checkedKinds = 0; checkedKinds < prefabs.Count; checkedKinds++)
            {
                if (TrySpawn(prefabs[nextKindIndex], position, out instance))
                {
                    remainingInTurn--;
                    if (remainingInTurn == 0)
                    {
                        AdvanceKind();
                    }

                    return true;
                }

                AdvanceKind();
            }

            return false;
        }

        /// <summary>지정한 종류 한 마리를 전체·종류별 상한 안에서 등장시킵니다.</summary>
        /// <param name="prefab">현재 난이도에 등록된 원본 프리팹입니다.</param>
        /// <param name="position">활성화 전에 설정할 월드 위치입니다.</param>
        /// <param name="instance">성공 시 새 상태로 초기화된 대여 개체입니다.</param>
        public bool TrySpawn(EnemyGlue prefab, Vector3 position, out EnemyGlue instance)
        {
            ThrowIfDisposed();
            EnsureConfigured();
            instance = null;
            int index = prefabs.IndexOf(prefab);
            if (index < 0 || !counts.TryAcquire(index))
            {
                return false;
            }

            bool fadeIn = active.Count > 0;
            if (fadeIn)
            {
                position.x = GetNextSlotX();
            }

            EnemyGlue rented = null;
            try
            {
                rented = pool.Rent(prefab);
                rented.transform.SetParent(null, false);
                rented.transform.position = position;
                active.Add(new ActiveEnemy(rented, index));
                RefreshDisplayOrder();
                rented.gameObject.SetActive(true);
                rented.ShowIdle();
                if (fadeIn)
                {
                    rented.BeginSpawnFade();
                }

                instance = rented;
                return true;
            }
            catch
            {
                if (rented != null)
                {
                    int addedIndex = FindActiveIndex(rented);
                    if (addedIndex >= 0)
                    {
                        active.RemoveAt(addedIndex);
                    }

                    pool.TryReturn(rented);
                    RefreshDisplayOrder();
                }

                counts.TryRelease(index);
                throw;
            }
        }

        /// <summary>현재 줄 바로 뒤 칸을 계산합니다. 앞선 적이 당겨지는 중이면 그 줄의 위치를 따릅니다.</summary>
        private float GetNextSlotX()
        {
            int tailIndex = active.Count - 1;
            float tailSlotX = frontX + tailIndex * slotSpacing;
            float tailOffset = Mathf.Max(0f, active[tailIndex].Instance.transform.position.x - tailSlotX);
            return frontX + active.Count * slotSpacing + tailOffset;
        }

        /// <summary>생존 적을 전진시키고 실제 이동 거리의 최댓값으로 사망 연출을 함께 이동시킵니다.</summary>
        /// <param name="distance">이번 프레임에 각 적이 이동할 수 있는 최대 거리입니다.</param>
        /// <param name="nowMs">Animator 완료 제한을 확인할 게임 시각입니다.</param>
        /// <param name="deltaSeconds">이번 프레임의 페이드에 적용할 게임 시간입니다.</param>
        public float AdvanceMovement(float distance, long nowMs, float deltaSeconds)
        {
            float maxDistance = 0f;
            float movementLimit = Mathf.Max(0f, distance);
            for (int i = 0; i < active.Count; i++)
            {
                EnemyGlue instance = active[i].Instance;
                if (instance == null)
                {
                    continue;
                }

                instance.AdvanceSpawnFade(deltaSeconds);
                Vector3 destination = instance.transform.position;
                destination.x = frontX + i * slotSpacing;
                maxDistance = Mathf.Max(maxDistance, MoveEnemy(instance.transform, destination, movementLimit));
            }

            for (int i = dying.Count - 1; i >= 0; i--)
            {
                DyingEnemy entry = dying[i];
                if (entry.Instance == null || !ReferenceEquals(entry.State, entry.Instance.State))
                {
                    dying.RemoveAt(i);
                    continue;
                }

                if (nowMs >= entry.AnimationDeadlineMs || entry.Instance.IsDeathFinished())
                {
                    dying.RemoveAt(i);
                    pool.TryReturn(entry.Instance);
                    continue;
                }

                if (maxDistance > 0f)
                {
                    Transform mover = entry.Instance.transform;
                    mover.position += Vector3.left * maxDistance;
                }
            }

            return maxDistance;
        }

        /// <summary>목표를 넘지 않게 이동하고 이번 프레임에 실제로 이동한 거리를 계산합니다.</summary>
        private static float MoveEnemy(Transform mover, Vector3 destination, float distance)
        {
            Vector3 previous = mover.position;
            Vector3 next = Vector3.MoveTowards(previous, destination, distance);
            if (next.Equals(previous))
            {
                return 0f;
            }

            mover.position = next;
            return Vector3.Distance(previous, next);
        }

        /// <summary>0번 칸에 도착한 첫 생존 적을 공격 대상으로 제공합니다.</summary>
        public EnemyGlue GetCombatTarget()
        {
            if (active.Count == 0)
            {
                return null;
            }

            EnemyGlue front = active[0].Instance;
            if (front != null && front.State != null && front.State.IsAlive && front.transform.position.x == frontX)
            {
                return front;
            }

            return null;
        }

        /// <summary>사망한 적의 스폰 점유를 한 번 해제하고 현재 위치에서 사망 연출을 시작합니다.</summary>
        /// <param name="instance">이번 타격으로 사망한 적입니다.</param>
        /// <param name="nowMs">사망이 확정된 게임 시각입니다.</param>
        public bool TryBeginDeath(EnemyGlue instance, long nowMs)
        {
            if (disposed || instance == null || instance.State == null || instance.State.IsAlive)
            {
                return false;
            }

            int index = FindActiveIndex(instance);
            if (index < 0)
            {
                return false;
            }

            ReleaseActive(index);
            if (instance.TryPlayDeath())
            {
                long animationDeadlineMs = Math.Min(nowMs, long.MaxValue - DeathTimeoutMs) + DeathTimeoutMs;
                dying.Add(new DyingEnemy(instance, instance.State, animationDeadlineMs));
            }
            else
            {
                pool.TryReturn(instance);
            }

            return true;
        }

        /// <summary>등장 순서에서 State가 존재하고 살아 있는 첫 적을 선택합니다.</summary>
        public EnemyGlue GetFirstLivingEnemy()
        {
            foreach (ActiveEnemy entry in active)
            {
                if (entry.Instance != null && entry.Instance.State != null && entry.Instance.State.IsAlive)
                {
                    return entry.Instance;
                }
            }

            return null;
        }

        /// <summary>이 담당이 등장시킨 개체를 한 번 반환하고 목록·점유·표시 순서를 갱신합니다.</summary>
        /// <param name="instance">반환할 개체입니다.</param>
        public bool TryDespawn(EnemyGlue instance)
        {
            if (disposed || instance == null)
            {
                return false;
            }

            int index = FindActiveIndex(instance);
            if (index < 0 || !pool.TryReturn(instance))
            {
                return false;
            }

            ReleaseActive(index);
            return true;
        }

        /// <summary>활성 목록에서 제거하면서 종류별 및 전체 점유를 해제합니다.</summary>
        private void ReleaseActive(int index)
        {
            counts.TryRelease(active[index].KindIndex);
            active.RemoveAt(index);
            RefreshDisplayOrder();
        }

        /// <summary>해당 종류의 현재 점유 수를 제공합니다.</summary>
        /// <param name="prefab">조회할 원본 프리팹입니다.</param>
        public int GetActiveCount(EnemyGlue prefab)
        {
            int index = prefabs.IndexOf(prefab);
            return index < 0 || counts == null ? 0 : counts.GetCount(index);
        }

        /// <summary>자신의 활성 개체와 사망 연출 개체를 반환합니다. 풀의 선행 정리와 중복 호출에도 안전합니다.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (ActiveEnemy entry in active)
            {
                pool.TryReturn(entry.Instance);
            }

            foreach (DyingEnemy entry in dying)
            {
                if (entry.Instance != null && ReferenceEquals(entry.State, entry.Instance.State))
                {
                    pool.TryReturn(entry.Instance);
                }
            }

            dying.Clear();
            active.Clear();
            prefabs.Clear();
            counts = null;
        }

        /// <summary>한 종류의 차례가 끝나거나 포화되면 다음 종류와 수량으로 이동합니다.</summary>
        private void AdvanceKind()
        {
            nextKindIndex = (nextKindIndex + 1) % prefabs.Count;
            remainingInTurn = counts.GetLimit(nextKindIndex);
        }

        /// <summary>현재 목록 크기로 표시 순서를 다시 계산해 먼저 등장한 생존 적이 앞에 보이게 합니다.</summary>
        private void RefreshDisplayOrder()
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Instance != null)
                {
                    // 누적 스폰 횟수를 쓰지 않아 장시간 실행해도 sortingOrder가 증가하지 않습니다.
                    active[i].Instance.SetDisplayOrder(Math.Min(short.MaxValue, active.Count - i - 1));
                }
            }
        }

        /// <summary>별도 Dictionary 없이 현재 소수 개체 목록에서 같은 대여 참조를 찾습니다.</summary>
        /// <param name="instance">찾을 대여 개체입니다.</param>
        private int FindActiveIndex(EnemyGlue instance)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (ReferenceEquals(active[i].Instance, instance))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>난이도 설정 전 요청에 이해하기 쉬운 오류를 제공합니다.</summary>
        private void EnsureConfigured()
        {
            if (counts == null)
            {
                throw new InvalidOperationException("스폰 요청 전에 난이도를 Configure해야 합니다.");
            }
        }

        /// <summary>소유권이 끝난 서비스의 신규 사용을 거부합니다.</summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemySpawnGlue));
            }
        }
    }
}
