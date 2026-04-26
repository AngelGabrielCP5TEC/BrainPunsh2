using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketReceiver : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    Thread receiveThread;

    volatile bool isRunning=false;

    readonly object dataLock = new object();

    public Vector2 Cursor { get; private set; } = Vector2.zero;
    public int State { get; private set; } = 0;

    void Start()
    {
        ConnectToServer("127.0.0.1",12345);
    }

    void OnApplicationQuit()
    {
        StopConnection();
    }

    void ConnectToServer(string host,int port)
    {
        try
        {
            client = new TcpClient(host,port);
            stream = client.GetStream();

            isRunning=true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground=true;
            receiveThread.Start();

            Debug.Log("Conectado.");
        }

        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        string leftover="";

        while(isRunning)
        {
            try
            {
                if(stream!=null && stream.DataAvailable)
                {
                    int bytesRead=
                        stream.Read(
                           buffer,
                           0,
                           buffer.Length
                        );

                    if(bytesRead>0)
                    {
                        string incoming=
                            Encoding.UTF8.GetString(
                               buffer,
                               0,
                               bytesRead
                            );

                        leftover+=incoming;

                        int newlineIndex;

                        while(
                           (newlineIndex=
                           leftover.IndexOf('\n'))>=0
                        )
                        {
                            string line=
                              leftover.Substring(
                                0,
                                newlineIndex
                              ).Trim();

                            leftover=
                              leftover.Substring(
                                newlineIndex+1
                              );

                            if(line.Length>0)
                            {
                                ParseData(line);
                            }
                        }
                    }
                }

                Thread.Sleep(10);
            }

            catch(Exception e)
            {
                Debug.LogError(e.Message);
                StopConnection();
                break;
            }
        }
    }

    void ParseData(string line)
    {
        string[] parts=line.Split(',');

        if(parts.Length==3 &&
           float.TryParse(parts[0],
             System.Globalization.NumberStyles.Float,
             System.Globalization.CultureInfo.InvariantCulture,
             out float x)

           &&

           float.TryParse(parts[1],
             System.Globalization.NumberStyles.Float,
             System.Globalization.CultureInfo.InvariantCulture,
             out float y)

           &&

           int.TryParse(
             parts[2],
             out int state))
        {
            lock(dataLock)
            {
                Cursor = new Vector2(x,y);
                State = state;
            }
        }
        else
        {
            Debug.LogWarning("Formato incorrecto: "+line);
        }
    }

    void StopConnection()
    {
        isRunning=false;

        if(receiveThread!=null &&
           receiveThread.IsAlive)
        {
            receiveThread.Join(500);
        }

        try{stream?.Close();} catch{}
        try{client?.Close();} catch{}

        Debug.Log("Conexión cerrada");
    }

    public (float,float,int) GetReceiverData()
    {
        lock(dataLock)
        {
            return(
                Cursor.x,
                Cursor.y,
                State
            );
        }
    }
}