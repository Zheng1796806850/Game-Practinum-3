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

    [Header("Camera")]
    public Toggle useMouseLookOffsetToggle;
    public Slider mouseOffsetLerpSlider;
    public Toggle enableScrollZoomToggle;

    private const string JumpKeyPrefKey = "JumpKey";
    private const string MasterVolumePrefKey = "MasterVolume";
    private const string UseMouseLookOffsetPrefKey = "UseMouseLookOffset";
    private const string MouseOffsetLerpPrefKey = "MouseOffsetLerp";
    private const string EnableScrollZoomPrefKey = "EnableScrollZoom";

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

        if (useMouseLookOffsetToggle != null)
        {
            useMouseLookOffsetToggle.onValueChanged.RemoveListener(OnUseMouseLookOffsetChanged);
            useMouseLookOffsetToggle.onValueChanged.AddListener(OnUseMouseLookOffsetChanged);
        }

        if (mouseOffsetLerpSlider != null)
        {
            mouseOffsetLerpSlider.onValueChanged.RemoveListener(OnMouseOffsetLerpChanged);
            mouseOffsetLerpSlider.onValueChanged.AddListener(OnMouseOffsetLerpChanged);
        }

        if (enableScrollZoomToggle != null)
        {
            enableScrollZoomToggle.onValueChanged.RemoveListener(OnEnableScrollZoomChanged);
            enableScrollZoomToggle.onValueChanged.AddListener(OnEnableScrollZoomChanged);
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

        int useOffsetInt = PlayerPrefs.GetInt(UseMouseLookOffsetPrefKey, 1);
        bool useOffset = useOffsetInt != 0;
        float mouseLerp = PlayerPrefs.GetFloat(MouseOffsetLerpPrefKey, 0.2f);
        mouseLerp = Mathf.Clamp01(mouseLerp);
        int enableScrollInt = PlayerPrefs.GetInt(EnableScrollZoomPrefKey, 1);
        bool enableScroll = enableScrollInt != 0;

        if (useMouseLookOffsetToggle != null)
        {
            useMouseLookOffsetToggle.isOn = useOffset;
        }

        if (mouseOffsetLerpSlider != null)
        {
            mouseOffsetLerpSlider.value = mouseLerp;
        }

        if (enableScrollZoomToggle != null)
        {
            enableScrollZoomToggle.isOn = enableScroll;
        }

        ApplyCameraSettings(useOffset, mouseLerp, enableScroll);

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

    private void OnUseMouseLookOffsetChanged(bool value)
    {
        if (loading) return;
        PlayerPrefs.SetInt(UseMouseLookOffsetPrefKey, value ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCameraSettingsFromCurrentUI();
    }

    private void OnMouseOffsetLerpChanged(float value)
    {
        if (loading) return;
        float v = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MouseOffsetLerpPrefKey, v);
        PlayerPrefs.Save();
        ApplyCameraSettingsFromCurrentUI();
    }

    private void OnEnableScrollZoomChanged(bool value)
    {
        if (loading) return;
        PlayerPrefs.SetInt(EnableScrollZoomPrefKey, value ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCameraSettingsFromCurrentUI();
    }

    private void ApplyCameraSettingsFromCurrentUI()
    {
        bool useOffset = useMouseLookOffsetToggle != null ? useMouseLookOffsetToggle.isOn : PlayerPrefs.GetInt(UseMouseLookOffsetPrefKey, 1) != 0;
        float mouseLerp = mouseOffsetLerpSlider != null ? mouseOffsetLerpSlider.value : PlayerPrefs.GetFloat(MouseOffsetLerpPrefKey, 0.2f);
        mouseLerp = Mathf.Clamp01(mouseLerp);
        bool enableScroll = enableScrollZoomToggle != null ? enableScrollZoomToggle.isOn : PlayerPrefs.GetInt(EnableScrollZoomPrefKey, 1) != 0;
        ApplyCameraSettings(useOffset, mouseLerp, enableScroll);
    }

    private void ApplyCameraSettings(bool useOffset, float mouseLerp, bool enableScroll)
    {
        CameraFollow[] cameras = Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            CameraFollow cam = cameras[i];
            if (cam == null) continue;
            cam.useMouseLookOffset = useOffset;
            cam.mouseOffsetLerp = mouseLerp;
            cam.enableScrollZoom = enableScroll;
        }
    }
}
