using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MessengerMQTT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static List<RowDefinition> listRows = new List<RowDefinition>();
        static List<Border> listBorder = new List<Border>();
        bool newSession = true;
        CancellationTokenSource cts = new CancellationTokenSource();
        bool haveTypingMessage = false;
        MqttClient client;
        public MainWindow()
        {
            Random rnd = new Random();
            InitializeComponent();
            tbxNick.Text = $"Gość#{rnd.Next(1, 5000000).ToString()}";
            Configuration.clientId = tbxLogin.Text;
            Configuration.clientPass = "ti123";
            Configuration.ip = tbxIp.Text;
            Configuration.myName = tbxNick.Text;
            Configuration.sound = chkboxSound.IsChecked.Value;
            Console.WriteLine(listRows.Count);
            MainWindow.listRows.Add(new RowDefinition());
            Console.WriteLine(listRows.Count);
            this.newSession = false;
            _ = MQTT();

        }

        private async Task MQTT()
        {
            newSession = false;
            IPAddress ipAddress;
            if (!IPAddress.TryParse(Configuration.ip, out ipAddress))
                ipAddress = Dns.GetHostEntry(Configuration.ip).AddressList[0];
           try
            {
                client = new MqttClient(ipAddress, getPort(Configuration.ip), false, null, null, MqttSslProtocols.None);
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                string clientId = Configuration.myName;
                try
                {
                    if (chkboxLogin.IsChecked == true)
                        client.Connect(clientId, Configuration.clientId, Configuration.clientPass);
                    else
                        client.Connect(clientId);
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message);
                    return;
                    throw;
                }
                client.Subscribe(new string[] { Configuration.topic.ToString() }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            catch (Exception x)
            {
          
                return;
            }
        }
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (this.newSession == true) {
                this.newSession = false;
            }
            string payload = Encoding.UTF8.GetString(e.Message);
            string typeMsg = payload.Substring(0, 3);
            payload = payload.Remove(0, 3);


            if (typeMsg == "wro")
            {
                Dispatcher.Invoke(delegate
                {
                     tryWriting();
                });
            }
            else if (typeMsg == "wrs")
            {
                 taskCancle();
            }
            else if (typeMsg == "msg")
            {
                    var deserializedMsg = JsonConvert.DeserializeObject<Msg>(payload);

                    taskCancle();

                    Dispatcher.Invoke(delegate
                    {
                        this.addMessageToGrid(new Message(deserializedMsg.data, deserializedMsg.topic, deserializedMsg.myName));
                        scrollViewer.ScrollToEnd();
                    });
            }

        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            _ = sendMsgPre();
        }


        private void changeMarginTyping(bool[] toUp, Ellipse[] elipseTab)
        {

            for (int i = 0; i < 3; i++)
            {
                if (toUp[i] == false)
                {
                    if (elipseTab[i].Margin.Bottom == 1)
                    {
                        toUp[i] = true;
                        elipseTab[i].Margin = new Thickness(0, 0, 0, 2);
                    }
                    elipseTab[i].Margin = new Thickness(0, 0, 0, elipseTab[i].Margin.Bottom - 1);
                }
                else
                {
                    if (elipseTab[i].Margin.Bottom == 8)
                    {
                        toUp[i] = false;
                        elipseTab[i].Margin = new Thickness(0, 0, 0, 7);
                    }
                    elipseTab[i].Margin = new Thickness(0, 0, 0, elipseTab[i].Margin.Bottom + 1);
                }

            }


        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _ = sendMsgPre();
            }
        }

        private void playSound()
        {

            if (Configuration.sound == true)
            {
                SystemSounds.Beep.Play();
            }

        }

        private async Task sendMsgPre(string type = "msg")
        {

            if (type == "wro" && tbxMessage.Text.Trim() != "")
            {
                string msg = "wro" + Configuration.myName;
                await sendMsg(msg);
            }
            else if (type == "wrs")
            {
                string msg = "wrs" + Configuration.myName;
                await sendMsg(msg);
            }
            else if (type == "msg")
            {
                if (tbxMessage.Text.Trim() != "")
                {
                    Msg obj = new Msg(tbxMessage.Text, Configuration.topic, Configuration.myName);
                    string msg = "msg" + JsonConvert.SerializeObject(obj);

                    await sendMsg(msg);

                    tbxMessage.Text = "";
                }
            }
        }

        private void chkboxLogin_Checked(object sender, RoutedEventArgs e)
        {
            if (chkboxLogin.IsChecked.Value == true)
            {

                tbxLogin.IsEnabled = true;
                passbxPass.IsEnabled = true;

            }

        }

        private void tbxIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbxIp.Text.Trim() != "")
            {

                Configuration.ip = tbxIp.Text.Trim();
                _ = resubscribeAsync();

            }
        }

        private void tbxNick_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void tbxLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbxLogin.Text.Trim() != "")
            {
                Configuration.clientId = tbxLogin.Text.Trim();
                _ = resubscribeAsync();
            }
        }

        private void passbxPass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passbxPass.Password.Trim() != "")
            {
                Configuration.clientPass = passbxPass.Password.Trim();
                _ = resubscribeAsync();
            }
        }


        private void chkboxSound_Checked(object sender, RoutedEventArgs e)
        {
            if (chkboxSound.IsChecked.Value == true)
            {
                Configuration.sound = true;
            }

        }

        private async Task resubscribeAsync()
        {
            var a = newSession;
            if (this.newSession == false)
            {
                try
                {
                    Console.WriteLine("### DISCONNECT ###");
                    client.Unsubscribe(new string[] { Configuration.topicLast });
                    client.Disconnect();
                    Console.WriteLine("### UNSUBSCRIBED ###");

                }
                catch (Exception)
                {
                }
                try
                {
                    this.newSession = true;
                    await MQTT();
                }
                catch (Exception)
                {

                    throw;
                }
            }

        }

        private void btnRConn_Click(object sender, RoutedEventArgs e)
        {
            _ = resubscribeAsync();
        }

        private void chkboxLogin_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkboxLogin.IsChecked.Value == false)
            {
                tbxLogin.IsEnabled = false;
                passbxPass.IsEnabled = false;
            }
        }

        private void chkboxSound_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkboxSound.IsChecked.Value == false)
            {
                Configuration.sound = false;
            }
        }

        private void WindowMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var item in listBorder)
            {
                item.MaxWidth = WindowMain.Width * 0.6;
            }
        }

        private void taskCancle()
        {

            if (haveTypingMessage == true)
            {
                cts.Cancel();
                haveTypingMessage = false;
            }
        }

        private void tbxMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbxMessage.Text.Trim() != "" && haveTypingMessage == false)
                _ = sendMsgPre("wro");
            else if (tbxMessage.Text.Trim() == "" && haveTypingMessage == true)
            {
                _ = sendMsgPre("wrs");
            }

        }
        private async Task sendMsg(string msg)
        {
            string strValue = Convert.ToString(msg);
            if (client.IsConnected)
            {
                client.Publish(Configuration.topic, Encoding.UTF8.GetBytes(strValue));
            }
            
        }
        public void addMessageToGrid(Message message)
        {
            MainWindow.listRows.Add(new RowDefinition());
            gridMessages.RowDefinitions.Add(MainWindow.listRows[listRows.Count - 1]);
            this.playSound();

            TextBlock textBlock = new TextBlock();

            textBlock.Text = $" {message.myName}: {message.getMessage()}";
            textBlock.FontSize = 20;
            textBlock.Margin = new Thickness(5, 2, 5, 2);
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;


            Border border = new Border();
            listBorder.Add(border);
            border.CornerRadius = new CornerRadius(5);

            border.MaxWidth = WindowMain.Width * 0.6;
            border.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            if (message.myName == Configuration.myName)
            {
                border.Background = (Brush)new BrushConverter().ConvertFrom("#3498db");
                border.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            }
            else
            {
                border.Background = (Brush)new BrushConverter().ConvertFrom("#bdc3c7");
                border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            }
            border.BorderThickness = new Thickness(2);
            border.Margin = new Thickness(0, 10, 0, 0);

            border.Child = textBlock;
            Grid.SetRow(border, message.getId());
            gridMessages.Children.Add(border);
        }

        private void tryWriting()
        {
            if (haveTypingMessage == true) return;
            MainWindow.listRows.Add(new RowDefinition());
            gridMessages.RowDefinitions.Add(MainWindow.listRows[listRows.Count - 1]);
            Border border = new Border();
            listBorder.Add(border);
            border.CornerRadius = new CornerRadius(5);
            border.Width = 50;
            border.Height = 20;
            border.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            border.Background = (Brush)new BrushConverter().ConvertFrom("#bdc3c7");
            border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Grid.SetRow(border, Message.getSize());
            gridMessages.Children.Add(border);
            Grid gridTyping = new Grid();
            gridTyping.HorizontalAlignment = HorizontalAlignment.Stretch;
            gridTyping.VerticalAlignment = VerticalAlignment.Stretch;
            gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
            gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
            gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
            border.Child = gridTyping;


            Ellipse[] elipseTab = new Ellipse[3];
            for (int i = 0; i < 3; i++)
            {
                elipseTab[i] = new Ellipse();
                elipseTab[i].Fill = (Brush)new BrushConverter().ConvertFrom("#FFF4F4F5");
                elipseTab[i].HorizontalAlignment = HorizontalAlignment.Center;
                elipseTab[i].Height = 11;
                elipseTab[i].Width = 11;
                switch (i)
                {
                    case 0:
                        elipseTab[0].Margin = new Thickness(0, 0, 0, 1);
                        break;
                    case 1:
                        elipseTab[1].Margin = new Thickness(0, 0, 0, 4);
                        break;
                    case 2:
                        elipseTab[2].Margin = new Thickness(0, 0, 0, 7);
                        break;
                }
                elipseTab[i].VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumn(elipseTab[i], i);
                Grid.SetRow(elipseTab[i], 0);
                gridTyping.Children.Add(elipseTab[i]);
            }

            haveTypingMessage = true;
            cts = new CancellationTokenSource();
            Task.Run(() => refreshTyping(elipseTab, border), cts.Token);
        }

        private async Task refreshTyping(Ellipse[] elipseTab, Border border)
        {
            bool[] toUp = new bool[3];
            toUp[0] = false;
            toUp[1] = false;
            toUp[2] = false;
            while (true)
            {

                if (cts.Token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(delegate
                    {

                        this.gridMessages.Children.Remove(border);
                        this.gridMessages.RowDefinitions.Remove(MainWindow.listRows[listRows.Count - 1]);
                        MainWindow.listRows.RemoveAt(listRows.Count - 1);
                        haveTypingMessage = false;
                    });
                    return;
                }
                Dispatcher.Invoke(delegate
                {
                    changeMarginTyping(toUp, elipseTab);
                });
                await Task.Delay(50);

            }


        }
        private int getPort(string ip) {
            for (int i = 0; i < ip.Length; i++)
            {
                if (ip[i] == ':')
                {
                    int port = 1883;
                    int.TryParse(ip.Substring(i+1), out port);
                    return port;
                }

            }

            return 1883;
        }
        private string getIp(string ip)
        {
            for (int i = 0; i < ip.Length; i++)
            {
                if (ip[i] == ':')
                {
                    string IP = Configuration.ip;
                    IP = ip.Substring(0, i);
                    return IP;
                }

            }

            return Configuration.ip;
        }
        private void WindowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(-1);
        }

        private void tbxIp_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbxIp.Text.Trim() != "")
            {

                Configuration.ip = tbxIp.Text.Trim();
                _ = resubscribeAsync();

            }
        }

        private void tbxNick_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbxNick.Text.Trim() != "")
            {
                Configuration.myName = tbxNick.Text.Trim();
            }
        }

        private void tbxTopic_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbxTopic.Text.Trim() != "")
            {
                Configuration.topicLast = Configuration.topic;
                Configuration.topic = tbxTopic.Text.Trim();
                _ = resubscribeAsync();
            }
        }
    }
}