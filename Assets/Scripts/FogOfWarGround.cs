using UnityEngine;

/// 플레이어(및 조명탄 등 추가 시야 소스) 주변 시야 반경만 실시간으로 걷어내는 스타크래프트식 안개.
/// `Custom/FogOfWarShroud` 셰이더가 프래그먼트 단계에서 월드 좌표 기준으로 직접 원을 계산하므로,
/// 이 오브젝트의 Transform 크기와 무관하게 항상 정확한 원으로 보인다(바닥을 넓힐 때 별도로 맞춰줘야
/// 하는 값이 없음 — 예전엔 worldSize/worldCenter를 안 맞추면 원이 타원으로 찌그러지는 문제가 있었음).
/// 상태를 저장하지 않는 완전 동적 방식이라 "한 번 걷힌 안개가 영구히 유지"되지 않고, 시야를 벗어나면
/// 다시 어두워진다(재도전 리셋도 따로 필요 없음 — 매 프레임 현재 위치로 다시 계산될 뿐).
[RequireComponent(typeof(MeshRenderer))]
public class FogOfWarGround : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float visionRadius = 8f;
    // 안개 자체의 색(어둡지만 완전히 불투명하지 않게 해서 바닥/길이 은은하게 비쳐 보이게 함)과
    // 시야 원 경계가 딱딱하게 끊기지 않도록 부드럽게 번지는 폭.
    [SerializeField] private Color fogColor = new Color(0.05f, 0.05f, 0.08f, 0.72f);
    [SerializeField] private float edgeSoftness = 1.5f;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int PlayerPosId = Shader.PropertyToID("_PlayerPos");
    private static readonly int VisionRadiusId = Shader.PropertyToID("_VisionRadius");
    private static readonly int SourceCountId = Shader.PropertyToID("_SourceCount");
    private static readonly int SourcePosId = Shader.PropertyToID("_SourcePos");
    private const int MaxSources = 8;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private readonly Vector4[] sourcePosBuffer = new Vector4[MaxSources];

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    // 예전 방식(텍스처에 영구히 칠하는 안개)의 흔적 — 지금은 상태를 저장하지 않아 매 프레임 현재 위치
    // 기준으로 다시 계산되므로 리셋할 것이 없다. 호출부(StageManager.RetryStage)와의 호환을 위해 남겨둠.
    public void ResetFog()
    {
    }

    private void LateUpdate()
    {
        propertyBlock.SetColor(FogColorId, fogColor);
        propertyBlock.SetFloat(EdgeSoftnessId, edgeSoftness);

        if (player != null)
        {
            propertyBlock.SetVector(PlayerPosId, new Vector4(player.position.x, player.position.z, 0f, 0f));
            propertyBlock.SetFloat(VisionRadiusId, visionRadius);
        }
        else
        {
            propertyBlock.SetVector(PlayerPosId, new Vector4(999999f, 999999f, 0f, 0f));
            propertyBlock.SetFloat(VisionRadiusId, 0f);
        }

        var sources = VisionSourceRegistry.Snapshot();
        int count = Mathf.Min(sources.Count, MaxSources);
        for (int i = 0; i < count; i++)
        {
            (Vector3 position, float radius) = sources[i];
            sourcePosBuffer[i] = new Vector4(position.x, position.z, radius, 0f);
        }
        for (int i = count; i < MaxSources; i++) sourcePosBuffer[i] = Vector4.zero;

        propertyBlock.SetFloat(SourceCountId, count);
        propertyBlock.SetVectorArray(SourcePosId, sourcePosBuffer);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}
