using UnityEngine;

/// 경로 전체(타일 배열)를 관리한다. 씬에 배치된 타일 마커(자식 오브젝트)로부터 타일 데이터를 만든다.
public class PathManager : MonoBehaviour
{
    [SerializeField] private Transform tileRoot;
    [SerializeField] private float tileGizmoSize = 2f;
    // 벽 배치 판정이 너무 빡빡하다는 피드백으로 추가한 여유값 — Player가 "지나간" 것으로 판정된
    // 타일이라도 이 값만큼 이전 타일까지는 아직 벽 배치를 허용한다(체감상 아직 안 지나간 것처럼
    // 보이는 순간에도 막히는 문제 완화용).
    [SerializeField] private int wallPlacementGraceTiles = 1;

    private TileData[] tiles;
    private int currentPlayerTileIndex;

    public int TileCount => tiles != null ? tiles.Length : 0;

    // Player의 현재 타일 인덱스가 바뀔 때만 발행(같은 값이 매 프레임 들어와도 중복 발행 안 함)
    public event System.Action<int> OnPlayerTileIndexChanged;

    private void Awake()
    {
        BuildTilesFromChildren();
    }

    private void BuildTilesFromChildren()
    {
        Transform root = tileRoot != null ? tileRoot : transform;
        int count = root.childCount;
        tiles = new TileData[count];
        for (int i = 0; i < count; i++)
        {
            Transform child = root.GetChild(i);
            tiles[i] = new TileData(i, child.position);
        }
    }

    public Vector3 GetTilePosition(int tileIndex)
    {
        return tiles[tileIndex].WorldPosition;
    }

    // 월드 좌표에서 가장 가까운 타일의 인덱스를 반환
    public int GetTileIndexAt(Vector3 worldPos)
    {
        int closestIndex = 0;
        float closestSqrDist = float.MaxValue;
        for (int i = 0; i < tiles.Length; i++)
        {
            float sqrDist = (tiles[i].WorldPosition - worldPos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    // PlayerAutoMover가 매 프레임 호출하여 Player의 현재 타일 인덱스를 갱신
    public void SetCurrentPlayerTileIndex(int tileIndex)
    {
        if (currentPlayerTileIndex == tileIndex) return;
        currentPlayerTileIndex = tileIndex;
        OnPlayerTileIndexChanged?.Invoke(tileIndex);
    }

    public int GetCurrentPlayerTileIndex()
    {
        return currentPlayerTileIndex;
    }

    // Player의 현재 타일보다 앞쪽 타일인지 검사 (벽 배치 제약에 사용).
    // wallPlacementGraceTiles만큼은 이미 "지나간" 타일이라도 배치를 허용한다.
    public bool CanPlaceAt(int tileIndex)
    {
        return tileIndex > currentPlayerTileIndex - wallPlacementGraceTiles;
    }

    // 해당 타일에 진입하는 진행 방향(이전 타일 → 이 타일 기준). 첫 타일은 이전 타일이 없어 다음 타일 기준으로 계산한다.
    // 코너 직전 타일에서 다음 타일(코너 이후 방향)을 기준으로 잡으면 방향이 꺾여 슬롯이 경로 한가운데로
    // 어긋나는 문제가 있어, 항상 "지금까지 온 방향"을 기준으로 삼는다.
    public Vector3 GetTileForward(int tileIndex)
    {
        Vector3 direction;
        if (tileIndex > 0)
        {
            direction = tiles[tileIndex].WorldPosition - tiles[tileIndex - 1].WorldPosition;
        }
        else
        {
            int nextIndex = tiles.Length > 1 ? 1 : 0;
            direction = tiles[nextIndex].WorldPosition - tiles[0].WorldPosition;
        }
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
    }

    public bool IsSlotOccupied(int tileIndex, SlotSide side)
    {
        return side == SlotSide.Left ? tiles[tileIndex].LeftSlotOccupied : tiles[tileIndex].RightSlotOccupied;
    }

    public void SetSlotOccupied(int tileIndex, SlotSide side)
    {
        if (side == SlotSide.Left) tiles[tileIndex].LeftSlotOccupied = true;
        else tiles[tileIndex].RightSlotOccupied = true;
    }

    // 재도전 시 호출 — Player 진행 상태와 모든 타일의 슬롯 점유 상태를 처음 상태로 되돌린다.
    public void ResetProgress()
    {
        currentPlayerTileIndex = 0;
        foreach (TileData tile in tiles)
        {
            tile.LeftSlotOccupied = false;
            tile.RightSlotOccupied = false;
        }
    }

    private void OnDrawGizmos()
    {
        Transform root = tileRoot != null ? tileRoot : transform;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < root.childCount; i++)
        {
            Vector3 pos = root.GetChild(i).position;
            Gizmos.DrawWireCube(pos + Vector3.up * 0.05f, new Vector3(tileGizmoSize, 0.1f, tileGizmoSize));
        }
    }
}
