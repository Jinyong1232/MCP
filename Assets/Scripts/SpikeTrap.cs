using UnityEngine;

/// 가시함정(상시형). 연속된 두 타일 위에 항상 가시가 솟아 있고, 그 위에 있는 동안 일정 간격으로
/// 피해를 준다(docs/feature-expansion.md 3-3). 자동 이동이라 피할 수 없어 가속·무적으로만 대응한다.
///
/// 판정 방식 — 물리 오버랩 박스(타일 인덱스 비교 대신):
/// PathManager가 보고하는 "현재 타일"은 arrivalThreshold(다음 타일 접근 시점) 기준으로 갱신되는 값이라
/// 실제 캐릭터 위치와 항상 정확히 일치하지는 않고, 코너 근처에서는 부드러운 회전 때문에 실제 경로가
/// 타일 중심선에서 다소 벗어날 수 있다(PlayerAutoMover의 steeringLookahead). 가시함정은 "물리적으로
/// 그 자리 위에 있는가"가 중요하므로, 두 타일의 실제 진행 방향(PathManager.GetTileForward, 벽 슬롯
/// 배치와 동일한 기준)에 맞춰 회전한 OverlapBox로 두 타일 영역을 정확히 덮어 판정한다. 코너 타일에는
/// 배치하지 않는 걸 권장하지만, 배치하더라도 타일 인덱스 방식보다 항상 실제 위치에 더 정확하다.
[RequireComponent(typeof(FogVisibility))]
public class SpikeTrap : MonoBehaviour
{
    [SerializeField] private PathManager pathManager;
    [SerializeField] private int tileIndexA = -1;
    [SerializeField] private int tileIndexB = -1; // tileIndexA 바로 다음 타일(연속 2칸)
    [SerializeField] private float tileLength = 2f;
    [SerializeField] private float boxWidth = 2.2f;
    [SerializeField] private float boxHeight = 2f;

    [Header("판정")]
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private float tickDamage = 4f;
    [SerializeField] private LayerMask targetLayerMask;

    [Header("비주얼(금속 회색 가시)")]
    [SerializeField] private int spikeCount = 8;
    [SerializeField] private float spikeRadius = 0.22f;
    [SerializeField] private float spikeHeight = 0.45f;
    [SerializeField] private Color spikeColor = new Color(0.32f, 0.33f, 0.35f);

