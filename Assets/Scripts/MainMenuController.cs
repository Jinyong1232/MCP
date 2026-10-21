using UnityEngine;
using UnityEngine.SceneManagement;

/// 메인 메뉴 버튼 동작. 시작하기 → 첫 스테이지 로드, 종료 → 게임 종료.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstStageSceneName = "Stage1(Test)";
    // 플레이테스트 설문 링크 — 끝까지 클리어 못 했더라도 "종료" 버튼으로 스스로 그만두는 시점에 열어준다.
    // (StageManager의 최종 클리어 시점 오픈과 별개, 서로 안 겹쳐도 괜찮음 — 둘 다 "플레이를 끝냈다"는 신호)
    [SerializeField] private string surveyFormUrl;

    public void StartGame()
    {
        SceneManager.LoadScene(firstStageSceneName);
    }

    public void QuitGame()
    {
        if (!string.IsNullOrEmpty(surveyFormUrl)) Application.OpenURL(surveyFormUrl);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
