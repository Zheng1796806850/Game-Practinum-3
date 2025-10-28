using UnityEngine;
using UnityEngine.UI;

public class BulletUI : MonoBehaviour
{
    [Header("Mode Icon")]
    public Image icon;
    public Sprite boopSprite;
    public Sprite electricSprite;

    [Header("Boop Bar")]
    public Slider boopBar;

    [Header("Electric Bar")]
    public Slider electricBar;

    [Header("Cooldown Wheel")]
    public Image cooldownWheel;

    public void ShowBoopOnly()
    {
        if (boopBar != null) boopBar.gameObject.SetActive(true);
        if (electricBar != null) electricBar.gameObject.SetActive(false);
        if (cooldownWheel != null) cooldownWheel.gameObject.SetActive(true);
        if (icon != null && boopSprite != null) icon.sprite = boopSprite;
    }

    public void ShowElectricOnly()
    {
        if (boopBar != null) boopBar.gameObject.SetActive(false);
        if (electricBar != null) electricBar.gameObject.SetActive(true);
        if (cooldownWheel != null) cooldownWheel.gameObject.SetActive(true);
        if (icon != null && electricSprite != null) icon.sprite = electricSprite;
    }

    public void HideAll()
    {
        if (boopBar != null) boopBar.gameObject.SetActive(false);
        if (electricBar != null) electricBar.gameObject.SetActive(false);
        if (cooldownWheel != null) cooldownWheel.gameObject.SetActive(false);
    }

    public void UpdateBoopBar(float current, float max)
    {
        if (boopBar == null) return;
        boopBar.maxValue = Mathf.Max(1f, max);
        boopBar.value = Mathf.Clamp(current, 0f, max);
    }

    public void UpdateElectricBar01(float ratio01)
    {
        if (electricBar == null) return;
        float t = Mathf.Clamp01(ratio01);
        electricBar.maxValue = 1f;
        electricBar.value = t;
    }

    public void UpdateCooldown01(float ratio01)
    {
        if (cooldownWheel == null) return;
        float t = Mathf.Clamp01(ratio01);
        cooldownWheel.type = Image.Type.Filled;
        cooldownWheel.fillMethod = Image.FillMethod.Radial360;
        cooldownWheel.fillAmount = t;
        if (!cooldownWheel.gameObject.activeSelf) cooldownWheel.gameObject.SetActive(true);
    }
}
