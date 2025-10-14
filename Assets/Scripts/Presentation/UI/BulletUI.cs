using UnityEngine;
using UnityEngine.UI;

public class BulletUI : MonoBehaviour
{
    [Header("Mode Icon")]
    public Image icon;
    public Sprite fireSprite;
    public Sprite iceSprite;

    [Header("Fire Bar")]
    public Slider fireBar;

    [Header("Ice Dots")]
    public Image[] iceDotImages;
    public Sprite iceFullSprite;
    public Sprite iceEmptySprite;
    public Color iceFullColor = Color.white;
    public Color iceEmptyColor = new Color(1f, 1f, 1f, 0.25f);

    public void UpdateIcon(bool useFire)
    {
        if (icon == null) return;

        if (useFire && fireSprite != null) icon.sprite = fireSprite;
        else if (!useFire && iceSprite != null) icon.sprite = iceSprite;
    }

    public void UpdateFireBar(float current, float max)
    {
        if (fireBar == null) return;
        fireBar.maxValue = Mathf.Max(1f, max);
        fireBar.value = Mathf.Clamp(current, 0f, max);
    }

    public void UpdateIceDots(int current, int max)
    {
        if (iceDotImages == null || iceDotImages.Length == 0) return;

        int visible = Mathf.Min(max, iceDotImages.Length);
        for (int i = 0; i < iceDotImages.Length; i++)
        {
            bool withinMax = i < visible;
            if (!withinMax)
            {
                iceDotImages[i].enabled = false;
                continue;
            }

            iceDotImages[i].enabled = true;
            bool filled = i < current;
            if (iceFullSprite != null && iceEmptySprite != null)
                iceDotImages[i].sprite = filled ? iceFullSprite : iceEmptySprite;

            iceDotImages[i].color = filled ? iceFullColor : iceEmptyColor;
        }
    }
}
