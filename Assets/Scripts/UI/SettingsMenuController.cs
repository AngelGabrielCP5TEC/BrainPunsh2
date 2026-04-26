using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Settings UI")]
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private TextMeshProUGUI _volumeText;
    [SerializeField] private Button _backButton;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _parentPanel;

    void Start()
    {
        if (_volumeSlider != null)
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);

        // Load saved volume
        _volumeSlider.value = AudioListener.volume;
        UpdateVolumeText();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        UpdateVolumeText();
        Debug.Log($"[Settings] Volume changed to: {AudioListener.volume:F2}");
    }

    private void UpdateVolumeText()
    {
        if (_volumeText != null)
            _volumeText.text = $"Volume: {Mathf.RoundToInt(AudioListener.volume * 100)}%";
    }

    private void OnBackClicked()
    {
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
        if (_parentPanel != null)
            _parentPanel.SetActive(true);

        Debug.Log("[Settings] Back to main menu");
    }
}
