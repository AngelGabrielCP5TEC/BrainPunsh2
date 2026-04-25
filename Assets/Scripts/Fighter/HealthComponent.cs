using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float _maxHP = 100f;

    public float MaxHP => _maxHP;
    public float CurrentHP { get; private set; }
    public float HP01 => CurrentHP / _maxHP;
    public bool IsKO => CurrentHP <= 0f;

    public event System.Action<float, float> OnHealthChanged;  // current, max
    public event System.Action<float> OnDamaged;               // damage amount (>0 only)
    public event System.Action OnKO;

    void Awake() => CurrentHP = _maxHP;

    public void ResetHP()
    {
        CurrentHP = _maxHP;
        OnHealthChanged?.Invoke(CurrentHP, _maxHP);
    }

    public void ApplyDamage(float amount)
    {
        if (IsKO || amount <= 0f) return;
        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        OnDamaged?.Invoke(amount);
        if (IsKO) OnKO?.Invoke();
    }
}
