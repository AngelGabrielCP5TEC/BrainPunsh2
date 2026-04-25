using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [SerializeField] private int _totalRounds = 3;
    [SerializeField] private float _roundDuration = 90f;
    [SerializeField] private float _roundIntroDelay = 3f;

    [SerializeField] private FighterController _player;
    [SerializeField] private FighterController _bot;

    public int CurrentRound { get; private set; } = 1;
    public float RoundTimeRemaining { get; private set; }
    public int PlayerWins { get; private set; }
    public int BotWins { get; private set; }

    public event System.Action<int> OnRoundStart;
    public event System.Action<bool> OnRoundEnd;   // true = player won round
    public event System.Action<bool> OnMatchEnd;   // true = player won match

    void Start() => StartCoroutine(BeginRound());

    void Update()
    {
        if (!GameManager.Instance.IsFighting) return;
        RoundTimeRemaining -= Time.deltaTime;
        if (RoundTimeRemaining <= 0f)
            EndRound(DetermineWinnerByHP());
    }

    public void OnFighterKO(FighterController loser) => EndRound(loser == _bot);

    private IEnumerator BeginRound()
    {
        GameManager.Instance.ChangeState(GameState.RoundIntro);
        RoundTimeRemaining = _roundDuration;
        _player.ResetForRound();
        _bot.ResetForRound();
        OnRoundStart?.Invoke(CurrentRound);
        yield return new WaitForSeconds(_roundIntroDelay);
        GameManager.Instance.ChangeState(GameState.Fighting);
    }

    private void EndRound(bool playerWon)
    {
        if (GameManager.Instance.CurrentState == GameState.RoundEnd ||
            GameManager.Instance.CurrentState == GameState.MatchEnd) return;

        GameManager.Instance.ChangeState(GameState.RoundEnd);
        if (playerWon) PlayerWins++; else BotWins++;
        OnRoundEnd?.Invoke(playerWon);

        int winsNeeded = Mathf.CeilToInt(_totalRounds / 2f);
        bool matchOver = PlayerWins >= winsNeeded || BotWins >= winsNeeded || CurrentRound >= _totalRounds;
        if (matchOver)
        {
            OnMatchEnd?.Invoke(PlayerWins > BotWins);
            GameManager.Instance.ChangeState(GameState.MatchEnd);
        }
        else
        {
            CurrentRound++;
            StartCoroutine(BeginRound());
        }
    }

    private bool DetermineWinnerByHP()
    {
        return _player.GetComponent<HealthComponent>().CurrentHP >=
               _bot.GetComponent<HealthComponent>().CurrentHP;
    }

    public void TogglePause()
    {
        if (GameManager.Instance.CurrentState == GameState.Paused)
            GameManager.Instance.ChangeState(GameState.Fighting);
        else if (GameManager.Instance.IsFighting)
            GameManager.Instance.ChangeState(GameState.Paused);
    }
}
