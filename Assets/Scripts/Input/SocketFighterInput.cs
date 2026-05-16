using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Simple socket-only fallback input for IFighterInput.
// Use PlayerFighterInput for keyboard fallback, mode switching, and auto-reconnect.
//
// Protocol: "x,y,focus,imaginary\n"  at ~25 Hz  (sent by TCP_server.py)
//   x, y      — swivel cursor in [-1, 1]
//   focus     — engagement index 0..100  (mapped to Focus01 = focus / 100)
//   imaginary — motor imagery state: 0 idle | 1 guard | 2 punch
public class SocketFighterInput : MonoBehaviour, IFighterInput
{
    [Header("Connection")]
    [SerializeField] private string _host = "127.0.0.1";
<<<<<<< Updated upstream
    [SerializeField] private int _port = 12345;
=======
    [SerializeField] private int    _port = 1234;
>>>>>>> Stashed changes

    [Header("Swivel Clamp")]
    [SerializeField] private float _swivelClamp = 1f;

    // ── IFighterInput ────────────────────────────────────────────────
    public bool    GuardHeld              { get; private set; }
    public bool    PunchHeld              { get; private set; }
    public bool    PunchReleasedThisFrame { get; private set; }
    public float   Focus01                { get; private set; }
    public Vector2 RawSwivel              { get; private set; }

    // ── Thread-safe raw values ───────────────────────────────────────
    private readonly object _lock = new object();
    private float _sockX, _sockY, _sockFocus;
    private int   _sockImagery;
    private bool  _hasData;

    // ── Socket ───────────────────────────────────────────────────────
    private TcpClient     _client;
    private NetworkStream _stream;
    private Thread        _rxThread;
    private volatile bool _isRunning;

    // ── Punch edge detection ─────────────────────────────────────────
    private bool _prevPunchHeld;

    // ────────────────────────────────────────────────────────────────

    void Start()  => Connect();

    void OnApplicationQuit() => Disconnect();

    void Update()
    {
        if (!_hasData) return;

        float x, y, focus;
        int   imagery;

        lock (_lock)
        {
            x       = _sockX;
            y       = _sockY;
            focus   = _sockFocus;
            imagery = _sockImagery;
        }

        RawSwivel = new Vector2(
            Mathf.Clamp(x, -_swivelClamp, _swivelClamp),
            Mathf.Clamp(y, -_swivelClamp, _swivelClamp));

        Focus01 = Mathf.Clamp01(focus / 100f);

        // imaginary: 0 = idle | 1 = guard | 2 = punch
        bool newPunch = (imagery == 2);
        bool newGuard = (imagery == 1);

        PunchReleasedThisFrame = _prevPunchHeld && !newPunch;
        _prevPunchHeld = newPunch;

        PunchHeld = newPunch;
        GuardHeld = newGuard;
    }

    // ── Socket lifecycle ─────────────────────────────────────────────

    private void Connect()
    {
        try
        {
            _client = new TcpClient(_host, _port);
            _stream = _client.GetStream();
            _isRunning = true;

            _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
            _rxThread.Start();

            Debug.Log($"[SocketFighterInput] Connected to {_host}:{_port}.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SocketFighterInput] Could not connect: {e.Message}");
        }
    }

    private void Disconnect()
    {
        _isRunning = false;
        _rxThread?.Join(300);

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        Debug.Log("[SocketFighterInput] Disconnected.");
    }

    // ── Receive loop (background thread) ────────────────────────────

    private void ReceiveLoop()
    {
        var buffer   = new byte[1024];
        var leftover = new StringBuilder();

        while (_isRunning)
        {
            try
            {
                if (_stream != null && _stream.DataAvailable)
                {
                    int n = _stream.Read(buffer, 0, buffer.Length);
                    if (n > 0) leftover.Append(Encoding.UTF8.GetString(buffer, 0, n));

                    string s = leftover.ToString();
                    int nl;
                    while ((nl = s.IndexOf('\n')) >= 0)
                    {
                        string line = s.Substring(0, nl).Trim();
                        s = s.Substring(nl + 1);
                        if (!string.IsNullOrEmpty(line)) ParseLine(line);
                    }
                    leftover.Clear();
                    leftover.Append(s);
                }

                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketFighterInput] Receive error: {e.Message}");
                break;
            }
        }
    }

    // ── Parser ───────────────────────────────────────────────────────

    // Expected: "x,y,focus,imaginary"
    // Backwards-compatible: ignores extra fields, skips if fewer than 4.
    private void ParseLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 4) return;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var fs  = System.Globalization.NumberStyles.Float;

        if (!float.TryParse(parts[0], fs, inv, out float x))       return;
        if (!float.TryParse(parts[1], fs, inv, out float y))       return;
        if (!float.TryParse(parts[2], fs, inv, out float focus))   return;
        if (!int.TryParse  (parts[3].Trim(), out int imagery))     return;

        lock (_lock)
        {
            _sockX       = x;
            _sockY       = y;
            _sockFocus   = focus;
            _sockImagery = imagery;
        }

        _hasData = true;
    }
}