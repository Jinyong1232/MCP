using UnityEngine;

/// 몬스터가 시야에 처음 발견될 때(FogVisibility.OnRevealed) 튜토리얼 설명 팝업을 한 번 띄운다.
/// tutorialKey가 같으면 같은 타입 몬스터가 여러 마리 있어도 전체 세션에서 한 번만 뜬다.
[RequireComponent(typeof(FogVisibility))]
public class MonsterTutorialTrigger : MonoBehaviour
{
    [SerializeField] private string tutorialKey;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string body;

    private FogVisibility fogVisibility;

    private void Awake()
    {
        fogVisibility = GetComponent<FogVisibility>();
    }

    private void OnEnable()
    {
        fogVisibility.OnRevealed += HandleRevealed;
    }

    private void OnDisable()
    {
        fogVisibility.OnRevealed -= HandleRevealed;
    }

    private void HandleRevealed()
    {
        if (TutorialPopupManager.Instance != null)
        {
            TutorialPopupManager.Instance.TryShow(tutorialKey, title, body);
        }
    }
}
