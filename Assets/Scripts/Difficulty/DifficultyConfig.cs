using System;
using System.Collections.Generic;
using TopXTop.Enemy;
using UnityEngine;
using UnityEngine.Serialization;

namespace TopXTop.Difficulty
{
    /// <summary>난이도 전체 상한과 적 종류별 프리팹·동시 상한을 정의하는 공유 기획 데이터입니다.</summary>
    [CreateAssetMenu(
        fileName = "DifficultyConfig",
        menuName = "TopXTop/Difficulty/난이도 정의")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        /// <summary>난이도를 구분하는 1 이상의 번호입니다.</summary>
        [SerializeField, Min(1)]
        private int difficultyId = 1;

        /// <summary>모든 적 종류를 합친 난이도 전체의 최대 동시 개체 수입니다. 풀 보관 수와 별개입니다.</summary>
        [SerializeField, Min(1)]
        private int maxTotalSpawnCount = 1;

        /// <summary>이 난이도에서 등장할 적 종류와 종류별 동시 최대 스폰 수 목록입니다.</summary>
        [SerializeField]
        private List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();

        /// <summary>난이도 식별 번호를 제공합니다.</summary>
        public int DifficultyId => difficultyId;

        /// <summary>종류를 합친 동시 스폰 상한입니다. 종류별 상한 합계보다 커도 유효합니다.</summary>
        public int MaxTotalSpawnCount => maxTotalSpawnCount;

        /// <summary>외부에서 항목을 추가하거나 제거할 수 없는 출현 목록을 제공합니다.</summary>
        public IReadOnlyList<EnemySpawnEntry> Enemies => enemies.AsReadOnly();

        /// <summary>난이도 데이터가 사용 가능한지 검사하고 첫 오류의 이유를 제공합니다.</summary>
        /// <param name="error">정상이면 빈 문자열, 잘못된 데이터이면 수정할 내용입니다.</param>
        /// <returns>모든 항목이 유효하면 true입니다.</returns>
        public bool TryValidate(out string error)
        {
            if (difficultyId < 1)
            {
                error = "난이도 번호는 1 이상이어야 합니다.";
                return false;
            }

            if (maxTotalSpawnCount < 1)
            {
                error = "난이도 전체 최대 스폰 수는 1 이상이어야 합니다.";
                return false;
            }

            if (enemies == null || enemies.Count == 0)
            {
                error = "출현 목록에 적을 한 종류 이상 지정해야 합니다.";
                return false;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                // Inspector의 목록 순서와 맞추어 문제 항목을 1부터 안내합니다.
                EnemySpawnEntry entry = enemies[i];

                if (entry == null || entry.Prefab == null)
                {
                    error = $"출현 목록 {i + 1}번의 적 프리팹 참조가 비어 있습니다.";
                    return false;
                }

                if (entry.Prefab.StatsConfig == null)
                {
                    error = $"출현 목록 {i + 1}번 프리팹의 StatsConfig 참조가 필요합니다.";
                    return false;
                }

                for (int previous = 0; previous < i; previous++)
                {
                    if (enemies[previous].Prefab == entry.Prefab)
                    {
                        error = $"출현 목록 {i + 1}번에 동일한 프리팹이 중복 지정되었습니다.";
                        return false;
                    }
                }

                if (entry.MaxSpawnCount < 1)
                {
                    error = $"출현 목록 {i + 1}번의 최대 스폰 수는 1 이상이어야 합니다.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Inspector 편집 시 잘못된 데이터를 자동 보정하지 않고 경고합니다.</summary>
        private void OnValidate()
        {
            if (!TryValidate(out string error))
            {
                Debug.LogWarning($"난이도 Config '{name}': {error}", this);
            }
        }
    }

    /// <summary>출현 목록 한 칸에서 적 종류와 해당 종류가 동시에 존재할 수 있는 개체 수 상한을 정의합니다.</summary>
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        /// <summary>외형과 기본 능력치 정의가 연결된 적 프리팹입니다.</summary>
        [SerializeField]
        private EnemyGlue prefab;

        /// <summary>이 항목의 적 종류가 동시에 존재할 수 있는 최대 개체 수입니다.</summary>
        [FormerlySerializedAs("count")]
        [SerializeField, Min(1)]
        [Tooltip("이 적 종류가 동시에 존재할 수 있는 개체 수의 상한입니다.")]
        private int maxSpawnCount = 1;

        /// <summary>대여할 적 종류의 프리팹을 제공합니다.</summary>
        public EnemyGlue Prefab => prefab;

        /// <summary>이 적 종류의 동시 최대 스폰 수를 제공합니다.</summary>
        public int MaxSpawnCount => maxSpawnCount;
    }
}
