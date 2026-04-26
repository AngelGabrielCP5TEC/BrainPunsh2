using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinMenuController : MonoBehaviour
{
    [Header("Win UI")]
    [SerializeField] private TextMeshProUGUI _winText;
    [SerializeField] private TextMeshProUGUI _statsText;
    [SerializeField] private Button _nextRoundButton;
    [SerializeField] private Button _mainMenuButton;

    void Start()
    {
        Time.timeScale = 1f; // Asegurar que el tiempo está corriendo

        if (_nextRoundButton != null)
            _nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        ShowWinScreen();
    }

    private void ShowWinScreen()
    {
        if (_winText != null)
            _winText.text = "¡VICTORIA!";

        if (_statsText != null)
        {
            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
                _statsText.text = $"Rondas Ganadas: {roundManager.PlayerWins}";
        }

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
