using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// PlayerHealth의 값을 체력 바(하트 아이콘 + 게이지 + 숫자)로 표시한다.
/// 피해를 입으면 게이지가 0.2초에 걸쳐 부드럽게 줄어들고, 남은 비율에 따라 색이 바뀐다.
/// 30% 이하로 내려가면 바가 천천히 맥박치듯 깜빡인다.
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthNumberText;
    [SerializeField] private float fillLerpDuration = 0.2f;
    [SerializeField] private Color highHealthColor = new Color(0.36f, 0.78f, 0.36f);
    [SerializeField] private Color midHealthColor = new Color(0.95f, 0.6f, 0.15f);
    [SerializeField] private Color lowHealthColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float midHealthThreshold = 0.6f;
    [SerializeField] private float lowHealthPulseSpeed = 3f;

    private Coroutine fillRoutine;
    private bool isLowHealth;

    private void OnEnable()
    {
        playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        ApplyImmediate(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void HandleHealthChanged(float current, float max)
    {
        float pct = max > 0f ? current / max : 0f;
        if (healthNumberText != null) healthNumberText.text = current.ToString("0");
        UpdateColorState(pct);

        if (fillRoutine != null) StopCoroutine(fillRoutine);
        fillRoutine = StartCoroutine(LerpFill(pct));
    }

    private void ApplyImmediate(float current, float max)
    {
        float pct = max > 0f ? current / max : 0f;
        if (healthNumberText != null) healthNumberText.text = current.ToString("0");
        UpdateColorState(pct);
        if (healthSlider != null) healthSlider.value = pct;
    }

    private IEnumerator LerpFill(float target)
    {
        float start = healthSlider != null ? healthSlider.value : target;
        float t = 0f;
        while (t < fillLerpDuration)
        {
            t += Time.deltaTime;
            if (healthSlider != null) healthSlider.value = Mathf.Lerp(start, target, t / fillLerpDuration);
            yield return null;
        }
        if (healthSlider != null) healthSlider.value = target;
    }

    private void UpdateColorState(float pct)
    {
        isLowHealth = pct <= lowHealthThreshold;
        if (fillImage == null) return;

        if (pct > midHealthThreshold) fillImage.color = highHealthColor;
        else if (pct > lowHealthThreshold) fillImage.color = midHealthColor;
        else fillImage.color = lowHealthColor;
    }

    private void Update()
    {
        if (!isLowHealth || fillImage == null) return;

        float pulse = (Mathf.Sin(Time.time * lowHealthPulseSpeed) + 1f) * 0.5f;
        Color c = lowHealthColor;
        c.a = Mathf.Lerp(0.55f, 1f, pulse);
        fillImage.color = c;
    }
}
