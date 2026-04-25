// Assets/Scripts/SocketReceiver.cs
//protocolo Socket TCP para variables FocusIndex y Jaw
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketReceiver : MonoBehaviour //monobehaviour es la clase base de Unity para scripts que se adjuntan a objetos en la escena
{
    //privadas
    private TcpClient client; //cliente que se va a conectar 
    private NetworkStream stream; //flujo de datos del socket
    private Thread receiveThread; //para recibir datos en este hilo  
    private volatile bool isRunning = false; //controla el ciclo del hilo

    private readonly object dataLock = new object();
    private string lastReceivedData = ""; //ultimo data recibido

    private  readonly object dataLock = new object();
    private string lastReceivedData = ""; //ultimo data recibido

    //propiedades públicas
    public float Concentration { get; private set; } = 0.0f; //propiedad para concentración, solo lectura desde fuera
    public vector Vector { get; private set; } = new vector(0.0f, 0.0f); //propiedad para el vector de posición, solo lectura desde fuera

    //Unity Lifecycle: se llama al iniciar el juego
    void Start()
    {
        ConnectToServer("127.0.0.1", 12345); //conexión a host local, ejemplo por ahora 
    }

    void OnApplicationQuit()
    {
        StopConnection();
    }

    //Conexión al servidor TCP
    void ConnectToServer(string host, int port)
    {
        try
        {   //se crea al cliente y empieza el stream de datos en el objeto
            client = new TcpClient(host, port);
            stream = client.GetStream();
            isRunning = true;

            //hilo donde se recibirán los datos
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("Conectado al servidor LSL.");
        }
        catch (Exception e) //manejo de errores en la conexión
        {
            Debug.LogError("Error al conectar al servidor: " + e.Message);
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024]; //región temporal de memoria para los datos entrantes
        //Ya que TCP funciona mandandole octetos de datos y no mensajes de un largo definido, tenemos que hacer 
        //un leftover que se quede con el resto de datos que no se hayan procesado en el último mensaje, para luego juntarlo con el siguiente mensaje y procesarlo todo junto
        string leftover = "";

        while (isRunning)
        {
            try
            {
                if (stream != null && stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        // Convertir los bytes a string y juntar con el leftover para procesar mensajes completos!!
                        string incoming = Encoding.UTF8.GetString(buffer, 0, bytesRead); //quita el UTF8 con el que se mandó la info!!
                        leftover += incoming;
                        int nrewlineIndex;
                        while ((nrewlineIndex = leftover.IndexOf('\n')) >= 0) //mientras haya un salto de línea en el leftover, procesamos el mensaje complet
                         // Procesar mensajes completos (se asume que cada mensaje termina con un salto de línea)
                        string line = leftover.Substring(0, nrewlineIndex).Trim(); //mensaje completo
                        leftover = leftover.Substring(nrewlineIndex + 1); //lo que queda después del mensaje procesado

                        if (line.lenght > 0) //si el mensaje no está vacío, lo procesamos
                        {
                        lock (dataLock) //guardamos el último mensaje recibido de forma segura para que no haya problemas de concurrencia entre el hilo que recibe los datos y el hilo principal que los procesa
                        {
                            lastReceivedData = line ;
                        }

                        ParseData(line);
                    }
                }
                Thread.Sleep(10); //pausa ara no saturar el hilo
            }
            catch (Exception e)
            {
                Debug.LogError(" Error al recibir datos: " + e.Message);
                StopConnection();
                break;
            }
        }
    }

    void ParseData(string line)()
    {
        string[] parts = line.Split(','); //separamos el msj en partes 

        if (parts.lenght == 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out float conc)))
        {
            X = x;
            Y = y;
            Concentration = conc;
        }
        else
        {
            Debug.LogWarning("Datos recibidos en formato incorrecto: " + line);
        }
        //cierre de conexión
        void StopConnection()
        {
            isRunning = false;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(500); //esperamos 500ms para que el hilo termine
            }

            try { stream?.Close(); } catch { } //esto cierra el stream de datos, y el ? es para evitar errores si el stream ya está cerrado o no se ha creado
            try { client?.Close(); } catch { }

            Debug.Log(" Conexión cerrada.");
        }

        //API pública, en cualquier script de Unity se puede acceder así: var (x, y, conc) = GetReceiverData();
        public (float x, float y, float concentration) GetReceiverData()
        {
            lock (dataLock) //acceso seguro a los datos compartidos entre hilos
            {
                return (X, Y, Concentration);
            }
        }


