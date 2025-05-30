using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace YourNamespace
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Action<string> _logAction;
        private IPAddress _localIP;

        public TcpClientService(Action<string> logAction)
        {
            _logAction = logAction;
            _localIP = GetLocalIPAddress();
        }

        private IPAddress GetLocalIPAddress()
        {
            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            return IPAddress.Loopback;
        }

        public async Task<bool> Connect(string serverIp, int port = 1010)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(serverIp), port);
                _stream = _client.GetStream();
                _logAction($"Подключено к серверу {serverIp}:{port}");
                _ = ReceiveMessagesAsync(); // Запуск асинхронного получения сообщений
                return true;
            }
            catch
            {
                _logAction("Ошибка: Неверный IP-адрес или сервер недоступен.");
                return false;
            }
        }

        public async Task SendMessage(string message)
        {
            if (_client == null || !_client.Connected) return;

            string formattedMessage = $"{_localIP} >> {message}";
            byte[] buffer = Encoding.Unicode.GetBytes(formattedMessage);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            _logAction($"Отправлено: {formattedMessage}");
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];

            while (_client.Connected)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.Unicode.GetString(buffer, 0, bytesRead);
                Dispatcher.UIThread.Post(() => _logAction($"Получено: {message}"));
            }

            Close();
        }

        public void Close()
        {
            _client?.Close();
            _logAction("Соединение закрыто.");
        }
    }
}
