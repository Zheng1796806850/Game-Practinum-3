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

    [Header("Gun Slot UI")]
    public Transform gun1Root;
    public Transform gun2Root;
    public Image gun1IconImage;
    public Image gun2IconImage;
    public Image gun1BGImage;
    public Image gun2BGImage;
    [Range(0f, 1f)] public float gun1Alpha = 1f;
    [Range(0f, 1f)] public float gun2Alpha = 0.3f;

    [Header("Gun BG Colors")]
    public Color boopBGColor = Color.white;
    public Color electricBGColor = Color.white;

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
        float clampedMax = Mathf.Max(1f, max);
        float clampedValue = Mathf.Clamp(current, 0f, clampedMax);
        boopBar.maxValue = clampedMax;
        boopBar.value = clampedValue;
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

    public void UpdateGunSlots(bool hasBoopGun, bool hasElectricGun, bool boopIsActiveGun)
    {
        if (gun1Root == null || gun2Root == null) return;

        if (!hasBoopGun && !hasElectricGun)
        {
            SetRootActive(gun1Root, false);
            SetRootActive(gun2Root, false);
            return;
        }

        if (hasBoopGun && !hasElectricGun)
        {
            SetRootActive(gun1Root, true);
            SetRootActive(gun2Root, false);
            SetSlotAlpha(gun1Root, gun1Alpha);
            ApplyGunVisualToSlot(true, true);
            return;
        }

        if (!hasBoopGun && hasElectricGun)
        {
            SetRootActive(gun1Root, true);
            SetRootActive(gun2Root, false);
            SetSlotAlpha(gun1Root, gun1Alpha);
            ApplyGunVisualToSlot(true, false);
            return;
        }

        SetRootActive(gun1Root, true);
        SetRootActive(gun2Root, true);

        bool slot1IsBoop = boopIsActiveGun;
        bool slot2IsBoop = !boopIsActiveGun;

        SetSlotAlpha(gun1Root, gun1Alpha);
        SetSlotAlpha(gun2Root, gun2Alpha);

        ApplyGunVisualToSlot(true, slot1IsBoop);
        ApplyGunVisualToSlot(false, slot2IsBoop);

        if (gun1Root.parent == gun2Root.parent && gun1Root.parent != null)
        {
            gun2Root.SetSiblingIndex(0);
            gun1Root.SetAsLastSibling();
        }
    }

    private void ApplyGunVisualToSlot(bool isSlot1, bool isBoopGun)
    {
        Image iconImage = isSlot1 ? gun1IconImage : gun2IconImage;
        Image bgImage = isSlot1 ? gun1BGImage : gun2BGImage;
        float alpha = isSlot1 ? gun1Alpha : gun2Alpha;

        Sprite iconSprite = isBoopGun ? boopSprite : electricSprite;
        Color baseBG = isBoopGun ? boopBGColor : electricBGColor;

        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }

        if (bgImage != null)
        {
            Color c = baseBG;
            c.a = alpha;
            bgImage.color = c;
        }
    }

    private void SetSlotAlpha(Transform root, float alpha)
    {
        if (root == null) return;
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    private void SetRootActive(Transform root, bool active)
    {
        if (root == null) return;
        if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
    }
}
