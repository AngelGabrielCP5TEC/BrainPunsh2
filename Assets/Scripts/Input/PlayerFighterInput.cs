using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Unified player input. Switch source via Inspector enum or runtime hotkey (F2).
//
// Modes:
//   Keyboard — full keyboard/mouse control (LShift guard, Space punch, mouse swivel, F focus, R recenter)
//   Socket   — Unicorn headset over TCP. Keyboard inputs still work as a coexisting
//              fallback so you can test/override even with the headset connected.
//
// Protocol: "x,y,focus,imaginary\n" at ~25 Hz.
//   x,y       — swivel cursor in [-1, 1]
//   focus     — engagement index 0..100 (mapped to Focus01 = focus / 100)
//   imaginary — tensing state: 0 idle | 1 charge punch | 2 guard up
public class PlayerFighterInput : MonoBehaviour, IFighterInput
{
    public enum InputMode { Keyboard, Socket }

    [Header("Mode")]
    [SerializeField] private InputMode _mode = InputMode.Keyboard;
    [SerializeField] private KeyCode _toggleModeKey = KeyCode.F2;

    [Header("Keys (used in Keyboard mode and as fallback in Socket mode)")]
    [SerializeField] private KeyCode _guardKey      = KeyCode.LeftShift;
    [SerializeField] private KeyCode _punchKey      = KeyCode.Space;
    [SerializeField] private KeyCode _recenterKey   = KeyCode.R;
    [SerializeField] private KeyCode _focusCycleKey = KeyCode.F;

    [Header("Mouse Swivel (Keyboard mode)")]
    [SerializeField] private float _mouseSensitivity = 0.05f;
    [SerializeField] private float _swivelClamp      = 1f;

    [Header("Focus Simulation (Keyboard mode)")]
    [SerializeField] private float _defaultFocus = 0.5f;
    private static readonly float[] FocusPresets = { 0.2f, 0.5f, 1.0f };
    private int _focusPresetIndex = 1;

    [Header("Socket (Unicorn TCP)")]
    [SerializeField] private string _host = "127.0.0.1";
    [SerializeField] private int    _port = 1234;
    [SerializeField] private bool   _autoReconnect = true;
    [SerializeField] private float  _reconnectDelay = 2f;

    // ── IFighterInput ────────────────────────────────────────────────
    // PunchHeld/GuardHeld are computed each Update() from keyboard + (in Socket mode)
    // the motor-imagery state. Keyboard always works as a coexisting fallback.
    public bool      GuardHeld              { get; private set; }
    public bool      PunchHeld              { get; private set; }
    public bool      PunchReleasedThisFrame { get; private set; }
    public float     Focus01                { get; private set; }
    public Vector2   RawSwivel              { get; private set; }

    // ── Extra socket fields (not in IFighterInput) ───────────────────
    // Motor-imagination / "tensing" state from the headset:
    //   0 = idle | 1 = charge punch | 2 = guard up
    // Steady value (recomputed ~0.5 s on Python side, sent every TCP packet).
    public int       MotorImagery           { get; private set; }

    // ── Mode state ───────────────────────────────────────────────────
    public InputMode CurrentMode => _mode;

    // ── Keyboard state ───────────────────────────────────────────────
    private Vector2 _kbSwivel;
    private bool    _prevPunchHeld;

    // ── Socket state ─────────────────────────────────────────────────
    private TcpClient     _client;
    private NetworkStream _stream;
    private Thread        _rxThread;
    private volatile bool _socketRunning;
    private readonly object _lock = new object();
    private float _sockX, _sockY, _sockConc;
    private int   _sockImagery;
    private bool  _socketHasData;
    private float _lastConnectAttempt;
    private bool  _socketConnected;
    private bool  _connectFailureLogged;

    void Awake()
    {
        Focus01 = _defaultFocus;
        if (_mode == InputMode.Socket) StartSocket();
    }

    void OnDestroy()      => StopSocket();
    void OnApplicationQuit() => StopSocket();

    void Update()
    {
        // Hotkey toggle
        if (Input.GetKeyDown(_toggleModeKey))
        {
            SetMode(_mode == InputMode.Keyboard ? InputMode.Socket : InputMode.Keyboard);
        }

        // Recenter works in either mode
        if (Input.GetKeyDown(_recenterKey)) _kbSwivel = Vector2.zero;

        // Per-frame inputs (swivel, focus, MotorImagery snapshot)
        if (_mode == InputMode.Keyboard) UpdateKeyboard();
        else                              UpdateSocket();

        // ── Combine keyboard + headset triggers ──────────────────────
        bool kbPunch  = Input.GetKey(_punchKey);
        bool kbGuard  = Input.GetKey(_guardKey);
        bool miPunch  = (_mode == InputMode.Socket) && (MotorImagery == 1);
        bool miGuard  = (_mode == InputMode.Socket) && (MotorImagery == 2);

        bool newPunch = kbPunch || miPunch;
        bool newGuard = kbGuard || miGuard;

        // Edge-detect punch release for charge-and-fire combat
        PunchReleasedThisFrame = _prevPunchHeld && !newPunch;
        _prevPunchHeld = newPunch;

        PunchHeld = newPunch;
        GuardHeld = newGuard;
    }

