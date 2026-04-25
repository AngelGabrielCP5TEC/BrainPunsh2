using UnityEngine;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _text;

    [SerializeField] private FighterController _player;
    [SerializeField] private FighterController _bot;

    private SwivelComponent _swivel;
    private IFighterInput _input;
    private bool _visible;

    void Start()
    {
        _swivel = _player.GetComponent<SwivelComponent>();
        _input  = _player.GetComponent<IFighterInput>();
        if (_panel) _panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _visible = !_visible;
            if (_panel) _panel.SetActive(_visible);
        }

        if (!_visible || _text == null || _input == null) return;

        _text.text =
            "=== INPUT ===\n" +
            $"Guard:   {_input.GuardHeld}\n" +
            $"Punch:   {_input.PunchHeld}\n" +
            $"Release: {_input.PunchReleasedThisFrame}\n" +
            $"Focus01: {_input.Focus01:F2}\n" +
            "\n=== SWIVEL ===\n" +
            $"RawSwivel:  {_input.RawSwivel:F3}\n" +
            $"Swivel01:   {_swivel.Swivel01:F3}\n" +
            $"AimWorld:   {_swivel.AimWorld:F3}\n" +
            "\n=== STATES ===\n" +
            $"Player: {_player.CurrentState}\n" +
            $"Bot:    {_bot.CurrentState}\n" +
            "\n=== FOCUS ===\n" +
            $"Player: {_player.Focus01:F2}\n" +
            $"Bot:    {_bot.Focus01:F2}\n" +
            "\n=== GAME ===\n" +
            $"State: {GameManager.Instance.CurrentState}\n";
    }
}
