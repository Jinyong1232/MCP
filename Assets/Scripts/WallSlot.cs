using UnityEngine;

/// 타일 좌/우의 벽 배치 슬롯. 클릭(레이캐스트) 대상이 되는 데이터만 보유한다.
public class WallSlot : MonoBehaviour
{
    [SerializeField] private int tileIndex;
    [SerializeField] private SlotSide side;

    public int TileIndex => tileIndex;
    public SlotSide Side => side;

    public void Initialize(int tileIndex, SlotSide side)
    {
        this.tileIndex = tileIndex;
        this.side = side;
    }
}