    // ── Keyboard path ────────────────────────────────────────────────
    private void UpdateKeyboard()
    {
        float dx = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float dy = Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _kbSwivel.x = Mathf.Clamp(_kbSwivel.x + dx, -_swivelClamp, _swivelClamp);
        _kbSwivel.y = Mathf.Clamp(_kbSwivel.y + dy, -_swivelClamp, _swivelClamp);
        RawSwivel = _kbSwivel;

        if (Input.GetKeyDown(_focusCycleKey))
        {
            _focusPresetIndex = (_focusPresetIndex + 1) % FocusPresets.Length;
            Focus01 = FocusPresets[_focusPresetIndex];
            Debug.Log($"[Input] Focus preset → {Focus01:F1} (F to cycle: Low/Mid/High)");
        }
    }

    // ── Socket path ──────────────────────────────────────────────────
    private void UpdateSocket()
    {
        // Reconnect if disconnected
        if (!_socketConnected && _autoReconnect &&
            Time.time - _lastConnectAttempt > _reconnectDelay)
        {
            StartSocket();
        }

        if (_socketHasData)
        {
            float x, y, conc; int imag;
            lock (_lock) { x = _sockX; y = _sockY; conc = _sockConc; imag = _sockImagery; }

            RawSwivel = new Vector2(
                Mathf.Clamp(x, -_swivelClamp, _swivelClamp),
                Mathf.Clamp(y, -_swivelClamp, _swivelClamp));
            Focus01 = Mathf.Clamp01(conc / 100f);
            MotorImagery = imag;  // 0 idle | 1 charge punch | 2 guard up
        }
        else
        {
            // No data yet — keep last keyboard swivel & default focus, don't lock the player out.
            RawSwivel = _kbSwivel;
            MotorImagery = 0;
        }
    }

    // ── Mode switching ───────────────────────────────────────────────
    public void SetMode(InputMode next)
    {
        if (_mode == next) return;
        _mode = next;
        Debug.Log($"[Input] Mode → {_mode}");

        if (_mode == InputMode.Socket) StartSocket();
        else                            StopSocket();
    }

    // ── Socket lifecycle ─────────────────────────────────────────────
    private void StartSocket()
    {
        StopSocket();
        _lastConnectAttempt = Time.time;

        try
        {
            _client = new TcpClient();
            _client.Connect(_host, _port);
            _stream = _client.GetStream();
            _socketRunning = true;
            _socketConnected = true;
            _connectFailureLogged = false;

            _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
            _rxThread.Start();

            Debug.Log($"[Input] Socket connected to {_host}:{_port}");
        }
        catch (Exception e)
        {
            // Log first failure verbosely, then go silent until success — avoids spamming
            // the console every _reconnectDelay seconds while the Python server is down.
            if (!_connectFailureLogged)
            {
                Debug.LogWarning($"[Input] Socket connect failed: {e.Message}" +
                                 (_autoReconnect ? $" (silently retrying every {_reconnectDelay}s — press F2 to switch to Keyboard)" : ""));
                _connectFailureLogged = true;
            }
            _socketConnected = false;
            CleanupSocket();
        }
    }

    private void StopSocket()
    {
        _socketRunning = false;
        _socketConnected = false;
        _socketHasData = false;
        _connectFailureLogged = false;

        if (_rxThread != null && _rxThread.IsAlive)
            _rxThread.Join(300);
        _rxThread = null;

        CleanupSocket();
    }

    private void CleanupSocket()
    {
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _stream = null;
        _client = null;
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[1024];
        var leftover = "";

        while (_socketRunning)
        {
            try
            {
                if (_stream != null && _stream.DataAvailable)
                {
                    int n = _stream.Read(buffer, 0, buffer.Length);
                    if (n > 0)
                    {
                        leftover += Encoding.UTF8.GetString(buffer, 0, n);

                        int nl;
                        while ((nl = leftover.IndexOf('\n')) >= 0)
                        {
                            string line = leftover.Substring(0, nl).Trim();
                            leftover = leftover.Substring(nl + 1);
                            if (line.Length > 0) ParseLine(line);
                        }
                    }
                }
                Thread.Sleep(5);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Input] Socket read error: {e.Message}");
                _socketConnected = false;
                break;
            }
        }
    }

    // Expected format: "x,y,focus,imaginary"  (3 floats + 1 int, comma-separated).
    // Backwards-compatible with old 3-field "x,y,focus" packets (imaginary defaults to 0).
    private void ParseLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 3) return;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out float x)) return;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out float y)) return;
        if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out float c)) return;

        int imag = 0;
        if (parts.Length >= 4)
            int.TryParse(parts[3].Trim(), System.Globalization.NumberStyles.Integer, inv, out imag);

        lock (_lock)
        {
            _sockX = x; _sockY = y; _sockConc = c; _sockImagery = imag;
        }
        _socketHasData = true;
    }
}
