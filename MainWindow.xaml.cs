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

        static List<RowDefinition> listRowsMessages = new List<RowDefinition>();
        static List<RowDefinition> listRowsErrors = new List<RowDefinition>();
        static List<Border> listBorder = new List<Border>();
        bool newSession = true;
        int[] typingGestAndMe = new int[2] { -10, -10 };
        MqttClient client;
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationTokenSource ctsMe = new CancellationTokenSource();
        public MainWindow()
        {
            Random rnd = new Random();
            InitializeComponent();
            tbxNick.Text = $"Gość#{rnd.Next(1, 5000000).ToString()}";
            Configuration.clientId = tbxLogin.Text;
            Configuration.clientPass = "ti123";
            Configuration.ip = tbxIp.Text;
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789*/-!@#$%^&*";
            var stringChars = new char[16];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            Configuration.uID = new String(stringChars);
            Configuration.name = tbxNick.Text;
            Configuration.sound = chkboxSound.IsChecked.Value;
            Console.WriteLine(listRowsMessages.Count);
            MainWindow.listRowsMessages.Add(new RowDefinition());
            Console.WriteLine(listRowsMessages.Count);
            this.newSession = false;
            try
            {
                _ = MQTT(Configuration.ip, Configuration.uID, Configuration.topic);
            }
            catch (ExceptionConnection e)
            {
                showError("Problem z połączeniem z serwerem MQTT:\n     " + e.Message);
            }
            catch (Exception e)
            {
                showError("Nieznany błąd " + e.Message);
            }

        }
        /// <summary>
        /// Connect with MQTT host.
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionConnection">Thrown when something wrong with connection.</exception>
        private async Task MQTT(string ip, string uID, string topic, bool? chkboxLoginIsChecked = false, string clientId = "", string clientPass = "")
        {
            IPAddress ipAddress;
            newSession = false;
            try
            {
                if (!IPAddress.TryParse(ip, out ipAddress))
                    ipAddress = Dns.GetHostEntry(ip).AddressList[0];
            }
            catch (Exception e)
            {

                throw new ExceptionConnection("Problem z ustleniem adresu hosta " + e.Message);
            }
            try
            {
                client = new MqttClient(ipAddress, getPort(ip), false, null, null, MqttSslProtocols.None);
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                try
                {
                    if (chkboxLoginIsChecked == true)
                        client.Connect(uID, clientId, clientPass);
                    else
                        client.Connect(uID);
                }
                catch (Exception e)
                {
                    throw new ExceptionConnection("Wystąpił problem z połączeniem - " + e.Message);
                }
                client.Subscribe(new string[] { topic.ToString() }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            catch (ExceptionConnection ex)
            {
                throw new ExceptionConnection(ex.Message);
            }
            catch (Exception ex)
            {
                throw new ExceptionConnection(ex.Message);
            }
        }

        /// <summary>
        /// It activate when the MQTT receives a message.
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionConnection">Thrown when something wrong with connection.</exception>
        /// <exception cref="ExceptionBadFormat">Thrown when the message is of unknown format.</exception>
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            _=dasd(e.Message);
        }

        public async Task dasd(byte[] message) 
        {

            if (this.newSession == true)
            {
                this.newSession = false;
            }
            try
            {
                string payload = Encoding.UTF8.GetString(message);
                var deserializedMsg = JsonConvert.DeserializeObject<Msg>(payload);
                switch (deserializedMsg.type)
                {
                    case "wro":
                        Dispatcher.Invoke(delegate
                        {
                            tryWriting(deserializedMsg.uID);
                        });
                        break;
                    case "wrs":
                        typingCancle(deserializedMsg.uID);
                        break;
                    case "msg":
                        typingCancle(deserializedMsg.uID);
                        Dispatcher.Invoke(delegate
                        {
                            this.addMessage(new Message(deserializedMsg.data, deserializedMsg.topic, deserializedMsg.name, deserializedMsg.uID));
                            scrollViewer.ScrollToEnd();
                        });
                        break;
                    default:
                        throw new ExceptionConnection("Nieznany typ odebranej wiadomości: " + deserializedMsg.type);
                }
            }
            catch (ExceptionConnection ex)
            {
                showError(ex.Message);
                throw new ExceptionConnection(ex.Message);
            }
            catch (Exception ex)
            {
                showError("Nieznany format wiadomości:\n     " + ex.Message);
                throw new ExceptionBadFormat(ex.Message);
            }
        }


        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = sendMsgPre(client, Configuration.topic);
                tbxMessage.Text = "";
            }
            catch (Exception ex)
            {
                showError(ex.Message);
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    _ = sendMsgPre(client,Configuration.topic);
                    tbxMessage.Text = "";
                }
                catch (Exception ex)
                {
                    showError(ex.Message);
                }

            }
        }

        private void playSound()
        {

            if (Configuration.sound == true)
            {
                SystemSounds.Beep.Play();
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

        private void typingCancle(string name)
        {
            if ((typingGestAndMe[1] > -10 && name == Configuration.uID) || (typingGestAndMe[0] > -10 && name != Configuration.uID))
            {
                if (name == Configuration.uID)
                    ctsMe.Cancel();
                else
                    cts.Cancel();
            }
        }

        private void tbxMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbxMessage.Text.Trim() != "" && typingGestAndMe[1] == -10)
                _ = sendMsgPre(client, Configuration.topic, "wro");
            else if (tbxMessage.Text.Trim() == "" && typingGestAndMe[1] > -10)
            {
                _ = sendMsgPre(client, Configuration.topic, "wrs");
            }

        }

        /// <summary>
        /// Checking before sending what type of message it is and it convert msg to json format 
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionSendingProblem">Thrown when something wrong with sending message.</exception>
        /// <exception cref="ExceptionBadFormat">Thrown when the message is of bad format.</exception>
        private async Task sendMsgPre(MqttClient mqttClient, string topic = "", string type = "msg")
        {
            if (type == "wro" && tbxMessage.Text.Trim() == "") return;

            Msg msgReadyToConvert;
            if (type != "msg")
                msgReadyToConvert = new Msg(type, "", Configuration.topic, Configuration.name, Configuration.uID);
            else
                msgReadyToConvert = new Msg(type, tbxMessage.Text, Configuration.topic, Configuration.name, Configuration.uID);

            try
            {
                string msg = JsonConvert.SerializeObject(msgReadyToConvert);

                try
                {
                    await sendMsg(msg, topic, mqttClient);
                }
                catch (Exception ex)
                {

                    throw new ExceptionSendingProblem("Problem z wysłaniem wiadomości: "+ ex.Message);
                }
            }
            catch (Exception e)
            {
                throw new ExceptionBadFormat("Błąd podczas konwersji" + e.Message);
            }
        }

        /// <summary>
        /// Sending message via MQTT
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionSendingProblem">Thrown when something wrong with sending message.</exception>
        /// <exception cref="ExceptionBadFormat">Thrown when the message is of bad format.</exception>
        private async Task sendMsg(string msg, string topic, MqttClient mqttClient)
        {
            string strValue = Convert.ToString(msg);
            if (mqttClient.IsConnected)
            {
                mqttClient.Publish(topic, Encoding.UTF8.GetBytes(strValue));
            }

        }
        public void addMessage(Message message)
        {
            MainWindow.listRowsMessages.Add(new RowDefinition());
            gridMessages.RowDefinitions.Add(MainWindow.listRowsMessages[listRowsMessages.Count - 1]);
            this.playSound();

            TextBlock textBlock = new TextBlock();

            textBlock.Text = $"{message.name}: {message.getMessage()}";
            textBlock.FontSize = 20;
            textBlock.Margin = new Thickness(5, 2, 5, 2);
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;


            Border border = new Border();
            listBorder.Add(border);
            border.CornerRadius = new CornerRadius(5);

            border.MaxWidth = WindowMain.Width * 0.6;
            border.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            if (message.uID == Configuration.uID)
            {
                try
                {
                    changeStylesIfItsMe(border, textBlock, message.type);
                }
                catch (ExceptionEditStyle e)
                {
                    showError(e.Message);
                }
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

        /// <summary>
        /// Changes color and site of message
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionEditStyle">Thrown when something wrong with sending message.</exception>
        private void changeStylesIfItsMe(Border border, TextBlock textBlock, string type)
        {
            try
            {
                border.Background = (Brush)new BrushConverter().ConvertFrom("#3498db");
                border.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                switch (type)
                {
                    case "msg":
                        textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        break;
                    case "wro":
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                throw new ExceptionEditStyle("Problem z zmienieniem dymku na wysłany od użytkownika aplikacji:" + e.Message);
            }
        }


        /// <summary>
        /// Shows typing 
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionEditStyle">Thrown when something wrong with sending message.</exception>
        private void tryWriting(string uID)
        {
            

                if ((typingGestAndMe[1] != -10 && uID == Configuration.uID) || (typingGestAndMe[0] != -10 && uID != Configuration.uID)) return;
                MainWindow.listRowsMessages.Add(new RowDefinition());
                gridMessages.RowDefinitions.Add(MainWindow.listRowsMessages[listRowsMessages.Count - 1]);
                Border border = new Border();
                listBorder.Add(border);
                border.CornerRadius = new CornerRadius(5);
                border.Width = 50;
                border.Height = 20;
                border.VerticalAlignment = System.Windows.VerticalAlignment.Top;

                if (uID == Configuration.uID)
                {
                    typingGestAndMe[1] = MainWindow.listRowsMessages.Count - 1;
                    changeStylesIfItsMe(border, null, "wro");
                }
                else
                {
                    typingGestAndMe[0] = MainWindow.listRowsMessages.Count - 1;
                    border.Background = (Brush)new BrushConverter().ConvertFrom("#bdc3c7");
                    border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                }
                Grid.SetRow(border, Message.getSize());
                gridMessages.Children.Add(border);
                Ellipse[] elipseTab = new Ellipse[3];
                Grid gridTyping = new Grid();
                gridTyping.HorizontalAlignment = HorizontalAlignment.Stretch;
                gridTyping.VerticalAlignment = VerticalAlignment.Stretch;
                gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
                gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
                gridTyping.ColumnDefinitions.Add(new ColumnDefinition());
                border.Child = gridTyping;

                
                startElipseToTyping(elipseTab, gridTyping);



            if (uID == Configuration.uID)
            {
                ctsMe = new CancellationTokenSource();
                Task.Run(() => refreshTyping(elipseTab, border), ctsMe.Token);
            }
            else
            {
                cts = new CancellationTokenSource();
                Task.Run(() => refreshTyping(elipseTab, border), cts.Token);
            }

        }


        /// <summary>
        /// Show the ellipses that writing effect
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        /// <exception cref="ExceptionEditStyle">Thrown when something wrong with sending message.</exception>
        private async Task startElipseToTyping(Ellipse[] elipseTab, Grid gridTyping)
        {
            try
            {
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
            }
            catch (Exception e)
            {

                throw new ExceptionEditStyle("Błąd podczas ruszania elipsami : " + e);
            }
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

        /// <summary>
        /// Resubscribe MQTT
        /// </summary>
        /// <returns>
        /// Nothing
        /// </returns>
        private async Task resubscribeAsync()
        {
            var a = newSession;
            if (this.newSession == false)
            {
                try
                {
                    client.Unsubscribe(new string[] { Configuration.topicLast });
                    client.Disconnect();
                }
                catch (Exception e)
                {
                    showError(e.Message);
                }
                try
                {
                    this.newSession = true;
                    await MQTT(Configuration.ip, Configuration.uID, Configuration.topic, chkboxLogin.IsChecked, Configuration.clientId, Configuration.clientPass);
                }
                catch (Exception e)
                {
                    showError(e.Message);
                }
            }

        }
        private async Task refreshTyping(Ellipse[] elipseTab, Border border)
        {
            bool[] toUp = new bool[3];
            toUp[0] = false;
            toUp[1] = false;
            toUp[2] = false;
            while (true)
            {

                if (cts.Token.IsCancellationRequested && typingGestAndMe[0] > -10)
                {
                    try
                    {
                        Dispatcher.Invoke(delegate
                        {
                            this.gridMessages.Children.Remove(border);
                            this.gridMessages.RowDefinitions.Remove(MainWindow.listRowsMessages[typingGestAndMe[0]]);
                            MainWindow.listRowsMessages.RemoveAt(typingGestAndMe[0]);
                            typingGestAndMe[0] = -10;
                        });
                    }
                    catch (Exception x)
                    {

                        showError("Błąd podczas anulowania procesu pisania przez kogoś innego" + x.Message);
                    }
                    return;

                }
                if (ctsMe.Token.IsCancellationRequested && typingGestAndMe[1] > -10)
                {

                    try
                    {
                        Dispatcher.Invoke(delegate
                        {
                            this.gridMessages.Children.Remove(border);
                            this.gridMessages.RowDefinitions.Remove(MainWindow.listRowsMessages[typingGestAndMe[1]]);
                            MainWindow.listRowsMessages.RemoveAt(typingGestAndMe[1]);
                            typingGestAndMe[1] = -10;
                        });
                    }
                    catch (Exception x)
                    {

                        showError("Błąd podczas anulowania procesu pisania przez ciebie" + x.Message);
                    }
                    return;
                }
                Dispatcher.Invoke(delegate
                {
                    changeMarginTyping(toUp, elipseTab);
                });
                await Task.Delay(50);
            }
        }
        private int getPort(string ip)
        {
            for (int i = 0; i < ip.Length; i++)
            {
                if (ip[i] == ':')
                {
                    int port = 1883;
                    try
                    {
                        int.TryParse(ip.Substring(i + 1), out port);
                    }
                    catch (Exception e)
                    {
                        showError(e.Message);
                    }
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
                Configuration.name = tbxNick.Text.Trim();
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

        private void showError(string errorMsg)
        {
            Dispatcher.Invoke(delegate
            {
                addNewError(errorMsg);
            });

        }
        private void addNewError(string errorMsg) {

            listRowsErrors.Add(new RowDefinition());
            gridError.RowDefinitions.Add(listRowsErrors[listRowsErrors.Count - 1]);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = errorMsg;
            textBlock.FontSize = 10;
            textBlock.Padding = new Thickness(5, 2, 5, 2);
            textBlock.Background = (Brush)new BrushConverter().ConvertFrom("#f44336");
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            textBlock.Name = ("tbxError_" + (listRowsErrors.Count - 1).ToString());
            Grid.SetRow(textBlock, listRowsErrors.Count - 1);
            gridError.Children.Add(textBlock);
        }
        private void gridError_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                gridError.Children.RemoveAt(0);
                listRowsErrors.RemoveAt(0);
            }
            catch (Exception x)
            {
                showError(x.Message);
            }
        }
    }

    class ExceptionEditStyle : Exception
    {
        public ExceptionEditStyle(string message) : base(message)
        {
        }
    }
    class ExceptionConnection : Exception
    {
        public ExceptionConnection(string message) : base(message)
        {
        }
    }
    class ExceptionBadFormat : Exception
    {
        public ExceptionBadFormat(string message) : base(message)
        {
        }
    }
    class ExceptionSendingProblem : Exception
    {
        public ExceptionSendingProblem(string message) : base(message)
        {
        }
    }

}
