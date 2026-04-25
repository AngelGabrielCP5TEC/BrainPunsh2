using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PunchComponent))]
[RequireComponent(typeof(GuardComponent))]
[RequireComponent(typeof(SwivelComponent))]
public class FighterController : MonoBehaviour
{
    [SerializeField] private FighterController _opponent;
    [SerializeField] private CombatResolver _combatResolver;

    public FighterState CurrentState { get; private set; } = FighterState.Idle;
    public float Focus01 { get; private set; }

    private IFighterInput _input;
    private HealthComponent _health;
    private PunchComponent _punch;
    private GuardComponent _guard;
    private SwivelComponent _swivel;
    private float _stunTimer;

    void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _punch = GetComponent<PunchComponent>();
        _guard = GetComponent<GuardComponent>();
        _swivel = GetComponent<SwivelComponent>();
        _input = GetComponent<IFighterInput>();
        _health.OnKO += HandleKO;
    }

    void Update()
    {
        if (_input == null || !GameManager.Instance.IsFighting) return;

        Focus01 = _input.Focus01;
        _swivel.UpdateSwivel(_input.RawSwivel);

        switch (CurrentState)
        {
            case FighterState.Idle:          UpdateIdle();       break;
            case FighterState.Guarding:      UpdateGuarding();   break;
            case FighterState.ChargingPunch: UpdateCharging();   break;
            case FighterState.Striking:      UpdateStriking();   break;
            case FighterState.Recovering:    UpdateRecovering(); break;
            case FighterState.Stunned:       UpdateStunned();    break;
        }
    }

    private void UpdateIdle()
    {
        if (_input.GuardHeld)  { EnterState(FighterState.Guarding);      return; }
        if (_input.PunchHeld)  { EnterState(FighterState.ChargingPunch); return; }
    }

    private void UpdateGuarding()
    {
        if (!_input.GuardHeld) EnterState(FighterState.Idle);
    }

    private void UpdateCharging()
    {
        _punch.UpdateCharge(Time.deltaTime);

        if (_input.PunchReleasedThisFrame)
        {
            // Resolve BEFORE Release — Release() zeros Charge01
            _combatResolver?.Resolve(this, _opponent);
            _punch.Release();
            EnterState(FighterState.Striking);
            return;
        }

        // Cancel charge into guard
        if (_input.GuardHeld)
        {
            _punch.Release();
            EnterState(FighterState.Guarding);
        }
    }

    private void UpdateStriking()
    {
        if (!_punch.IsStriking) EnterState(FighterState.Recovering);
    }

    private void UpdateRecovering()
    {
        if (!_punch.IsRecovering) EnterState(FighterState.Idle);
    }

    private void UpdateStunned()
    {
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0f) EnterState(FighterState.Idle);
    }

    private void EnterState(FighterState next)
    {
        if (CurrentState == FighterState.Guarding) _guard.SetGuard(false);

        CurrentState = next;

        switch (next)
        {
            case FighterState.Guarding:      _guard.SetGuard(true);  break;
            case FighterState.ChargingPunch: _punch.BeginCharge();   break;
        }
    }

    public void EnterStun(float duration)
    {
        _stunTimer = duration;
        EnterState(FighterState.Stunned);
    }

    public void ResetForRound()
    {
        EnterState(FighterState.Idle);
        _health.ResetHP();
        _stunTimer = 0f;
    }

    private void HandleKO() => FindObjectOfType<RoundManager>()?.OnFighterKO(this);
}
