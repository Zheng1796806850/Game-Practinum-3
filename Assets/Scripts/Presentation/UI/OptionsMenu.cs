using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    [Header("Jump Key")]
    public Button jumpKeyWButton;
    public Button jumpKeySpaceButton;
    public Image jumpKeyWImage;
    public Image jumpKeySpaceImage;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.7f, 1f, 0.7f, 1f);

    [Header("Audio")]
    public Slider masterVolumeSlider;
    public AudioMixer masterMixer;
    public string masterVolumeParameter = "Master";

    private const string JumpKeyPrefKey = "JumpKey";
    private const string MasterVolumePrefKey = "MasterVolume";

    private bool loading = false;

    void Awake()
    {
        SetupListeners();
    }

    void OnEnable()
    {
        LoadSettingsToUI();
    }

    private void SetupListeners()
    {
        if (jumpKeyWButton != null)
        {
            jumpKeyWButton.onClick.RemoveListener(OnClickJumpKeyW);
            jumpKeyWButton.onClick.AddListener(OnClickJumpKeyW);
        }

        if (jumpKeySpaceButton != null)
        {
            jumpKeySpaceButton.onClick.RemoveListener(OnClickJumpKeySpace);
            jumpKeySpaceButton.onClick.AddListener(OnClickJumpKeySpace);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
    }

    private void LoadSettingsToUI()
    {
        loading = true;

        string jumpKey = PlayerPrefs.GetString(JumpKeyPrefKey, "Space");
        if (jumpKey == "W")
            ApplyJumpKeyUI("W");
        else
            ApplyJumpKeyUI("Space");

        float volume = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f);
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = volume;
        }
        ApplyMasterVolume(volume);

        loading = false;
    }

    public void OnClickJumpKeyW()
    {
        SetJumpKey("W");
    }

    public void OnClickJumpKeySpace()
    {
        SetJumpKey("Space");
    }

    private void SetJumpKey(string key)
    {
        PlayerPrefs.SetString(JumpKeyPrefKey, key);
        PlayerPrefs.Save();
        ApplyJumpKeyUI(key);
    }

    private void ApplyJumpKeyUI(string key)
    {
        if (jumpKeyWImage == null && jumpKeyWButton != null)
            jumpKeyWImage = jumpKeyWButton.targetGraphic as Image;
        if (jumpKeySpaceImage == null && jumpKeySpaceButton != null)
            jumpKeySpaceImage = jumpKeySpaceButton.targetGraphic as Image;

        if (jumpKeyWImage != null)
            jumpKeyWImage.color = key == "W" ? selectedColor : normalColor;

        if (jumpKeySpaceImage != null)
            jumpKeySpaceImage.color = key == "Space" ? selectedColor : normalColor;
    }

    public void OnMasterVolumeChanged(float value)
    {
        if (!loading)
        {
            PlayerPrefs.SetFloat(MasterVolumePrefKey, value);
            PlayerPrefs.Save();
        }
        ApplyMasterVolume(value);
    }

    private void ApplyMasterVolume(float value)
    {
        if (masterMixer == null) return;
        float v = Mathf.Clamp01(value);
        float dB = v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
        masterMixer.SetFloat(masterVolumeParameter, dB);
    }
}
