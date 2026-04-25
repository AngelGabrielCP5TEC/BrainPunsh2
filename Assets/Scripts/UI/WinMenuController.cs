using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinMenuController : MonoBehaviour
{
    [Header("Win Panel")]
    [SerializeField] private GameObject _winPanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _winText;
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Buttons")]
    [SerializeField] private Button _nextRoundButton;
    [SerializeField] private Button _mainMenuButton;

    void Start()
    {
        if (_nextRoundButton != null)
            _nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (_winPanel != null)
            _winPanel.SetActive(false);

        // Listen to match end event
        var roundManager = FindObjectOfType<RoundManager>();
        if (roundManager != null)
            roundManager.OnMatchEnd += OnMatchEnd;
    }

    private void OnMatchEnd(bool playerWon)
    {
        if (playerWon)
        {
            ShowWinMenu();
        }
    }

    private void ShowWinMenu()
    {
        if (_winPanel != null)
            _winPanel.SetActive(true);

        if (_winText != null)
            _winText.text = "¡VICTORIA!";

        if (_statsText != null)
        {
            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
                _statsText.text = $"Rondas Ganadas: {roundManager.PlayerWins}";
        }

        Time.timeScale = 0f;
        Debug.Log("[WinMenu] You won!");
    }

    private void OnNextRoundClicked()
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
