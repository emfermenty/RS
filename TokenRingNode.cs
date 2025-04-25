using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

class TokenRingNode
{
    private readonly int myPort;
    private readonly string nextIp;
    private readonly int nextPort;
    private readonly bool isInitialHolder;

    private UdpClient udpClient;
    private bool running = true;

    public TokenRingNode(int myPort, string nextIp, int nextPort, bool isInitialHolder = false)
    {
        this.myPort = myPort;
        this.nextIp = nextIp;
        this.nextPort = nextPort;
        this.isInitialHolder = isInitialHolder;
    }

    public void Start()
    {
        udpClient = new UdpClient(myPort);
        Log($"Узел запущен на порту {myPort}. Следующий узел: {nextIp}:{nextPort}");

        if (isInitialHolder)
        {
            Thread.Sleep(1000); // Ждём запуска других
            Log("Узел стартует с токеном.");
            SendToken();
        }

        while (running)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message == "TOKEN")
                {
                    Log($"Токен получен от {remoteEP}. Выполняется работа...");
                    Thread.Sleep(500); // Эмуляция работы
                    SendToken();
                }
            }
            catch (SocketException se)
            {
                Log($"Сокет закрыт: {se.Message}");
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
        }
    }

    private void SendToken()
    {
        try
        {
            IPEndPoint nextEP = new IPEndPoint(IPAddress.Parse(nextIp), nextPort);
            byte[] tokenData = Encoding.UTF8.GetBytes("TOKEN");
            udpClient.Send(tokenData, tokenData.Length, nextEP);
            Log($"Токен передан узлу {nextIp}:{nextPort}.");
        }
        catch (Exception ex)
        {
            Log($"Ошибка при отправке токена: {ex.Message}");
        }
    }

    public void Stop()
    {
        running = false;
        udpClient?.Close();
    }

    private void Log(string message)
    {
        string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Console.WriteLine(logLine);
        File.AppendAllText($"log_{myPort}.txt", logLine + Environment.NewLine);
    }
}