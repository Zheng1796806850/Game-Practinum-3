using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialEndTrigger : MonoBehaviour
{
    [SerializeField] private GameObject tutorialEndPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        if (tutorialEndPanel != null)
        {
            tutorialEndPanel.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