    [Header("피해 숫자 팝업(선택)")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Transform damageNumberParent;

    private FogVisibility fogVisibility;
    private Vector3 boxCenter;
    private Quaternion boxRotation;
    private Vector3 boxHalfExtents;
    private float tickTimer;

    private void Awake()
    {
        fogVisibility = GetComponent<FogVisibility>();
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
    }

    private void Start()
    {
        ComputeZone();
        BuildSpikeVisuals();
    }

    private void ComputeZone()
    {
        Vector3 posA = pathManager.GetTilePosition(tileIndexA);
        Vector3 posB = pathManager.GetTilePosition(tileIndexB);
        boxCenter = (posA + posB) * 0.5f + Vector3.up * (boxHeight * 0.5f);

        Vector3 forward = pathManager.GetTileForward(tileIndexB);
        boxRotation = Quaternion.LookRotation(forward);

        float length = Vector3.Distance(posA, posB) + tileLength; // 두 타일 끝에서 끝까지 정확히 덮는 길이
        boxHalfExtents = new Vector3(boxWidth * 0.5f, boxHeight * 0.5f, length * 0.5f);
    }

    private void Update()
    {
        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation, targetLayerMask);
        if (hits.Length == 0)
        {
            tickTimer = 0f;
            return;
        }

        tickTimer += Time.deltaTime;
        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            ApplyTick(hits);
        }
    }

    private void ApplyTick(Collider[] hits)
    {
        foreach (Collider col in hits)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null) continue;

            bool applied = damageable.TakeDamage(tickDamage);
            if (applied) SpawnDamageNumber(col.ClosestPoint(transform.position));
        }
    }

    // 재도전 시 호출 — 틱 타이머만 초기화하면 된다(가시는 항상 솟아있는 상시형이라 별도 상태 없음).
    // FogVisibility 쪽 재공개 여부는 StageManager가 모든 FogVisibility를 일괄로 리셋한다.
    public void ResetTrap()
    {
        tickTimer = 0f;
    }

    private void BuildSpikeVisuals()
    {
        Mesh coneMesh = BuildConeMesh(spikeRadius, spikeHeight, 8);
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", spikeColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.75f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.45f);

        Vector3 posA = pathManager.GetTilePosition(tileIndexA);
        Vector3 posB = pathManager.GetTilePosition(tileIndexB);
        Vector3 forward = (posB - posA).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 zoneCenter = (posA + posB) * 0.5f;
        float zoneLength = Vector3.Distance(posA, posB) + tileLength; // ComputeZone의 판정 길이와 동일 기준

        var renderers = new Renderer[spikeCount];
        for (int i = 0; i < spikeCount; i++)
        {
            // 판정 영역(zoneLength) 전체에 고르게 뿌리되, 살짝 어긋나게 배치해 자연스러운 가시밭처럼 보이게 한다.
            float alongT = (i + 0.5f) / spikeCount;
            Vector3 basePos = zoneCenter + forward * zoneLength * (alongT - 0.5f);
            float sideJitter = ((i % 2 == 0) ? 1f : -1f) * Random.Range(0.15f, 0.5f);
            Vector3 worldPos = basePos + right * sideJitter;

            GameObject spike = new GameObject($"Spike_{i}");
            spike.transform.SetParent(transform, true);
            spike.transform.position = new Vector3(worldPos.x, 0f, worldPos.z);
            spike.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            MeshFilter mf = spike.AddComponent<MeshFilter>();
            mf.sharedMesh = coneMesh;
            MeshRenderer mr = spike.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            renderers[i] = mr;
        }

        // 가시는 절차적으로 여기서 생성되는 거라 FogVisibility.Awake() 시점엔 아직 존재하지 않았다(자동 탐색이
        // 빈 배열로 끝남) — 만든 직후 렌더러 목록을 다시 알려줘서 안개 규칙(시야 밖엔 항상 안 보임)이 적용되게 한다.
        fogVisibility.SetTargetRenderers(renderers);
    }

    // 원뿔 메시를 코드로 직접 만든다(Unity 기본 프리미티브에는 원뿔이 없음) — 밑면 다각형 + 꼭짓점 하나.
    private static Mesh BuildConeMesh(float radius, float height, int segments)
    {
        var mesh = new Mesh { name = "SpikeCone" };
        var vertices = new Vector3[segments + 2]; // 밑면 둘레 + 밑면 중심 + 꼭짓점
        var triangles = new int[segments * 3 * 2];

        vertices[0] = Vector3.zero; // 밑면 중심
        vertices[segments + 1] = new Vector3(0f, height, 0f); // 꼭짓점

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int cur = i + 1;
            int next = (i + 1) % segments + 1;

            // 밑면(아래를 향하도록 감기 순서 반대)
            triangles[t++] = 0;
            triangles[t++] = next;
            triangles[t++] = cur;

            // 옆면
            triangles[t++] = cur;
            triangles[t++] = next;
            triangles[t++] = segments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void SpawnDamageNumber(Vector3 worldPosition)
    {
        if (damageNumberPrefab == null) return;

        if (damageNumberParent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) damageNumberParent = canvas.transform;
        }

        GameObject popupObj = Instantiate(damageNumberPrefab, damageNumberParent);
        DamageNumberPopup popup = popupObj.GetComponent<DamageNumberPopup>();
        popup.Initialize(worldPosition + Vector3.up * 0.5f, tickDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, boxRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
        Gizmos.matrix = prev;
    }
}
