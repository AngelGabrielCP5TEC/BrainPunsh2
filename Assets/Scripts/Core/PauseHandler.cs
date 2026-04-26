using UnityEngine;

public class PauseHandler : MonoBehaviour
{
    [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;
    [SerializeField] private RoundManager _roundManager;

    void Update()
    {
        if (Input.GetKeyDown(_pauseKey))
        {
            if (GameManager.Instance.CurrentState == GameState.Fighting ||
                GameManager.Instance.CurrentState == GameState.Paused)
            {
                _roundManager?.TogglePause();
            }
        }
    }
}
