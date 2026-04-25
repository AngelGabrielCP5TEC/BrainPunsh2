using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState _startingState = GameState.RoundIntro;

    public GameState CurrentState { get; private set; }
    public bool IsFighting => CurrentState == GameState.Fighting;

    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentState = _startingState;
    }

    public void ChangeState(GameState next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(next);
    }
}
