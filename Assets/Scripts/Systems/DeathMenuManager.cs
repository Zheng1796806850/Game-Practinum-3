using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenuManager : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject deathPanel;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void ShowDeathPanel()
    {
        Time.timeScale = 0f;
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
