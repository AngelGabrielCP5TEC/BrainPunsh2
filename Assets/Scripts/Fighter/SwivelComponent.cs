using UnityEngine;

public class SwivelComponent : MonoBehaviour
{
    [Header("Body Pivot")]
    [SerializeField] private Transform _bodyPivot;
    [SerializeField] private float _yawScale = 30f;
    [SerializeField] private float _pitchScale = 15f;

    [Header("Aim")]
    [SerializeField] private float _aimScaleX = 1f;
    [SerializeField] private float _aimScaleY = 1f;
    // +1 = fighter faces right, -1 = fighter faces left
    [SerializeField] private float _facingSign = 1f;

    [Header("Smoothing")]
    [SerializeField] private float _smoothSpeed = 8f;
    [SerializeField] private float _deadzone = 0.05f;

    public Vector2 Swivel01 { get; private set; }   // smoothed, clamped -1..1
    public Vector2 AimWorld { get; private set; }   // aim in ring frame

    private Vector2 _smoothed;

    public void UpdateSwivel(Vector2 raw)
    {
        Vector2 input = raw;
        if (Mathf.Abs(input.x) < _deadzone) input.x = 0f;
        if (Mathf.Abs(input.y) < _deadzone) input.y = 0f;

        _smoothed = Vector2.Lerp(_smoothed, input, Time.deltaTime * _smoothSpeed);
        Swivel01 = new Vector2(
            Mathf.Clamp(_smoothed.x, -1f, 1f),
            Mathf.Clamp(_smoothed.y, -1f, 1f)
        );

        if (_bodyPivot != null)
            _bodyPivot.localRotation = Quaternion.Euler(
                Swivel01.y * _pitchScale,
                Swivel01.x * _yawScale,
                0f);

        // Horizontal inverted, vertical proportional, corrected for facing direction
        AimWorld = new Vector2(
            Swivel01.x * _aimScaleX * _facingSign,
            -Swivel01.y * _aimScaleY
        );
    }
}
