using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class HitReaction : MonoBehaviour
{
    [Header("Body Pivot (auto-found if null)")]
    [SerializeField] private Transform _bodyPivot;

    [Header("Renderers (auto-found under BodyPivot if empty)")]
    [SerializeField] private Renderer[] _flashRenderers;

    [Header("Flash")]
    [SerializeField] private Color _flashColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float _flashHoldTime    = 0.06f;
    [SerializeField] private float _flashRecoverTime = 0.18f;

    [Header("Recoil")]
    [SerializeField] private float _recoilDistance  = 0.20f;  // local -Z push (away from opponent)
    [SerializeField] private float _recoilDuration  = 0.10f;
    [SerializeField] private float _recoverDuration = 0.22f;
    [SerializeField] private float _maxDamageScale  = 25f;    // damage above this = full recoil

    private HealthComponent _health;
    private Color[] _originalColors;
    private Vector3 _pivotRest;
    private Coroutine _flashCo, _recoilCo;

    void Awake()
    {
        _health = GetComponent<HealthComponent>();
        if (_bodyPivot == null) _bodyPivot = transform.Find("BodyPivot");
        if (_bodyPivot != null) _pivotRest = _bodyPivot.localPosition;

        if (_flashRenderers == null || _flashRenderers.Length == 0)
            _flashRenderers = _bodyPivot != null
                ? _bodyPivot.GetComponentsInChildren<Renderer>()
                : new Renderer[0];

        _originalColors = new Color[_flashRenderers.Length];
        for (int i = 0; i < _flashRenderers.Length; i++)
            if (_flashRenderers[i] != null)
                _originalColors[i] = _flashRenderers[i].material.color;

        _health.OnDamaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(float amount)
    {
        if (_flashCo  != null) StopCoroutine(_flashCo);
        if (_recoilCo != null) StopCoroutine(_recoilCo);
        _flashCo  = StartCoroutine(Flash());
        _recoilCo = StartCoroutine(Recoil(amount));
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < _flashRenderers.Length; i++)
            if (_flashRenderers[i] != null) _flashRenderers[i].material.color = _flashColor;

        yield return new WaitForSeconds(_flashHoldTime);

        float t = 0f;
        while (t < _flashRecoverTime)
        {
            t += Time.deltaTime;
            float k = t / _flashRecoverTime;
            for (int i = 0; i < _flashRenderers.Length; i++)
                if (_flashRenderers[i] != null)
                    _flashRenderers[i].material.color = Color.Lerp(_flashColor, _originalColors[i], k);
            yield return null;
        }
        for (int i = 0; i < _flashRenderers.Length; i++)
            if (_flashRenderers[i] != null) _flashRenderers[i].material.color = _originalColors[i];
        _flashCo = null;
    }

    private IEnumerator Recoil(float damage)
    {
        if (_bodyPivot == null) { _recoilCo = null; yield break; }

        float scale = Mathf.Clamp01(damage / _maxDamageScale);
        Vector3 recoilPos = _pivotRest + new Vector3(0, 0, -_recoilDistance * scale);
        Vector3 startPos  = _bodyPivot.localPosition;

        float t = 0f;
        while (t < _recoilDuration)
        {
            t += Time.deltaTime;
            _bodyPivot.localPosition = Vector3.Lerp(startPos, recoilPos, t / _recoilDuration);
            yield return null;
        }

        Vector3 fromPos = _bodyPivot.localPosition;
        t = 0f;
        while (t < _recoverDuration)
        {
            t += Time.deltaTime;
            _bodyPivot.localPosition = Vector3.Lerp(fromPos, _pivotRest, t / _recoverDuration);
            yield return null;
        }
        _bodyPivot.localPosition = _pivotRest;
        _recoilCo = null;
    }
}
