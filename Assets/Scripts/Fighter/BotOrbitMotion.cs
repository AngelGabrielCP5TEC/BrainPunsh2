using UnityEngine;

// Bot-only motion: orbit-style lateral + depth sway around a fixed anchor,
// plus continuous face-toward-player rotation. The swaying root makes the
// bot a moving target the player must track with the reticle.
public class BotOrbitMotion : MonoBehaviour
{
    [Header("Target (auto-finds 'Player' tag)")]
    [SerializeField] private Transform _player;

    [Header("Lateral Sway")]
    [SerializeField] private float _lateralAmplitude = 1.0f;
    [SerializeField] private float _lateralFrequency = 0.55f;

    [Header("Depth Sway (fore/aft)")]
    [SerializeField] private float _depthAmplitude = 0.25f;
    [SerializeField] private float _depthFrequency = 0.35f;

    [Header("Charge Reaction (juke when player charges)")]
    [SerializeField] private float _jukeStrength = 0.45f;
    [SerializeField] private float _jukeLerpSpeed = 5f;

    [Header("Look At Player")]
    [SerializeField] private bool _faceTarget = true;
    [SerializeField] private float _faceLerpSpeed = 8f;

    public Vector3 Anchor { get; private set; }

    private float _phase;
    private float _juke;
    private PunchComponent _opponentPunch;

    void Awake()
    {
        Anchor = transform.position;
        _phase = Random.Range(0f, Mathf.PI * 2f);

        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
        if (_player != null) _opponentPunch = _player.GetComponent<PunchComponent>();
    }

    void Update()
    {
        bool fighting = GameManager.Instance != null && GameManager.Instance.IsFighting;

        Vector3 sway = Vector3.zero;
        if (fighting)
        {
            float t = Time.time;
            float lat = Mathf.Sin(t * _lateralFrequency * Mathf.PI * 2f + _phase) * _lateralAmplitude;
            float dep = Mathf.Sin(t * _depthFrequency   * Mathf.PI * 2f + _phase * 0.6f) * _depthAmplitude;

            float jukeTarget = 0f;
            if (_opponentPunch != null && _opponentPunch.IsCharging)
                jukeTarget = (Mathf.PerlinNoise(t * 1.8f, _phase) - 0.5f) * 2f * _jukeStrength;
            _juke = Mathf.Lerp(_juke, jukeTarget, Time.deltaTime * _jukeLerpSpeed);

            sway = new Vector3(lat + _juke, 0f, dep);
        }
        else
        {
            _juke = Mathf.Lerp(_juke, 0f, Time.deltaTime * _jukeLerpSpeed);
        }

        transform.position = Anchor + sway;

        if (_faceTarget && _player != null)
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, target, Time.deltaTime * _faceLerpSpeed);
            }
        }
    }
}
