using UnityEngine;

public class GuardComponent : MonoBehaviour
{
    [SerializeField] private float _guardDamageFactor = 0.4f;

    public bool IsGuarding { get; private set; }
    public float GuardFactor => IsGuarding ? _guardDamageFactor : 1f;

    public void SetGuard(bool held) => IsGuarding = held;
}
