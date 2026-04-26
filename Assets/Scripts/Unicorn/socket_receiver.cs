using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketReceiver : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private volatile bool isRunning = false;

    private readonly object dataLock = new object();

    // Datos recibidos
    public Vector2 Cursor { get; private set; } = Vector2.zero;
    public int Focus { get; private set; } = 0;
    public int Imaginary { get; private set; } = 0;

    void Start()
    {
        ConnectToServer(
            "127.0.0.1",
            12345
        );
    }

    void OnApplicationQuit()
    {
        StopConnection();
    }

    void ConnectToServer(
        string host,
        int port
    )
    {
        try
        {
            client = new TcpClient(
                host,
                port
            );

            stream = client.GetStream();

            isRunning = true;

            receiveThread =
                new Thread(ReceiveData);

            receiveThread.IsBackground = true;

            receiveThread.Start();

            Debug.Log(
                "TCP conectado."
            );
        }

        catch(Exception e)
        {
            Debug.LogError(
                e.Message
            );
        }
    }
    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        string leftover = "";

        while(isRunning)
        {
            try
            {
                int bytesRead =
                    stream.Read(
                        buffer,
                        0,
                        buffer.Length
                    );

                if(bytesRead <= 0)
                    continue;

                string incoming =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead
                    );

                leftover += incoming;

                int newlineIndex;

                while(
                    (newlineIndex =
                    leftover.IndexOf('\n')) >= 0
                )
                {
                    string line =
                        leftover.Substring(
                            0,
                            newlineIndex
                        ).Trim();

                    leftover =
                        leftover.Substring(
                            newlineIndex + 1
                        );

                    if(line.Length > 0)
                    {
                        ParseData(line);
                    }
                }
            }

            catch(Exception e)
            {
                Debug.LogError(
                    e.Message
                );

                StopConnection();
                break;
            }
        }
    }

    void ParseData(string line)
    {
        string[] parts =
            line.Split(',');

        if(
            parts.Length == 4

            &&

            float.TryParse(
                parts[0],
                System.Globalization.
                NumberStyles.Float,

                System.Globalization.
                CultureInfo.InvariantCulture,

                out float x
            )

            &&

            float.TryParse(
                parts[1],
                System.Globalization.
                NumberStyles.Float,

                System.Globalization.
                CultureInfo.InvariantCulture,

                out float y
            )

            &&

            int.TryParse(
                parts[1],
                System.Globalization.
                NumberStyles.Float,

                System.Globalization.
                CultureInfo.InvariantCulture,

                out float focus
            )

            &&

            int.TryParse(
                parts[3],
                out int imag
            )
        )
        {
            lock(dataLock)
            {
                Cursor =
                    new Vector2(
                        x,
                        y
                    );

                Focus = focus;
                Imaginary = imag;
            }

            Debug.Log(
              $"X:{x} Y:{y} Focus:{focus} MI:{imag}"
            );
        }

        else
        {
            Debug.LogWarning(
                "Formato incorrecto: "
                + line
            );
        }
    }

    void StopConnection()
    {
        isRunning = false;

        if(
            receiveThread != null &&
            receiveThread.IsAlive
        )
        {
            receiveThread.Join(500);
        }

        try
        {
            stream?.Close();
        }
        catch{}

        try
        {
            client?.Close();
        }
        catch{}

        Debug.Log(
            "Conexión cerrada."
        );
    }

    public (
        float,
        float,
        float,
        int
    ) GetReceiverData()
    {
        lock(dataLock)
        {
            return
            (
                Cursor.x,
                Cursor.y,
                Focus,
                Imaginary
            );
        }
    }
}