using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// 벽 배치 요청을 처리한다. 마우스 클릭으로 WallSlot을 레이캐스트로 감지해 배치 가능 여부를 검사한다.
public class WallPlacementManager : MonoBehaviour
{
    [SerializeField] private PathManager pathManager;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private LayerMask slotLayerMask;
    [SerializeField] private int wallCost = 1;
    [SerializeField] private float raycastMaxDistance = 200f;
    // false면 재화를 전혀 소모하지 않고 무제한으로 배치 가능(스테이지1 튜토리얼 초반 구간용).
    // 기본값은 true(항상 제한) — 제한을 없애려는 씬만 명시적으로 false로 시작한다.
    [SerializeField] private bool wallLimitEnabled = true;

    private readonly List<GameObject> placedWalls = new List<GameObject>();
    private bool slotFilterActive;
    private int filterTileIndex;
    private SlotSide filterSide;

    // 입력 자체를 완전히 막을지 여부(예: 외부 시스템이 안내 문구를 보여주는 동안). 기본은 항상 입력 받음.
    public bool InputLocked { get; set; }

    // 결과 화면(U5)에서 "사용한 벽 개수" 표시에 쓰인다.
    public int WallsPlaced => placedWalls.Count;

    // 스테이지1 튜토리얼(Monster_C 조우)에서 true로 켠다. 그 전까지는 재화를 소모하지 않는다.
    public bool WallLimitEnabled
    {
        get => wallLimitEnabled;
        set => wallLimitEnabled = value;
    }


    public event System.Action<WallSlot> OnWallPlaced;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && InputLocked)
        {
            Debug.Log("[WallPlacementManager] 클릭했지만 InputLocked=true라서 무시됨");
        }
        if (InputLocked) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        // 스킬 버튼 등 UI 위 클릭이면 벽 배치로 이어지지 않게 한다(docs/feature-expansion.md 2-2).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, slotLayerMask))
        {
            Debug.Log($"[WallPlacementManager] 클릭했지만 슬롯 레이어에 아무것도 안 맞음 (screenPos={Mouse.current.position.ReadValue()})");
            return;
        }

        WallSlot slot = hit.collider.GetComponent<WallSlot>();
        if (slot == null)
        {
            Debug.Log($"[WallPlacementManager] 클릭한 콜라이더({hit.collider.name})에 WallSlot 컴포넌트가 없음");
            return;
        }

        if (slotFilterActive && (slot.TileIndex != filterTileIndex || slot.Side != filterSide))
        {
            Debug.Log($"[WallPlacementManager] 슬롯({slot.TileIndex} {slot.Side}) 클릭했지만 지정된 슬롯({filterTileIndex} {filterSide})이 아니라서 무시됨");
            return;
        }

        TryPlaceWall(slot);
    }

    private void TryPlaceWall(WallSlot slot)
    {
        if (!pathManager.CanPlaceAt(slot.TileIndex))
        {
            Debug.Log($"[WallPlacementManager] 타일{slot.TileIndex}은 이미 지나쳐서 배치 불가");
            return;
        }
        if (pathManager.IsSlotOccupied(slot.TileIndex, slot.Side))
        {
            Debug.Log($"[WallPlacementManager] 타일{slot.TileIndex} {slot.Side}은 이미 벽이 세워져 있음");
            return;
        }
        if (wallLimitEnabled && !currencyManager.TrySpend(wallCost))
        {
            Debug.Log($"[WallPlacementManager] 재화 부족으로 배치 불가");
            return;
        }

        pathManager.SetSlotOccupied(slot.TileIndex, slot.Side);
        GameObject wall = Instantiate(wallPrefab, slot.transform.position, slot.transform.rotation);
        placedWalls.Add(wall);
        Debug.Log($"[WallPlacementManager] 타일{slot.TileIndex} {slot.Side}에 벽 배치 성공");
        OnWallPlaced?.Invoke(slot);
    }

    // 특정 슬롯 하나만 배치를 허용하도록 제한한다(그 외 슬롯 클릭은 무시). ClearSlotFilter로 해제.
    public void RestrictToSlot(int tileIndex, SlotSide side)
    {
        slotFilterActive = true;
        filterTileIndex = tileIndex;
        filterSide = side;
    }

    public void ClearSlotFilter()
    {
        slotFilterActive = false;
    }

    // 재도전 시 호출 — 그동안 배치한 벽을 전부 제거한다(슬롯 점유 상태는 PathManager.ResetProgress가 처리).
    public void ClearAllWalls()
    {
        foreach (GameObject wall in placedWalls)
        {
            if (wall != null) Destroy(wall);
        }
        placedWalls.Clear();
    }
}
