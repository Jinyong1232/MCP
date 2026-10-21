using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// 결과 화면(클리어/실패)의 연출을 담당한다. 문구·버튼 자체는 StageManager가 그대로 처리하고,
/// 이 스크립트는 패널 전체의 표시/숨김과 클리어 시 항목 순차 등장+카운트업, 실패 시 화면 회색화를 맡는다.
public class ResultPanelUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private Image backdrop;
    [SerializeField] private CanvasGroup clearGroup;
    [SerializeField] private CanvasGroup failGroup;
    [SerializeField] private Color backdropClearColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color backdropFailColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    [SerializeField] private float backdropFadeDuration = 1.2f;
    [SerializeField] private float countUpDuration = 0.6f;

    [Header("클리어 — 순차 등장 행 (위에서부터 0.2초 간격)")]
    [SerializeField] private CanvasGroup healthRow;
    [SerializeField] private CanvasGroup wallsRow;
    [SerializeField] private CanvasGroup timeRow;
    [SerializeField] private CanvasGroup starRow;
    [SerializeField] private float rowStagger = 0.2f;

    [Header("클리어 — 체력")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthNumberText;

    [Header("클리어 — 사용한 벽")]
    [SerializeField] private TMP_Text wallsText;
    [SerializeField] private Color wallsNormalColor = new Color(0.35f, 0.3f, 0.22f);
    [SerializeField] private Color wallsEmphasisColor = new Color(0.78f, 0.42f, 0.06f);

    [Header("클리어 — 시간(선택 표시)")]
    [SerializeField] private TMP_Text timeText;

    [Header("실패")]
    [SerializeField] private TMP_Text failProgressText;

    private Coroutine playRoutine;
    private Coroutine backdropRoutine;

    private void Awake()
    {
        HideAll();
    }

    // 재도전 등으로 결과 화면을 완전히 숨길 때 호출
    public void HideAll()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        if (backdropRoutine != null) StopCoroutine(backdropRoutine);

        gameObject.SetActive(false);
        if (clearGroup != null) clearGroup.gameObject.SetActive(false);
        if (failGroup != null) failGroup.gameObject.SetActive(false);

        SetAlpha(healthRow, 0f);
        SetAlpha(wallsRow, 0f);
        SetAlpha(timeRow, 0f);
        SetAlpha(starRow, 0f);

        if (backdrop != null)
        {
            Color c = backdropClearColor;
            c.a = 0f;
            backdrop.color = c;
        }
    }

    public void PlayClear(float currentHealth, float maxHealth, int wallsUsed, int wallsTotal, float clearTimeSeconds)
    {
        gameObject.SetActive(true);
        if (clearGroup != null) clearGroup.gameObject.SetActive(true);
        if (failGroup != null) failGroup.gameObject.SetActive(false);

        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayClearRoutine(currentHealth, maxHealth, wallsUsed, wallsTotal, clearTimeSeconds));
    }

    public void PlayFail(float reachedPct)
    {
        gameObject.SetActive(true);
        if (clearGroup != null) clearGroup.gameObject.SetActive(false);
        if (failGroup != null) failGroup.gameObject.SetActive(true);

        if (failProgressText != null)
        {
            failProgressText.text = $"여기까지 도달했습니다 ({Mathf.RoundToInt(reachedPct * 100f)}%)";
        }

        if (backdropRoutine != null) StopCoroutine(backdropRoutine);
        backdropRoutine = StartCoroutine(FadeBackdrop(backdropFailColor));
    }

    private IEnumerator PlayClearRoutine(float currentHealth, float maxHealth, int wallsUsed, int wallsTotal, float clearTimeSeconds)
    {
        if (backdropRoutine != null) StopCoroutine(backdropRoutine);
        backdropRoutine = StartCoroutine(FadeBackdrop(backdropClearColor));

        SetAlpha(healthRow, 0f);
        SetAlpha(wallsRow, 0f);
        SetAlpha(timeRow, 0f);
        SetAlpha(starRow, 0f);

        SetAlpha(healthRow, 1f);
        StartCoroutine(CountUpHealth(currentHealth, maxHealth));
        yield return new WaitForSecondsRealtime(rowStagger);

        SetAlpha(wallsRow, 1f);
        StartCoroutine(CountUpWalls(wallsUsed, wallsTotal));
        yield return new WaitForSecondsRealtime(rowStagger);

        SetAlpha(timeRow, 1f);
        StartCoroutine(CountUpTime(clearTimeSeconds));
        yield return new WaitForSecondsRealtime(rowStagger);

        // 별 등급제는 향후 확장(design.md 8장) — 자리만 비워둔 채로 등장만 시킨다.
        SetAlpha(starRow, 1f);
    }

    private IEnumerator CountUpHealth(float current, float max)
    {
        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float shown = Mathf.Lerp(0f, current, Mathf.Clamp01(t / countUpDuration));
            ApplyHealth(shown, max);
            yield return null;
        }
        ApplyHealth(current, max);
    }

    private void ApplyHealth(float value, float max)
    {
        if (healthSlider != null) healthSlider.value = max > 0f ? value / max : 0f;
        if (healthNumberText != null) healthNumberText.text = Mathf.RoundToInt(value).ToString();
    }

    private IEnumerator CountUpWalls(int used, int total)
    {
        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;
            int shown = Mathf.RoundToInt(Mathf.Lerp(0, used, Mathf.Clamp01(t / countUpDuration)));
            if (wallsText != null) wallsText.text = $"{total}개 중 {shown}개 사용";
            yield return null;
        }
        if (wallsText != null)
        {
            wallsText.text = $"{total}개 중 {used}개 사용";
            float usedRatio = total > 0 ? (float)used / total : 0f;
            // 적게 쓸수록 강조색(골드)에 가깝게, 많이 쓸수록 기본색
            wallsText.color = Color.Lerp(wallsEmphasisColor, wallsNormalColor, usedRatio);
        }
    }

    private IEnumerator CountUpTime(float seconds)
    {
        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float shown = Mathf.Lerp(0f, seconds, Mathf.Clamp01(t / countUpDuration));
            if (timeText != null) timeText.text = $"클리어 시간 {shown:0.0}초";
            yield return null;
        }
        if (timeText != null) timeText.text = $"클리어 시간 {seconds:0.0}초";
    }

    private IEnumerator FadeBackdrop(Color target)
    {
        if (backdrop == null) yield break;

        Color start = backdrop.color;
        float t = 0f;
        while (t < backdropFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            backdrop.color = Color.Lerp(start, target, t / backdropFadeDuration);
            yield return null;
        }
        backdrop.color = target;
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }
}
