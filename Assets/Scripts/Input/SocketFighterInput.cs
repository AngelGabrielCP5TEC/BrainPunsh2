using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Current format: "focus_index,jaw\n"  (int 0-100, int 0/1)
// Expand ParseLine() once the real Unicorn socket protocol is confirmed.
// Future fields to add: left_binary, right_binary, ax, ay, az, gx, gy, gz, focus[]
public class SocketFighterInput : MonoBehaviour, IFighterInput
{
    [Header("Connection")]
    [SerializeField] private string _host = "127.0.0.1";
    [SerializeField] private int _port = 1234;

    [Header("Debounce (seconds)")]
    [SerializeField] private float _debounceTime = 0.05f;

    // IFighterInput — populated each Update() from thread-safe raw values
    public bool GuardHeld { get; private set; }
    public bool PunchHeld { get; private set; }
    public bool PunchReleasedThisFrame { get; private set; }
    public float Focus01 { get; private set; }
    public Vector2 RawSwivel { get; private set; }  // populated once IMU arrives in socket

    // Thread-shared state
    private readonly object _lock = new object();
    private int _rawFocus;
    private int _rawJaw;

    // Debounce
    private bool _punchDebounced;
    private float _punchDebounceTimer;
    private bool _prevPunchHeld;

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private volatile bool _isRunning;

    void Start() => Connect();

    void Update()
    {
        int focus, jaw;
        lock (_lock) { focus = _rawFocus; jaw = _rawJaw; }

        Focus01 = Mathf.Clamp01(focus / 100f);

        // TODO: split jaw into left_binary (guard) and right_binary (punch)
        // when the real socket sends separate channels.
        // For now, jaw drives punch only.
        UpdateDebounce(jaw == 1, ref _punchDebounced, ref _punchDebounceTimer);
        PunchReleasedThisFrame = _prevPunchHeld && !_punchDebounced;
        _prevPunchHeld = _punchDebounced;
        PunchHeld = _punchDebounced;
        GuardHeld = false;  // placeholder until left_binary channel arrives
    }

    private void UpdateDebounce(bool raw, ref bool state, ref float timer)
    {
        if (raw) timer = _debounceTime;
        else timer -= Time.deltaTime;
        state = timer > 0f;
    }

    private void Connect()
    {
        try
        {
            _client = new TcpClient(_host, _port);
            _stream = _client.GetStream();
            _isRunning = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            Debug.Log("[Socket] Connected.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Socket] Could not connect: {e.Message}");
        }
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[1024];
        var partial = new StringBuilder();

        while (_isRunning)
        {
            try
            {
                if (_stream != null && _stream.DataAvailable)
                {
                    int n = _stream.Read(buffer, 0, buffer.Length);
                    if (n > 0) partial.Append(Encoding.UTF8.GetString(buffer, 0, n));

                    string s = partial.ToString();
                    int nl;
                    while ((nl = s.IndexOf('\n')) >= 0)
                    {
                        string line = s.Substring(0, nl).Trim();
                        s = s.Substring(nl + 1);
                        if (!string.IsNullOrEmpty(line)) ParseLine(line);
                    }
                    partial.Clear();
                    partial.Append(s);
                }
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Socket] Receive error: {e.Message}");
                break;
            }
        }
    }

    private void ParseLine(string line)
    {
        // Format: "focus_index,jaw"
        var parts = line.Split(',');
        if (parts.Length < 2) return;
        if (int.TryParse(parts[0], out int f) && int.TryParse(parts[1], out int j))
            lock (_lock) { _rawFocus = f; _rawJaw = j; }
    }

    void OnApplicationQuit() => Disconnect();

    private void Disconnect()
    {
        _isRunning = false;
        _receiveThread?.Join(200);
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        Debug.Log("[Socket] Disconnected.");
    }
}
