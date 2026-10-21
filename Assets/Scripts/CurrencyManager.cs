using UnityEngine;

/// 재화 관리. 스테이지 시작 시 고정 총량을 한 번에 지급한다.
public class CurrencyManager : MonoBehaviour
{
    [SerializeField] private int totalCurrency = 10;

    private int currentCurrency;

    public int CurrentCurrency => currentCurrency;
    public int TotalCurrency => totalCurrency;

    public event System.Action<int> OnCurrencyChanged;

    private void Awake()
    {
        currentCurrency = totalCurrency;
    }

    public bool TrySpend(int amount)
    {
        if (currentCurrency < amount) return false;

        currentCurrency -= amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
        return true;
    }

    // 재도전 시 호출 — 시작 총량으로 되돌린다.
    public void ResetCurrency()
    {
        currentCurrency = totalCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
}
