using UnityEngine;

public class KeyboardFighterInput : MonoBehaviour, IFighterInput
{
    [Header("Keys")]
    [SerializeField] private KeyCode _guardKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _punchKey = KeyCode.Space;
    [SerializeField] private KeyCode _recenterKey = KeyCode.R;
    [SerializeField] private KeyCode _focusCycleKey = KeyCode.F;

    [Header("Mouse Swivel")]
    [SerializeField] private float _mouseSensitivity = 0.05f;
    [SerializeField] private float _swivelClamp = 1f;

    [Header("Focus Simulation")]
    [SerializeField] private float _defaultFocus = 0.5f;
    private static readonly float[] FocusPresets = { 0.2f, 0.5f, 1.0f };
    private int _focusPresetIndex = 1;

    private Vector2 _rawSwivel;
    private bool _prevPunchHeld;

    public bool GuardHeld => Input.GetKey(_guardKey);
    public bool PunchHeld => Input.GetKey(_punchKey);
    public bool PunchReleasedThisFrame { get; private set; }
    public float Focus01 { get; private set; }
    public Vector2 RawSwivel => _rawSwivel;

    void Awake() => Focus01 = _defaultFocus;

    void Update()
    {
        // Raw delta accumulation — simulates head lean (no deltaTime: Mouse X/Y is already per-frame)
        float dx = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float dy = Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _rawSwivel.x = Mathf.Clamp(_rawSwivel.x + dx, -_swivelClamp, _swivelClamp);
        _rawSwivel.y = Mathf.Clamp(_rawSwivel.y + dy, -_swivelClamp, _swivelClamp);

        if (Input.GetKeyDown(_recenterKey)) _rawSwivel = Vector2.zero;

        PunchReleasedThisFrame = _prevPunchHeld && !PunchHeld;
        _prevPunchHeld = PunchHeld;

        if (Input.GetKeyDown(_focusCycleKey))
        {
            _focusPresetIndex = (_focusPresetIndex + 1) % FocusPresets.Length;
            Focus01 = FocusPresets[_focusPresetIndex];
            Debug.Log($"[Input] Focus preset → {Focus01:F1} (F to cycle: Low/Mid/High)");
        }
    }
}
