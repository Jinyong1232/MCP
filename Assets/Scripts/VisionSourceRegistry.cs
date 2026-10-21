using System.Collections.Generic;
using UnityEngine;

/// 플레이어 시야(각 컴포넌트 고유의 visionRadius) 외에 추가로 시야를 밝히는 소스(조명탄 등)를
/// 등록해두는 전역 목록. `FogVisibility`/`AttackRangeIndicator`/`FogOfWarGround`는 기존의
/// "플레이어까지 거리 <= visionRadius" 판정에 더해 이 목록도 함께 확인한다.
/// 각 컴포넌트의 visionRadius 값 자체는 건드리지 않는다(docs/feature-expansion.md 2-3④, E2 게이트).
public static class VisionSourceRegistry
{
    private struct Source
    {
        public Transform transform;
        public float radius;
    }

    private static readonly List<Source> sources = new List<Source>();

    public static void Register(Transform source, float radius)
    {
        sources.Add(new Source { transform = source, radius = radius });
    }

    public static void Unregister(Transform source)
    {
        sources.RemoveAll(s => s.transform == source);
    }

    // position이 등록된 소스 중 하나의 반경 안에 있으면 true(플레이어 자체 시야는 호출부에서 별도 처리).
    public static bool IsWithinAnySource(Vector3 position)
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            Source s = sources[i];
            if (s.transform == null) { sources.RemoveAt(i); continue; }
            if (Vector3.Distance(s.transform.position, position) <= s.radius) return true;
        }
        return false;
    }

    // FogOfWarGround처럼 각 소스 위치에 직접 무언가를 해야 하는 경우를 위한 스냅샷.
    public static List<(Vector3 position, float radius)> Snapshot()
    {
        List<(Vector3, float)> result = new List<(Vector3, float)>();
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            Source s = sources[i];
            if (s.transform == null) { sources.RemoveAt(i); continue; }
            result.Add((s.transform.position, s.radius));
        }
        return result;
    }
}
