using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoseMenuController : MonoBehaviour
{
    [Header("Lose Panel")]
    [SerializeField] private GameObject _losePanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _loseText;
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Buttons")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;

    void Start()
    {
        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnRetryClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (_losePanel != null)
            _losePanel.SetActive(false);

        // Listen to match end event
        var roundManager = FindObjectOfType<RoundManager>();
        if (roundManager != null)
            roundManager.OnMatchEnd += OnMatchEnd;
    }

    private void OnMatchEnd(bool playerWon)
    {
        if (!playerWon)
        {
            ShowLoseMenu();
        }
    }

    private void ShowLoseMenu()
    {
        if (_losePanel != null)
            _losePanel.SetActive(true);

        if (_loseText != null)
            _loseText.text = "¡DERROTA!";

        if (_statsText != null)
        {
            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
                _statsText.text = $"Rondas Ganadas: {roundManager.PlayerWins}\nRondas Perdidas: {roundManager.BotWins}";
        }

        Time.timeScale = 0f;
        Debug.Log("[LoseMenu] You lost!");
    }

    private void OnRetryClicked()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.TransitionToScene("Ring");
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.TransitionToScene("MainMenu");
    }
}
