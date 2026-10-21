using UnityEngine;

/// 경로 위 타일 1칸의 데이터
[System.Serializable]
public class TileData
{
    [SerializeField] private int index;
    [SerializeField] private Vector3 worldPosition;
    [SerializeField] private bool leftSlotOccupied;
    [SerializeField] private bool rightSlotOccupied;

    public int Index => index;
    public Vector3 WorldPosition => worldPosition;

    public bool LeftSlotOccupied
    {
        get => leftSlotOccupied;
        set => leftSlotOccupied = value;
    }

    public bool RightSlotOccupied
    {
        get => rightSlotOccupied;
        set => rightSlotOccupied = value;
    }

    public TileData(int index, Vector3 worldPosition)
    {
        this.index = index;
        this.worldPosition = worldPosition;
        leftSlotOccupied = false;
        rightSlotOccupied = false;
    }
}
