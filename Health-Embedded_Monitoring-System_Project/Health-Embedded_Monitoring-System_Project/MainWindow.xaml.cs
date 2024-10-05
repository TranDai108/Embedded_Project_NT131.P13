using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Health_Embedded_Monitoring_System_Project
{
    public partial class MainWindow : Window
    {
        private IMqttClient _mqttClient;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToMqttBrokerAsync();
        }

        private async Task ConnectToMqttBrokerAsync()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())  // Đặt tên cho client của bạn
                .WithTcpServer("d3339750ee4c4b2d9351cf54b5f358c6.s1.eu.hivemq.cloud", 8883)  // Đổi cổng thành 8883 nếu dùng TLS
                .WithCredentials("SlayPanther", "123456789")  // Thay bằng tài khoản HiveMQ
                .WithTls()
                .WithCleanSession()             
                .Build();

            _mqttClient.ConnectedAsync += async e =>
            {
                UpdateStatusThread("Kết nối thành công."); // Cập nhật giao diện khi kết nối thành công
                await SubscribeToTopicAsync();
            };

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                UpdateRecievedThread($"Tin nhắn nhận được: {message}"); // Cập nhật giao diện khi nhận được tin nhắn
                return Task.CompletedTask;
            };

            try
            {
                await _mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                UpdateStatusThread($"Lỗi khi kết nối: {ex.Message}"); // Cập nhật lỗi lên giao diện
            }
        }

        private async Task SubscribeToTopicAsync()
        {
            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic("heart")
                .Build();

            await _mqttClient.SubscribeAsync(topicFilter);
            UpdateStatusThread("Đã đăng ký chủ đề."); // Cập nhật giao diện khi đăng ký chủ đề thành công
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("heart")
                .WithPayload(PublishTextBox.Text)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            if (_mqttClient.IsConnected)
            {
                await _mqttClient.PublishAsync(message);
                UpdateStatusThread("Tin nhắn đã được gửi."); // Cập nhật giao diện khi tin nhắn được gửi
            }
            else
            {
                UpdateStatusThread("Client chưa kết nối."); // Cập nhật giao diện khi client chưa kết nối
            }
        }

        // Hàm cập nhật giao diện chính từ các luồng khác
        private void UpdateStatusThread(string message)
        {
            // Sử dụng Dispatcher để đảm bảo mã được thực thi trên luồng UI chính
            Dispatcher.Invoke(() =>
            {
                StatusBox.Text += $"{message}\n";  // Thêm thông báo vào TextBox
            });
        }
        private void UpdateRecievedThread(string message)
        {
            // Sử dụng Dispatcher để đảm bảo mã được thực thi trên luồng UI chính
            Dispatcher.Invoke(() =>
            {
                ReceivedDataTextBox.Text += $"{message}\n";  // Thêm thông báo vào TextBox
            });
        }
    }
}
