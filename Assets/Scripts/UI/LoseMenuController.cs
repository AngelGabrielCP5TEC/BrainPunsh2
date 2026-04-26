using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoseMenuController : MonoBehaviour
{
    [Header("Lose UI")]
    [SerializeField] private TextMeshProUGUI _loseText;
    [SerializeField] private TextMeshProUGUI _statsText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;

    void Start()
    {
        Time.timeScale = 1f; // Asegurar que el tiempo está corriendo

        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnRetryClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        ShowLoseScreen();
    }

    private void ShowLoseScreen()
    {
        if (_loseText != null)
            _loseText.text = "¡DERROTA!";

        if (_statsText != null)
        {
            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
                _statsText.text = $"Rondas Ganadas: {roundManager.PlayerWins}\nRondas Perdidas: {roundManager.BotWins}";
        }

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
