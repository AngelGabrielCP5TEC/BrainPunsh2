using UnityEngine;

// Visualizes where the player's punch will land based on swivel-derived aim.
// Anchored at the target (bot) and offset by SwivelComponent.AimWorld.
[ExecuteAlways]
public class AimReticle : MonoBehaviour
{
    [Header("Source / Target")]
    [SerializeField] private SwivelComponent _swivelSource;   // player swivel
    [SerializeField] private Transform _target;               // bot transform

    [Header("Placement")]
    [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1.5f, -0.3f);
    [SerializeField] private float _aimScale = 0.6f;

    [Header("Visual")]
    [SerializeField] private float _size = 0.18f;
    [SerializeField] private Color _color = new Color(1f, 0.25f, 0.25f, 0.95f);
    [SerializeField] private bool _autoCreateVisual = true;

    private Renderer _visualRenderer;
    private BotOrbitMotion _orbitCache;
    private bool _orbitChecked;

    void OnEnable()
    {
        if (_autoCreateVisual && transform.childCount == 0)
            CreateVisual();
    }

    private void CreateVisual()
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * _size;

        var col = visual.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        }

        _visualRenderer = visual.GetComponent<Renderer>();
        if (_visualRenderer != null) _visualRenderer.sharedMaterial.color = _color;
    }

    void LateUpdate()
    {
        if (_swivelSource == null || _target == null) return;

        if (!_orbitChecked && Application.isPlaying)
        {
            _orbitCache = _target.GetComponent<BotOrbitMotion>();
            _orbitChecked = true;
        }

        // Anchor at the bot's fixed orbit anchor (so the reticle stays still
        // while the bot sways through it) — fall back to its transform.
        Vector3 origin = (_orbitCache != null) ? _orbitCache.Anchor : _target.position;

        Vector2 aim = _swivelSource.AimWorld;
        transform.position = origin + _targetOffset
                           + Vector3.right * aim.x * _aimScale
                           + Vector3.up    * aim.y * _aimScale;
    }
}
