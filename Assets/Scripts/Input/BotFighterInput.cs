using UnityEngine;

public class BotFighterInput : MonoBehaviour, IFighterInput
{
    [Header("Difficulty")]
    [SerializeField] private float _reactionDelay = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float _aggressiveness = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _guardTendency = 0.4f;
    [SerializeField] private float _focusBaseline = 0.6f;
    [SerializeField] private float _aimNoise = 0.2f;

    [Header("Punch Timing")]
    [SerializeField] private float _minChargeTime = 0.4f;
    [SerializeField] private float _maxChargeTime = 1.2f;
    [SerializeField] private float _minCooldown = 1.2f;
    [SerializeField] private float _maxCooldown = 2.8f;

    public bool GuardHeld { get; private set; }
    public bool PunchHeld { get; private set; }
    public bool PunchReleasedThisFrame { get; private set; }
    public float Focus01 => _focusBaseline;
    public Vector2 RawSwivel { get; private set; }

    private FighterController _player;
    private bool _prevPunchHeld;
    private bool _isCharging;
    private float _chargeTimer;
    private float _cooldownTimer;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) _player = go.GetComponent<FighterController>();
        _cooldownTimer = _reactionDelay;
    }

    void Update()
    {
        if (!GameManager.Instance.IsFighting)
        {
            GuardHeld = false;
            PunchHeld = false;
            PunchReleasedThisFrame = false;
            return;
        }

        UpdateGuard();
        UpdatePunch();
        UpdateSwivel();

        PunchReleasedThisFrame = _prevPunchHeld && !PunchHeld;
        _prevPunchHeld = PunchHeld;
    }

    private void UpdateGuard()
    {
        bool playerCharging = _player != null &&
                              _player.CurrentState == FighterState.ChargingPunch;
        // Raise guard probability when player is charging
        float chance = playerCharging ? _guardTendency * 3f : _guardTendency;
        if (!GuardHeld && Random.value < chance * Time.deltaTime) GuardHeld = true;
        if (GuardHeld && Random.value < 0.5f * Time.deltaTime) GuardHeld = false;
    }

    private void UpdatePunch()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            PunchHeld = false;
            return;
        }

        if (!_isCharging && Random.value < _aggressiveness * Time.deltaTime)
        {
            _isCharging = true;
            _chargeTimer = Random.Range(_minChargeTime, _maxChargeTime);
        }

        if (_isCharging)
        {
            PunchHeld = true;
            _chargeTimer -= Time.deltaTime;
            if (_chargeTimer <= 0f)
            {
                _isCharging = false;
                PunchHeld = false;
                _cooldownTimer = Random.Range(_minCooldown, _maxCooldown);
            }
        }
    }

    private void UpdateSwivel()
    {
        float t = Time.time;
        RawSwivel = new Vector2(
            Mathf.Sin(t * 0.7f) * 0.3f + Random.Range(-_aimNoise, _aimNoise) * Time.deltaTime,
            Mathf.Sin(t * 0.5f) * 0.2f
        );
    }
}
