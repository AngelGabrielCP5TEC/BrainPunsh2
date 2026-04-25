using UnityEngine;

public class PunchComponent : MonoBehaviour
{
    [SerializeField] private float _maxChargeTime = 1.5f;
    [SerializeField] private float _strikeTime = 0.2f;
    [SerializeField] private float _recoverTime = 0.5f;

    public float Charge01 { get; private set; }
    public bool IsCharging { get; private set; }
    public bool IsStriking => _strikeTimer > 0f;
    public bool IsRecovering => _recoverTimer > 0f;
    public bool CanAct => !IsStriking && !IsRecovering;

    private float _chargeTimer;
    private float _strikeTimer;
    private float _recoverTimer;

    public event System.Action<float> OnChargeUpdated;   // Charge01
    public event System.Action<float> OnPunchReleased;   // Charge01 at release

    void Update()
    {
        if (!GameManager.Instance.IsFighting) return;
        if (_strikeTimer > 0f) { _strikeTimer -= Time.deltaTime; return; }
        if (_recoverTimer > 0f) { _recoverTimer -= Time.deltaTime; }
    }

    public void BeginCharge()
    {
        if (!CanAct) return;
        IsCharging = true;
        _chargeTimer = 0f;
        Charge01 = 0f;
    }

    public void UpdateCharge(float dt)
    {
        if (!IsCharging) return;
        _chargeTimer += dt;
        Charge01 = Mathf.Clamp01(_chargeTimer / _maxChargeTime);
        OnChargeUpdated?.Invoke(Charge01);
    }

    public float Release()
    {
        if (!IsCharging) return 0f;
        float released = Charge01;
        IsCharging = false;
        _strikeTimer = _strikeTime;
        _recoverTimer = _recoverTime;
        Charge01 = 0f;
        OnChargeUpdated?.Invoke(0f);
        OnPunchReleased?.Invoke(released);
        return released;
    }
}
