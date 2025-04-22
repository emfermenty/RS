using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpChatAvalonia
{
    public partial class MainWindow : Window
    {
        private Button loginButton;
        private Button logoutButton;
        private Button sendButton;
        private TextBox chatTextBox;
        private TextBox messageTextBox;
        private TextBox userNameTextBox;

        private const string HOST = "235.5.5.1";
        private const int PORT = 9001;
        private const int TTL = 20;

        private UdpClient? client;
        private IPAddress groupAddress = IPAddress.Parse(HOST);
        private bool alive = false;

        private static Dictionary<string, IPEndPoint> userMap = new();

        private string userName = "";

        public MainWindow()
        {
            InitializeComponent();

            // �������� ������ � ��������� ���������� ����� �� �����
            loginButton = this.FindControl<Button>("LoginButton");
            logoutButton = this.FindControl<Button>("LogoutButton");
            sendButton = this.FindControl<Button>("SendButton");
            chatTextBox = this.FindControl<TextBox>("ChatTextBox");
            messageTextBox = this.FindControl<TextBox>("MessageTextBox");
            userNameTextBox = this.FindControl<TextBox>("UserNameTextBox");

            // ���������� �������
            loginButton.Click += LoginButton_Click;
            logoutButton.Click += LogoutButton_Click;
            sendButton.Click += SendButton_Click;
            this.Closing += MainWindow_Closing;

            // ���������� ������ ������ ���� ���������
            logoutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            chatTextBox.IsReadOnly = true;
        }

        private async void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive && client != null)
                {
                    var result = await client.ReceiveAsync();
                    string message = Encoding.Unicode.GetString(result.Buffer);

                    // ������������ �������� ���������
                    if (message.EndsWith("����� � ���"))
                    {
                        string login = message.Split(' ')[0];
                        if (!userMap.ContainsKey(login))
                            userMap[login] = result.RemoteEndPoint;
                    }

                    // ��������� UI � ������� Dispatcher ��� ����������� ��������� � ����
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        chatTextBox.Text = $"{DateTime.Now:T} {message}\n" + chatTextBox.Text;
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    chatTextBox.Text = $"������: {ex.Message}\n" + chatTextBox.Text;
                });
            }
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            userName = userNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userName)) return;

            // ������������� �������
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            client.JoinMulticastGroup(groupAddress, TTL);

            // ������ ������ ��� ��������� ���������
            Task.Run(ReceiveMessages);

            // ���������� ��������� ��������� � ����� � ���
            SendSystemMessage($"{userName} ����� � ���");

            // ���������/�������� �������� ���������� � ����������� �� ���������
            loginButton.IsEnabled = false;
            logoutButton.IsEnabled = true;
            sendButton.IsEnabled = true;
            userNameTextBox.IsEnabled = false;
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            if (client == null) return;

            // ���������� ��������� ��������� � ������ �� ����
            SendSystemMessage($"{userName} �������� ���");

            // ��������� ����������
            client.DropMulticastGroup(groupAddress);
            client.Close();
            client = null;
            alive = false;

            // ��������/��������� �������� ����������
            loginButton.IsEnabled = true;
            logoutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            userNameTextBox.IsEnabled = true;
        }

        private void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(messageTextBox.Text) || client == null) return;

            string input = messageTextBox.Text.Trim();

            if (input.StartsWith("/private "))
            {
                var parts = input.Split(' ', 3);
                if (parts.Length >= 3)
                {
                    string targetLogin = parts[1];
                    string msg = parts[2];

                    if (userMap.TryGetValue(targetLogin, out var targetEP))
                    {
                        string privateMsg = $"[������ �� {userName}]: {msg}";
                        byte[] data = Encoding.Unicode.GetBytes(privateMsg);
                        client.Send(data, data.Length, targetEP);
                    }
                    else
                    {
                        chatTextBox.Text = $"������������ �� ������\n" + chatTextBox.Text;
                    }
                }
            }
            else
            {
                string message = $"{userName}: {input}";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, new IPEndPoint(groupAddress, PORT));
            }

            messageTextBox.Text = "";
        }

        private void SendSystemMessage(string text)
        {
            if (client == null) return;
            byte[] data = Encoding.Unicode.GetBytes(text);
            client.Send(data, data.Length, new IPEndPoint(groupAddress, PORT));
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (alive)
                LogoutButton_Click(null, null!);
        }
    }
}
