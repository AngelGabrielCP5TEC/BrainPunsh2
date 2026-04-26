using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Pause Panel")]
    [SerializeField] private GameObject _pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Settings")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Button _settingsBackButton;

    private RoundManager _roundManager;

    void Start()
    {
        _roundManager = FindObjectOfType<RoundManager>();

        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(OnResumeClicked);
        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_settingsBackButton != null)
            _settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

        if (_pausePanel != null)
            _pausePanel.SetActive(false);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);

        GameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Paused)
        {
            ShowPauseMenu();
        }
        else if (newState == GameState.Fighting || newState == GameState.RoundIntro)
        {
            HidePauseMenu();
        }
    }

    private void ShowPauseMenu()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(true);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
        Time.timeScale = 0f;
        Debug.Log("[PauseMenu] Game paused");
    }

    private void HidePauseMenu()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Game resumed");
    }

    private void OnResumeClicked()
    {
        _roundManager?.TogglePause();
    }

    private void OnSettingsClicked()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(true);
    }

    private void OnSettingsBackClicked()
    {
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
        if (_pausePanel != null)
            _pausePanel.SetActive(true);
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Returning to main menu...");
        SceneTransitionManager.Instance.TransitionToScene("MainMenu");
    }
}
