using UnityEngine;

public class CombatResolver : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float _baseDamage = 20f;
    [SerializeField] private float _minFocusMultiplier = 0.8f;
    [SerializeField] private float _maxFocusMultiplier = 1.5f;

    [Header("Accuracy")]
    [SerializeField] private float _perfectHitRadius = 0.4f;    // full accuracy inside this
    [SerializeField] private float _maxMissRadius = 1.5f;       // zero accuracy beyond this

    [Header("Guard")]
    [SerializeField] private float _guardFactor = 0.4f;

    [Header("Stun")]
    [SerializeField] private float _stunDamageThreshold = 12f;
    [SerializeField] private float _stunDuration = 0.8f;

    public void Resolve(FighterController attacker, FighterController defender)
    {
        if (attacker == null || defender == null) return;

        var punch  = attacker.GetComponent<PunchComponent>();
        var swivel = attacker.GetComponent<SwivelComponent>();
        var guard  = defender.GetComponent<GuardComponent>();
        var health = defender.GetComponent<HealthComponent>();

        if (punch == null || swivel == null || guard == null || health == null) return;

        float charge         = punch.Charge01;
        float focusMult      = Mathf.Lerp(_minFocusMultiplier, _maxFocusMultiplier, attacker.Focus01);
        float accuracyFactor = ComputeAccuracy(swivel.AimWorld);
        bool  isGuarding     = guard.IsGuarding;
        float gf             = isGuarding ? _guardFactor : 1f;

        float damage = _baseDamage * charge * focusMult * accuracyFactor * gf;

        health.ApplyDamage(damage);

        if (!isGuarding && damage >= _stunDamageThreshold)
            defender.EnterStun(_stunDuration);

        Debug.Log($"[Combat] {attacker.name}→{defender.name} | " +
                  $"dmg={damage:F1} ch={charge:F2} focus={focusMult:F2} " +
                  $"acc={accuracyFactor:F2} defGuard={isGuarding}");
    }

    private float ComputeAccuracy(Vector2 aimWorld)
    {
        float dist = aimWorld.magnitude;
        if (dist <= _perfectHitRadius) return 1f;
        if (dist >= _maxMissRadius) return 0f;
        return 1f - (dist - _perfectHitRadius) / (_maxMissRadius - _perfectHitRadius);
    }
}
