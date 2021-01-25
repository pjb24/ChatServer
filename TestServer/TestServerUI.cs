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

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        public Dictionary<string, string> userList = new Dictionary<string, string>();
        public Dictionary<string, List<string>> groupList = new Dictionary<string, List<string>>();

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
            }
            else if (message.Contains("signin"))
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
                        string msg = user_ID + "allowSignin";

                        SendMessageClient(msg, user_name, true);
                    }
                    else
                    {
                        DisplayText("incorrect PW");
                    }
                }
            } // 동기화 할 때 발생
            else if (message.Contains("requestGroupList"))
            {
                string user_ID = message.Substring(0, message.IndexOf("requestGroupList"));

                // 요청한 user_ID가 들어있는 groupList를 추출
                foreach(var group in groupList)
                {
                    var g = group.Value;
                    // user가 들어있는 group이 있는 개수만큼 msg 발생 1회만 발생하도록 고치자
                    if (g.Contains(user_ID))
                    {
                        string msg = group.Key + "groupList";
                        SendMessageClient(msg, user_name, true);
                    }
                }
                DisplayText(user_ID);
            } // 동기화 할 때 발생
            else if (message.Contains("requestUserList"))
            {
                string user_ID = message.Substring(0, message.IndexOf("requestUserList"));
                string msg = null;
                // 일단 전체 user_ID 정보 전송
                foreach(var pair in userList)
                {
                    msg = msg + pair.Key + "&";
                }
                msg = msg + "userID";
                SendMessageClient(msg, user_name, true);

                DisplayText(user_ID);
            }
            else if (message.Contains("createGroup"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&"));

                string user_ID = message.Substring(message.LastIndexOf("&"));

                // user_ID 자르기
                string[] users = msg.Split('&');
                
                List<string> usersInGroup = new List<string>();
                foreach(string user in users)
                {
                    usersInGroup.Add(user);
                }
                // groupList에 추가
                groupList.Add(msg+"Group", usersInGroup);

                // group 생성 완료 message 전송
                DisplayText(msg);
                msg = user_ID + "completeCreateGroup";
                SendMessageClient(msg, user_name, true);
            }
            // send message
            else
            {
                string displayMessage = "From client : " + user_name + " : " + message;
                DisplayText(displayMessage);
                SendMessageAll(message, user_name, true);
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

        public void SendMessageClient(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                if (pair.Value.Equals(user_name))
                {
                    Console.WriteLine(string.Format("tcpclient : {0} user_name : {1}", pair.Key, pair.Value));
                    DisplayText("tcpclient : " + pair.Key + " user_name : " + pair.Value);

                    // message 받을 client
                    TcpClient client = pair.Key as TcpClient;
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = null;

                    buffer = Encoding.Unicode.GetBytes(message);

                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
            }
        }
    }
}

// 출처: https://it-jerryfamily.tistory.com/80 [IT 이야기]
// 출처: https://yeolco.tistory.com/53 [열코의 프로그래밍 일기]