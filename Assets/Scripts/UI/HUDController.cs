using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Slider _playerHPBar;
    [SerializeField] private Slider _playerChargeBar;
    [SerializeField] private Slider _playerFocusBar;
    [SerializeField] private Image  _playerGuardIcon;

    [Header("Bot")]
    [SerializeField] private Slider _botHPBar;

    [Header("Round")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _roundLabel;
    [SerializeField] private TextMeshProUGUI _roundResultText;

    [Header("References")]
    [SerializeField] private FighterController _player;
    [SerializeField] private FighterController _bot;
    [SerializeField] private RoundManager _roundManager;

    private GuardComponent _playerGuard;
    private PunchComponent _playerPunch;

    void Start()
    {
        _playerGuard = _player.GetComponent<GuardComponent>();
        _playerPunch = _player.GetComponent<PunchComponent>();

        _player.GetComponent<HealthComponent>().OnHealthChanged +=
            (cur, max) => SetSlider(_playerHPBar, cur / max);

        _bot.GetComponent<HealthComponent>().OnHealthChanged +=
            (cur, max) => SetSlider(_botHPBar, cur / max);

        _playerPunch.OnChargeUpdated += v => SetSlider(_playerChargeBar, v);

        _roundManager.OnRoundStart  += r => SetRoundLabel(r);
        _roundManager.OnRoundEnd    += won => ShowRoundResult(won ? "Round Won!" : "Round Lost");
        _roundManager.OnMatchEnd    += won => ShowRoundResult(won ? "YOU WIN!" : "YOU LOSE");

        SetRoundLabel(_roundManager.CurrentRound);
        if (_roundResultText) _roundResultText.gameObject.SetActive(false);
    }

    void Update()
    {
        SetSlider(_playerFocusBar, _player.Focus01);
        if (_playerGuardIcon) _playerGuardIcon.enabled = _playerGuard.IsGuarding;
        if (_timerText) _timerText.text = Mathf.CeilToInt(_roundManager.RoundTimeRemaining).ToString();
    }

    private void SetSlider(Slider s, float v) { if (s) s.value = Mathf.Clamp01(v); }

    private void SetRoundLabel(int round)
    {
        if (_roundLabel) _roundLabel.text = $"Round {round}";
        if (_roundResultText) _roundResultText.gameObject.SetActive(false);
    }

    private void ShowRoundResult(string msg)
    {
        if (!_roundResultText) return;
        _roundResultText.text = msg;
        _roundResultText.gameObject.SetActive(true);
    }
}
