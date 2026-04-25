using System.Collections;
using UnityEngine;

// Visual-only glove driver:
//   • Thrust forward on punch release (charge-scaled)
//   • Lerp toward Guard pose (gloves up to face) while GuardComponent.IsGuarding
// Auto-finds gloves named "GloveL"/"GloveR" under BodyPivot if not assigned.
[RequireComponent(typeof(PunchComponent))]
public class GloveAnimator : MonoBehaviour
{
    [Header("Glove Transforms (auto-found if left empty)")]
    [SerializeField] private Transform _gloveLeft;
    [SerializeField] private Transform _gloveRight;

    [Header("Thrust")]
    [SerializeField] private float _thrustDistance   = 1.8f;
    [SerializeField] private float _thrustDuration   = 0.08f;
    [SerializeField] private float _retractDuration  = 0.20f;
    [SerializeField] [Range(0.1f,1f)] private float _minDistanceMul = 0.5f;

    [Header("Guard Pose (local positions while guarding)")]
    [SerializeField] private Vector3 _guardPoseLeft  = new Vector3(-0.22f, 2.00f, 0.50f);
    [SerializeField] private Vector3 _guardPoseRight = new Vector3( 0.22f, 2.00f, 0.50f);
    [SerializeField] private float _poseLerpSpeed    = 14f;

    private PunchComponent _punch;
    private GuardComponent _guard;
    private Vector3 _leftRest, _rightRest;
    private bool _useRight = true;
    private Coroutine _leftCo, _rightCo;

    void Awake()
    {
        _punch = GetComponent<PunchComponent>();
        _guard = GetComponent<GuardComponent>();
        AutoFindGloves();
        if (_gloveLeft  != null) _leftRest  = _gloveLeft.localPosition;
        if (_gloveRight != null) _rightRest = _gloveRight.localPosition;
        _punch.OnPunchReleased += OnPunchReleased;
    }

    void OnDestroy()
    {
        if (_punch != null) _punch.OnPunchReleased -= OnPunchReleased;
    }

    private void AutoFindGloves()
    {
        if (_gloveLeft != null && _gloveRight != null) return;
        var pivot = transform.Find("BodyPivot");
        if (pivot == null) return;
        if (_gloveLeft  == null) _gloveLeft  = pivot.Find("GloveL");
        if (_gloveRight == null) _gloveRight = pivot.Find("GloveR");
    }

    void Update()
    {
        // Lerp toward current rest target (rest or guard pose) when no thrust active
        bool guarding = _guard != null && _guard.IsGuarding;
        if (_leftCo == null && _gloveLeft != null)
        {
            Vector3 target = guarding ? _guardPoseLeft : _leftRest;
            _gloveLeft.localPosition = Vector3.Lerp(
                _gloveLeft.localPosition, target, Time.deltaTime * _poseLerpSpeed);
        }
        if (_rightCo == null && _gloveRight != null)
        {
            Vector3 target = guarding ? _guardPoseRight : _rightRest;
            _gloveRight.localPosition = Vector3.Lerp(
                _gloveRight.localPosition, target, Time.deltaTime * _poseLerpSpeed);
        }
    }

    private void OnPunchReleased(float charge01)
    {
        Transform g = _useRight ? _gloveRight : _gloveLeft;
        Vector3 rest = _useRight ? _rightRest : _leftRest;
        if (g == null) { _useRight = !_useRight; return; }

        Coroutine running = _useRight ? _rightCo : _leftCo;
        if (running != null) StopCoroutine(running);

        bool isRight = _useRight;
        Coroutine co = StartCoroutine(Thrust(g, rest, charge01, isRight));
        if (isRight) _rightCo = co; else _leftCo = co;

        _useRight = !_useRight;
    }

    private IEnumerator Thrust(Transform glove, Vector3 rest, float charge01, bool isRight)
    {
        float dist = _thrustDistance * Mathf.Lerp(_minDistanceMul, 1f, charge01);
        Vector3 forward = rest + new Vector3(0, 0, dist);

        float t = 0f;
        Vector3 start = glove.localPosition;
        while (t < _thrustDuration)
        {
            t += Time.deltaTime;
            glove.localPosition = Vector3.Lerp(start, forward, t / _thrustDuration);
            yield return null;
        }
        glove.localPosition = forward;

        t = 0f;
        Vector3 from = forward;
        while (t < _retractDuration)
        {
            t += Time.deltaTime;
            glove.localPosition = Vector3.Lerp(from, rest, t / _retractDuration);
            yield return null;
        }

        // Hand off to Update() for guard/rest pose lerping
        if (isRight) _rightCo = null; else _leftCo = null;
    }
}
