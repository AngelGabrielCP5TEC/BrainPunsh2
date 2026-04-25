using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _settingsPanel;

    void Start()
    {
        if (_playButton != null)
            _playButton.onClick.AddListener(OnPlayClicked);
        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);

        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenu] Loading Ring scene...");
        SceneTransitionManager.Instance.TransitionToScene("Ring");
    }

    private void OnSettingsClicked()
    {
        if (_mainPanel != null)
            _mainPanel.SetActive(false);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quitting application...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
