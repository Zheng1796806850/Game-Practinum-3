using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlackScreenFadeOut : MonoBehaviour
{
    public Image blackImage;

    public float delayBeforeFade = 0.5f;

    public float fadeDuration = 1f;

    void Awake()
    {

        if (blackImage == null)
        {
            blackImage = GetComponent<Image>();
        }

        if (blackImage != null)
        {

            Color c = blackImage.color;
            c.a = 1f;
            blackImage.color = c;
        }
    }

    void Start()
    {
        if (blackImage == null) return;

        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        Color c = blackImage.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);

            c.a = Mathf.Lerp(startAlpha, 0f, lerp);
            blackImage.color = c;

            yield return null;
        }

        c.a = 0f;
        blackImage.color = c;

        Destroy(gameObject);
    }
}
