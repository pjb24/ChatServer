using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        public Dictionary<string, string> userList = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> groupList = new Dictionary<string, Dictionary<string, string>>();

        public TestServerUI()
        {
            InitializeComponent();
            // socket start
            Thread t = new Thread(InitSocket);
            t.IsBackground = true;
            t.Start();
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void InitSocket()
        {
            server = new TcpListener(IPAddress.Any, 11000);
            clientSocket = default(TcpClient);
            server.Start();
            DisplayText(">> Server Started");

            while (true)
            {
                try
                {
                    counter++;
                    clientSocket = server.AcceptTcpClient();
                    DisplayText(">> Accept connection from client");

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string user_name = Encoding.Unicode.GetString(buffer, 0, bytes);
                    user_name = user_name.Substring(0, user_name.IndexOf("$"));

                    clientList.Add(clientSocket, user_name);

                    // send message all user
                    SendMessageAll(user_name + " Joined ", "", false);

                    handleClient h_client = new handleClient();
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }
                catch (SocketException se)
                {
                    Console.WriteLine(string.Format("InitSocket - SocketException : {0}", se.Message));
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("InitSocket - Exception : {0}", ex.Message));
                    break;
                }
            }

            clientSocket.Close();
            server.Stop();
        }

        void h_client_OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsKey(clientSocket))
                clientList.Remove(clientSocket);
        }

        private void OnReceived(string message, string user_name)
        {
            if (!message.Contains("register") && !message.Contains("signin"))
            {
                string displayMessage = "From client : " + user_name + " : " + message;
                DisplayText(displayMessage);
                SendMessageAll(message, user_name, true);
            } else
            {
                if (message.Contains("register"))
                {
                    string user_ID = message.Substring(0, message.IndexOf("register"));
                    string user_PW = message.Substring(message.IndexOf("register") + 8);
                    DisplayText(user_ID + user_PW);

                    if (!userList.ContainsKey(user_ID))
                    {
                        userList.Add(user_ID, user_PW);
                        DisplayText("Register : " + user_ID);
                    }
                    else
                    {
                        DisplayText(user_ID + " is aleady registered");
                    }
                } else if (message.Contains("signin"))
                {
                    string user_ID = message.Substring(0, message.IndexOf("signin"));
                    string user_PW = message.Substring(message.IndexOf("signin") + 6);
                    DisplayText(user_ID + user_PW);

                    if (!userList.ContainsKey(user_ID))
                    {
                        DisplayText(user_ID + " is not registered yet");
                    }
                    else
                    {
                        if (userList[user_ID].Equals(user_PW))
                        {
                            DisplayText(user_ID + " sign in");
                        } else
                        {
                            DisplayText("incorrect PW");
                        }
                    }
                }
            }
            
        }

        public void SendMessageAll(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                Console.WriteLine(string.Format("tcpclient : {0} user_name : {1}", pair.Key, pair.Value));

                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    buffer = Encoding.Unicode.GetBytes(user_name + " says : " + message);
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message);
                }

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        private void DisplayText(string text)
        {
            if (lb_Result.InvokeRequired)
            {
                lb_Result.BeginInvoke(new MethodInvoker(delegate
                {
                    lb_Result.Items.Add(text + Environment.NewLine);
                }));
            }
            else
                lb_Result.Items.Add(text + Environment.NewLine);
        }
    }
}

// 출처: https://it-jerryfamily.tistory.com/80 [IT 이야기]
// 출처: https://yeolco.tistory.com/53 [열코의 프로그래밍 일기]