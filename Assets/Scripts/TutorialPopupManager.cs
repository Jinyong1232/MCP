using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// 특정 이벤트가 발생했을 때 게임을 일시정지하고 설명 팝업을 띄우는 범용 튜토리얼 시스템.
/// 이 클래스는 "언제 띄울지"를 모른다 — 호출하는 쪽(MonsterTutorialTrigger 등)이 키/제목/본문만 넘기면
/// 언제든 새로운 트리거를 추가할 수 있다. 같은 키는 세션 중 한 번만(재도전해도 다시 뜨지 않음) 표시한다.
public class TutorialPopupManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;

    public static TutorialPopupManager Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    private readonly HashSet<string> shownKeys = new HashSet<string>();
    private float previousTimeScale = 1f;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        bool dismissPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
        if (dismissPressed) Close();
    }

    // key가 처음 보는 것이면 팝업을 띄우고 true, 이미 본 적 있으면 아무 것도 안 하고 false를 반환한다.
    public bool TryShow(string key, string title, string body)
    {
        if (string.IsNullOrEmpty(key) || shownKeys.Contains(key)) return false;

        shownKeys.Add(key);
        Show(title, body);
        return true;
    }

    private void Show(string title, string body)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (panel != null) panel.SetActive(true);

        IsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
        Time.timeScale = previousTimeScale;
    }
}
