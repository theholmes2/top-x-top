using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TopXTop.Enemy
{
    /// <summary>프리팹별 비활성 개체 재사용과 생성한 개체의 소유권을 관리합니다.</summary>
    public sealed class EnemyPoolGlue : IDisposable
    {
        /// <summary>새 개체 생성 시 필요한 컴포넌트 주입을 수행할 컨테이너입니다.</summary>
        private readonly IObjectResolver resolver;

        /// <summary>프리팹별 반환 개체 보관함입니다. 동시 스폰 상한과 별개입니다.</summary>
        private readonly Dictionary<EnemyGlue, Stack<EnemyGlue>> available =
            new Dictionary<EnemyGlue, Stack<EnemyGlue>>();

        /// <summary>생성한 모든 개체와 그 원본 프리팹의 대응입니다.</summary>
        private readonly Dictionary<EnemyGlue, EnemyGlue> origins =
            new Dictionary<EnemyGlue, EnemyGlue>();

        /// <summary>현재 이 풀에서 대여 중인 개체 집합입니다.</summary>
        private readonly HashSet<EnemyGlue> rented = new HashSet<EnemyGlue>();

        /// <summary>초기화 전 개체가 활성화되지 않도록 생성 시 사용하는 비활성 루트입니다.</summary>
        private GameObject storage;

        /// <summary>소유권 종료 후 신규 대여를 차단하는 상태입니다.</summary>
        private bool disposed;

        /// <summary>VContainer에서 생성 서비스 의존성을 주입받습니다.</summary>
        /// <param name="resolver">새 인스턴스 생성·주입에 사용할 컨테이너입니다.</param>
        public EnemyPoolGlue(IObjectResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>비활성 개체를 대여하고 새 상태로 초기화합니다. 위치·활성화는 호출자가 맡습니다.</summary>
        /// <param name="prefab">StatsConfig가 연결된 원본 프리팹입니다.</param>
        public EnemyGlue Rent(EnemyGlue prefab)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyPoolGlue));
            }

            if (prefab == null || prefab.StatsConfig == null)
            {
                throw new ArgumentException("StatsConfig가 연결된 적 프리팹이 필요합니다.", nameof(prefab));
            }

            if (storage == null)
            {
                storage = new GameObject("EnemyPoolStorage");
                storage.SetActive(false);
            }

            if (!available.TryGetValue(prefab, out Stack<EnemyGlue> stack))
            {
                stack = new Stack<EnemyGlue>();
                available.Add(prefab, stack);
            }

            // 외부 파괴로 남은 무효 참조는 다시 대여하지 않습니다.
            EnemyGlue instance = null;
            while (stack.Count > 0 && instance == null)
            {
                instance = stack.Pop();
            }

            if (instance == null)
            {
                instance = resolver.Instantiate(prefab, storage.transform);
                instance.gameObject.SetActive(false);
                origins.Add(instance, prefab);
            }

            try
            {
                instance.InitializeState();
                rented.Add(instance);
                return instance;
            }
            catch
            {
                instance.UnbindState();
                stack.Push(instance);
                throw;
            }
        }

        /// <summary>이 풀에서 대여한 개체만 반환하며 중복·다른 풀 반환은 거부합니다.</summary>
        /// <param name="instance">반환할 대여 개체입니다. 스폰 개체는 스폰 담당을 통해 반환합니다.</param>
        public bool TryReturn(EnemyGlue instance)
        {
            if (disposed || instance == null || !rented.Remove(instance))
            {
                return false;
            }

            EnemyGlue prefab = origins[instance];
            instance.gameObject.SetActive(false);
            instance.UnbindState();
            // 씬 종료 중 루트 개체를 다른 루트의 자식으로 옮기면 Unity 파괴 순회와 충돌할 수 있습니다.
            // 반환 스택·원본 대응이 소유권을 추적하므로 재부모화 없이 비활성 상태로 보관합니다.
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = prefab.transform.localScale;
            var originalBody = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (originalBody != null)
            {
                instance.SetDisplayOrder(originalBody.sortingOrder);
            }

            available[prefab].Push(instance);
            return true;
        }

        /// <summary>수명 종료 시 대여·보관 여부와 관계없이 생성한 모든 개체를 정리합니다.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (EnemyGlue instance in origins.Keys)
            {
                if (instance != null)
                {
                    instance.gameObject.SetActive(false);
                    instance.UnbindState();
                    DestroyOwned(instance.gameObject);
                }
            }

            if (storage != null)
            {
                DestroyOwned(storage);
            }

            rented.Clear();
            origins.Clear();
            available.Clear();
        }

        /// <summary>현재 Unity 실행 환경에 맞는 방식으로 소유 객체를 제거합니다.</summary>
        /// <param name="target">풀에서 생성한 객체입니다.</param>
        private static void DestroyOwned(GameObject target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
