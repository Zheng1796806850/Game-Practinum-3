using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroCutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class Shot
    {
        [Header("Name (just for you to read)")]
        public string shotName;

        [Header("Objects to turn ON in this shot")]
        public GameObject[] enableObjects;

        [Header("Objects to turn OFF in this shot")]
        public GameObject[] disableObjects;

        [Header("Camera target (empty GameObject in the scene)")]
        public Transform cameraTarget;
        public bool smoothMove = true;
        public float moveTime = 1f;

        [Header("Text for this shot")]
        [TextArea(2, 4)]
        public string text;
        public float textStayTime = 3f;
        public bool waitForClick = false;

        [Header("Fade options")]
        public bool fadeFromBlackAtStart = true;
        public bool fadeToBlackAtEnd = true;
    }

    [Header("All shots in order")]
    public Shot[] shots;

    [Header("Scene references")]
    public Camera mainCam;
    public CanvasGroup fadeCanvas;
    public TextMeshProUGUI subtitleText;   
    public GameObject subtitleBackground;  

    [Header("Settings")]
    public float fadeDuration = 1f;
    public string titleSceneName = "TitleScene";

    private bool skipping = false;

    void Start()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 1f;
        }

        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        if (subtitleBackground != null)
        {
            subtitleBackground.SetActive(false);
        }

        StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            skipping = true;
        }
    }

    IEnumerator PlayCutscene()
    {
        for (int i = 0; i < shots.Length; i++)
        {
            Shot s = shots[i];

            if (s.enableObjects != null)
            {
                foreach (var go in s.enableObjects)
                {
                    if (go != null) go.SetActive(true);
                }
            }

            if (s.disableObjects != null)
            {
                foreach (var go in s.disableObjects)
                {
                    if (go != null) go.SetActive(false);
                }
            }

            if (mainCam != null && s.cameraTarget != null)
            {
                if (s.smoothMove)
                    yield return MoveCamera(mainCam.transform, s.cameraTarget.position, s.moveTime);
                else
                    mainCam.transform.position = s.cameraTarget.position;
            }


            if (s.fadeFromBlackAtStart && fadeCanvas != null)
            {
                yield return FadeTo(0f);
            }

            if (subtitleText != null)
            {
                subtitleText.text = s.text;
            }

            if (subtitleBackground != null)
            {
                bool hasText = !string.IsNullOrEmpty(s.text);
                subtitleBackground.SetActive(hasText);
            }

            float timer = 0f;
            while (timer < s.textStayTime)
            {
                if (skipping) break;

                if (s.waitForClick && Input.GetMouseButtonDown(0))
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (subtitleText != null)
            {
                subtitleText.text = "";
            }

            if (subtitleBackground != null)
            {
                subtitleBackground.SetActive(false);
            }

            if (s.fadeToBlackAtEnd && fadeCanvas != null)
            {
                yield return FadeTo(1f);
            }

            if (skipping)
            {
                break;
            }
        }

        SceneManager.LoadScene(titleSceneName);
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerp);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }

    IEnumerator MoveCamera(Transform cam, Vector3 targetPos, float duration)
    {
        Vector3 startPos = cam.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            cam.position = Vector3.Lerp(startPos, targetPos, lerp);
            yield return null;
        }

        cam.position = targetPos;
    }
}
