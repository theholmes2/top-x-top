using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopXTop.Simulation
{
    /// <summary>대시 중의 플레이어 자세를 복제하고 지나간 자세를 월드 스크롤에 맞춰 흘립니다.</summary>
    public sealed class DashGhostTrailGlue : IDisposable
    {
        /// <summary>재사용할 잔상 렌더러와 남은 표시 시간입니다.</summary>
        private sealed class Ghost
        {
            /// <summary>복제한 스프라이트를 표시합니다.</summary>
            public SpriteRenderer Renderer;

            /// <summary>현재 잔상의 남은 수명입니다.</summary>
            public float Remaining;

            /// <summary>페이드 시작 시의 투명도입니다.</summary>
            public float Alpha;
        }

        /// <summary>잔상이 화면에서 사라지는 게임 시간입니다.</summary>
        private const float Lifetime = 0.20f;

        /// <summary>Scope에서 다시 쓸 수 있도록 보관하는 렌더러입니다.</summary>
        private readonly List<Ghost> ghosts = new List<Ghost>();

        /// <summary>다음 자세를 복제할 때까지 남은 시간입니다.</summary>
        private float sampleDelay;

        /// <summary>달리기와 베기 중 어느 간격으로 잔상을 남겼는지 보관합니다.</summary>
        private bool samplingSlash;

        /// <summary>압축 선딜에서 같은 스프라이트를 연속 복제하지 않도록 마지막 자세를 기억합니다.</summary>
        private Sprite lastSlashSprite;

        /// <summary>기존 잔상을 이동시킨 뒤 대시 중이라면 현재 자세를 복제합니다.</summary>
        public void Advance(float distance, float seconds, SpriteRenderer source, bool slash, bool stationary = false)
        {
            foreach (Ghost ghost in ghosts)
            {
                if (ghost.Remaining <= 0f)
                {
                    continue;
                }

                ghost.Remaining = Mathf.Max(0f, ghost.Remaining - seconds);
                ghost.Renderer.transform.position += Vector3.left * distance;
                Color tint = ghost.Renderer.color;
                tint.a = ghost.Alpha * (ghost.Remaining / Lifetime);
                ghost.Renderer.color = tint;
                ghost.Renderer.enabled = ghost.Remaining > 0f;
            }

            if (source == null || source.sprite == null)
            {
                ResetSampling();
                return;
            }

            if (samplingSlash != slash)
            {
                sampleDelay = 0f;
                samplingSlash = slash;
            }

            sampleDelay -= seconds;
            if (sampleDelay > 0f || (slash && source.sprite == lastSlashSprite))
            {
                return;
            }

            sampleDelay = slash ? 0.025f : 0.055f;
            lastSlashSprite = slash ? source.sprite : null;
            Ghost current = GetAvailable();
            var copy = current.Renderer;
            copy.sprite = source.sprite;
            copy.sharedMaterial = source.sharedMaterial;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = Mathf.Max(short.MinValue, source.sortingOrder - 1);
            Vector3 offset = stationary ? Vector3.left * (source.flipX ? -0.09f : 0.09f) : Vector3.zero;
            copy.transform.SetPositionAndRotation(source.transform.position + offset, source.transform.rotation);
            copy.transform.localScale = source.transform.lossyScale;
            current.Remaining = Lifetime;
            current.Alpha = slash ? 0.55f : 0.38f;
            Color color = source.color;
            color.a *= current.Alpha;
            current.Alpha = color.a;
            copy.color = color;
            copy.enabled = true;
        }

        /// <summary>새 접근에서는 첫 잔상을 다시 즉시 남길 수 있게 합니다.</summary>
        public void ResetSampling()
        {
            sampleDelay = 0f;
            samplingSlash = false;
            lastSlashSprite = null;
        }

        /// <summary>수명이 끝난 렌더러를 찾고 필요할 때만 하나를 생성합니다.</summary>
        private Ghost GetAvailable()
        {
            foreach (Ghost ghost in ghosts)
            {
                if (ghost.Remaining <= 0f)
                {
                    return ghost;
                }
            }

            var created = new Ghost { Renderer = new GameObject("DashGhost").AddComponent<SpriteRenderer>() };
            ghosts.Add(created);
            return created;
        }

        /// <summary>전투 Scope가 끝날 때 모든 잔상 오브젝트를 정리합니다.</summary>
        public void Dispose()
        {
            foreach (Ghost ghost in ghosts)
            {
                if (ghost.Renderer != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(ghost.Renderer.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(ghost.Renderer.gameObject);
                    }
                }
            }

            ghosts.Clear();
        }
    }
}
