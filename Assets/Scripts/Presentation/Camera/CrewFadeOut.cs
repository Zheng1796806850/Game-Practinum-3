using System.Collections;
using UnityEngine;

public class CrewFadeOut : MonoBehaviour
{
    public SpriteRenderer[] crewSprites;

    public float delayBeforeStart = 1f;

    public float timeBetweenMembers = 0.5f;

    public float fadeDuration = 1f;

    public bool autoPlayOnStart = true;

    void Start()
    {
        if (autoPlayOnStart)
        {
            StartFade();
        }
    }

    public void StartFade()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return new WaitForSeconds(delayBeforeStart);

        for (int i = 0; i < crewSprites.Length; i += 2)
        {
            SpriteRenderer sr1 = crewSprites[i];
            SpriteRenderer sr2 = null;

            if (i + 1 < crewSprites.Length)
            {
                sr2 = crewSprites[i + 1];
            }

            yield return StartCoroutine(FadePair(sr1, sr2));

            if (timeBetweenMembers > 0f)
            {
                yield return new WaitForSeconds(timeBetweenMembers);
            }
        }
    }

    IEnumerator FadePair(SpriteRenderer sr1, SpriteRenderer sr2)
    {
        float t = 0f;

        Color c1 = Color.white;
        float startAlpha1 = 0f;
        if (sr1 != null)
        {
            c1 = sr1.color;
            startAlpha1 = c1.a;
        }

        Color c2 = Color.white;
        float startAlpha2 = 0f;
        if (sr2 != null)
        {
            c2 = sr2.color;
            startAlpha2 = c2.a;
        }

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;

            if (sr1 != null)
            {
                c1.a = Mathf.Lerp(startAlpha1, 0f, lerp);
                sr1.color = c1;
            }

            if (sr2 != null)
            {
                c2.a = Mathf.Lerp(startAlpha2, 0f, lerp);
                sr2.color = c2;
            }

            yield return null;
        }

        if (sr1 != null)
        {
            c1.a = 0f;
            sr1.color = c1;
        }

        if (sr2 != null)
        {
            c2.a = 0f;
            sr2.color = c2;
        }
    }
}
