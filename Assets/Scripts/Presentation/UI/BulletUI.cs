using UnityEngine;
using UnityEngine.UI;

public class BulletUI : MonoBehaviour
{
    public Image icon;
    public Sprite fireSprite;
    public Sprite iceSprite;

    public void UpdateIcon(bool useFire)
    {
        if (icon == null) return;

        if (useFire && fireSprite != null)
        {
            icon.sprite = fireSprite;
        }
        else if (!useFire && iceSprite != null)
        {
            icon.sprite = iceSprite;
        }
    }
}