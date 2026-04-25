using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Unified player input. Switch source via Inspector enum or runtime hotkey (F2).
//
// Modes:
//   Keyboard — full keyboard/mouse control (LShift guard, Space punch, mouse swivel, F focus, R recenter)
//   Socket   — Unicorn headset over TCP (x,y → swivel, concentration → Focus01).
//              Punch and Guard still come from keyboard (Space/LShift) until the
//              headset protocol gains those triggers.
//
// Protocol (current first-draft, expandable): "x,y,concentration\n" at ~25 Hz.
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
    [SerializeField] private int    _port = 12345;
    [SerializeField] private bool   _autoReconnect = true;
    [SerializeField] private float  _reconnectDelay = 2f;

    // ── IFighterInput ────────────────────────────────────────────────
    public bool      GuardHeld              => Input.GetKey(_guardKey);
    public bool      PunchHeld              => Input.GetKey(_punchKey);
    public bool      PunchReleasedThisFrame { get; private set; }
    public float     Focus01                { get; private set; }
    public Vector2   RawSwivel              { get; private set; }

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
    private bool  _socketHasData;
    private float _lastConnectAttempt;
    private bool  _socketConnected;

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

        // Punch-released edge detection (always from keyboard for now)
        PunchReleasedThisFrame = _prevPunchHeld && !PunchHeld;
        _prevPunchHeld = PunchHeld;

        // Recenter works in either mode
        if (Input.GetKeyDown(_recenterKey)) _kbSwivel = Vector2.zero;

        if (_mode == InputMode.Keyboard) UpdateKeyboard();
        else                              UpdateSocket();
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
            float x, y, conc;
            lock (_lock) { x = _sockX; y = _sockY; conc = _sockConc; }

            RawSwivel = new Vector2(
                Mathf.Clamp(x, -_swivelClamp, _swivelClamp),
                Mathf.Clamp(y, -_swivelClamp, _swivelClamp));
            Focus01 = Mathf.Clamp01(conc);
        }
        else
        {
            // No data yet — keep last keyboard swivel & default focus, don't lock the player out.
            RawSwivel = _kbSwivel;
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

            _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
            _rxThread.Start();

            Debug.Log($"[Input] Socket connected to {_host}:{_port}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Input] Socket connect failed: {e.Message}" +
                             (_autoReconnect ? " (will retry)" : ""));
            _socketConnected = false;
            CleanupSocket();
        }
    }

    private void StopSocket()
    {
        _socketRunning = false;
        _socketConnected = false;
        _socketHasData = false;

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

    // Expected format: "x,y,concentration"  (3 floats, comma-separated)
    private void ParseLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 3) return;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out float x) &&
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out float y) &&
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out float c))
        {
            lock (_lock)
            {
                _sockX = x; _sockY = y; _sockConc = c;
            }
            _socketHasData = true;
        }
    }
}
