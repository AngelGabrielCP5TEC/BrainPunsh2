using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState _startingState = GameState.MainMenu;

    public GameState CurrentState { get; private set; }
    public bool IsFighting => CurrentState == GameState.Fighting;
    public bool IsPaused => CurrentState == GameState.Paused;

    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        // Scene-bound (NOT DontDestroyOnLoad). Match state restarts cleanly on every RING load.
        // Destroying the *component* (not the GameObject) so siblings like RoundManager/CombatResolver survive.
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        CurrentState = _startingState;
    }

    public void ChangeState(GameState next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
        OnStateChanged?.Invoke(next);
    }
}
