using System.Collections;
using UnityEngine;

public class SimpleFadeIn : MonoBehaviour
{
    public SpriteRenderer target;

    public float delay = 2f;

    public float fadeDuration = 1f;

    void OnEnable()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (target == null) yield break;

        Color c = target.color;
        c.a = 0f;
        target.color = c;

        yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;

            c.a = Mathf.Lerp(0f, 1f, lerp);
            target.color = c;

            yield return null;
        }

        c.a = 1f;
        target.color = c;
    }
}
