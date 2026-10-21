using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// CurrencyManager의 값을 남은 벽 개수만큼 아이콘 슬롯으로 표시한다.
/// 총 개수가 slotModeMaxCount를 넘으면 아이콘 하나 + 숫자 표기로 전환한다.
/// 벽을 배치해 슬롯이 하나 어두워질 때 짧은 펀치 애니메이션을 준다. 0개가 되면 전부 붉게 고정된다.
public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private Image slotIconPrefab;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject fallbackGroup;
    [SerializeField] private TMP_Text fallbackCountText;
    [SerializeField] private int slotModeMaxCount = 10;
    [SerializeField] private Color unusedColor = Color.white;
    [SerializeField] private Color usedColor = new Color(0.4f, 0.4f, 0.4f, 0.55f);
    [SerializeField] private Color zeroColor = new Color(0.85f, 0.2f, 0.2f, 1f);
    [SerializeField] private float punchDuration = 0.15f;

    private readonly List<Image> slots = new List<Image>();
    private int total;
    private bool useSlotMode;
    private int previousUsed;

    // 현재 모드(슬롯 아이콘 / 폴백 숫자)에서 실제로 보이는 콘텐츠의 RectTransform.
    // 강조 표시 등 바깥에서 "실제 표시 영역"이 필요할 때 이 값을 기준으로 삼을 것 — 부모 컨테이너 크기 아님.
    public RectTransform ActiveContentRect => useSlotMode
        ? slotContainer as RectTransform
        : (fallbackGroup != null ? fallbackGroup.GetComponent<RectTransform>() : null);

    private void Awake()
    {
        total = currencyManager.TotalCurrency;
        useSlotMode = total <= slotModeMaxCount;

        if (useSlotMode)
        {
            if (fallbackGroup != null) fallbackGroup.SetActive(false);
            for (int i = 0; i < total; i++)
            {
                Image icon = Instantiate(slotIconPrefab, slotContainer);
                icon.gameObject.SetActive(true);
                slots.Add(icon);
            }
        }
        else if (fallbackGroup != null)
        {
            fallbackGroup.SetActive(true);
            if (slotContainer != null) slotContainer.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        currencyManager.OnCurrencyChanged += UpdateAmount;
    }

    private void OnDisable()
    {
        currencyManager.OnCurrencyChanged -= UpdateAmount;
    }

    private void Start()
    {
        previousUsed = total - currencyManager.CurrentCurrency;
        Refresh(currencyManager.CurrentCurrency, false);
    }

    private void UpdateAmount(int amount)
    {
        Refresh(amount, true);
    }

    private void Refresh(int amount, bool animate)
    {
        if (useSlotMode)
        {
            int used = total - amount;
            bool zero = amount <= 0;
            for (int i = 0; i < slots.Count; i++)
            {
                bool isUsed = i < used;
                Color target = zero ? zeroColor : (isUsed ? usedColor : unusedColor);
                slots[i].color = target;
            }
            if (animate && used > previousUsed && used - 1 < slots.Count)
            {
                StartCoroutine(Punch(slots[used - 1].transform));
            }
            previousUsed = used;
        }
        else if (fallbackCountText != null)
        {
            fallbackCountText.text = amount.ToString();
        }
    }

    private IEnumerator Punch(Transform t)
    {
        float half = punchDuration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0.75f, elapsed / half);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0.75f, 1f, elapsed / half);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
