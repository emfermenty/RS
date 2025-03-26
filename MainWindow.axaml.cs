using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace YourNamespace
{
    public partial class MainWindow : Window
    {
        private TcpClientService _clientService;

        public MainWindow()
        {
            InitializeComponent();
            _clientService = new TcpClientService(LogMessage);

            this.FindControl<Button>("ConnectButton").Click += async (_, _) => await ConnectToServer();
            this.FindControl<Button>("SendButton").Click += async (_, _) => await SendMessage();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task ConnectToServer()
        {
            var ipTextBox = this.FindControl<TextBox>("IpTextBox");
            var connectButton = this.FindControl<Button>("ConnectButton");
            var sendButton = this.FindControl<Button>("SendButton");

            bool connected = await _clientService.Connect(ipTextBox.Text);
            if (connected)
            {
                connectButton.IsEnabled = false;
                sendButton.IsEnabled = true;
            }
        }

        private async Task SendMessage()
        {
            var messageTextBox = this.FindControl<TextBox>("MessageTextBox");
            await _clientService.SendMessage(messageTextBox.Text);
            messageTextBox.Text = "";
        }

        private void LogMessage(string message)
        {
            var messagesBox = this.FindControl<TextBox>("MessagesBox");
            messagesBox.Text += $"{message}\n";
        }

        protected override void OnClosed(EventArgs e)
        {
            _clientService.Close();
            base.OnClosed(e);
        }
    }
}
