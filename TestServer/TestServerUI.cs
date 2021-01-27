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

        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();
        Dictionary<string, string> userList = new Dictionary<string, string>();
        Dictionary<string, List<string>> groupList = new Dictionary<string, List<string>>();

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

                    // send message all user
                    SendMessageAll(user_name + " Joined ", "", false);

                    handleClient h_client = new handleClient();
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket);
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
            if (clientList.ContainsValue(clientSocket))
            {
                try
                {
                    foreach (var item in clientList)
                    {
                        if (item.Value.Equals(clientSocket))
                        {
                            clientList.Remove(item.Key);
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void OnReceived(string message, TcpClient client)
        {
            if (message.Contains("register"))
            {
                string msg = message.Substring(0, message.LastIndexOf("register"));

                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));
                string user_PW = msg.Substring(msg.LastIndexOf("&"));
                DisplayText(user_ID + "&" + user_PW);

                if (!userList.ContainsKey(user_ID))
                {
                    userList.Add(user_ID, user_PW);
                    DisplayText("Register : " + user_ID);

                    string sendMsg = user_ID + " is register";
                    SendMessageClient(sendMsg, client);
                }
                else
                {
                    DisplayText(user_ID + " is aleady registered");
                }
            }
            else if (message.Contains("signin"))
            {
                string msg = message.Substring(0, message.LastIndexOf("signin")) ;

                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));
                string user_PW = msg.Substring(msg.LastIndexOf("&"));
                DisplayText(user_ID + "&" + user_PW);

                if (!userList.ContainsKey(user_ID))
                {
                    DisplayText(user_ID + " is not registered");
                }
                else
                {
                    if (userList[user_ID].Equals(user_PW))
                    {
                        DisplayText(user_ID + " sign in");
                        string sendMsg = user_ID + "&allowSignin";
                        clientList.Add(user_ID, clientSocket);

                        SendMessageClient(sendMsg, user_ID);
                    }
                    else
                    {
                        DisplayText("incorrect PW");
                    }
                }
            } // 동기화 할 때 발생
            else if (message.Contains("requestGroupList"))
            {
                string msg = message.Substring(0, message.LastIndexOf("requestGroupList"));
                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));

                string sendMsg = null;
                // 요청한 user_ID가 들어있는 groupList를 추출
                foreach(var group in groupList)
                {
                    var g = group.Value;
                    if (g.Contains(user_ID))
                    {
                        sendMsg = sendMsg + group.Key + "&";
                    }
                }
                sendMsg = sendMsg + "responseGroupList";

                SendMessageClient(sendMsg, user_ID);
                DisplayText(user_ID + " responseGroupList");
            } // 동기화 할 때 발생
            else if (message.Contains("requestUserList"))
            {
                string msg = message.Substring(0, message.LastIndexOf("requestUserList"));
                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));
                string sendMsg = null;
                // 일단 전체 user_ID 정보 전송, 친구 기능을 넣고 싶음
                foreach(var pair in userList)
                {
                    if (!pair.Key.Equals(user_ID))
                    {
                        sendMsg = sendMsg + pair.Key + "&";
                    }
                }
                sendMsg = sendMsg + "responseUserList";
                SendMessageClient(sendMsg, user_ID);

                DisplayText(user_ID);
            }
            else if (message.Contains("createGroup"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&createGroup"));

                string user_ID = msg.Substring(msg.LastIndexOf("&") +1);

                // user_ID 자르기
                string[] users = msg.Split('&');
                
                List<string> usersInGroup = new List<string>();
                foreach(string user in users)
                {
                    usersInGroup.Add(user);
                }
                string group = msg.Replace('&', '+');
                // groupList에 추가
                groupList.Add(group+"Group", usersInGroup);

                // group 생성 완료 message 전송, 현재는 생성 요청한 user에게만 보냄
                DisplayText(msg);
                string sendMsg = user_ID + "&completeCreateGroup";
                SendMessageClient(sendMsg, user_ID);
            }
            // groupChat
            else if (message.Contains("&groupChat"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&groupChat"));

                string user_ID = msg.Substring(msg.LastIndexOf("&")+1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));
                
                string group = msg.Substring(msg.LastIndexOf("&")+1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string chat = msg;

                string sendMsg = chat + "&" + group + "&" + user_ID + "&groupChat";

                foreach(string user in groupList[group])
                {
                    SendMessageClient(sendMsg, user);
                }
            }
        }

        // clientList에 있는 모든 사용자에게 보내는 메시지, 이게 필요한가?
        public void SendMessageAll(string message, string user_ID, bool flag)
        {
            foreach (var pair in clientList)
            {
                Console.WriteLine(string.Format("tcpclient : {0} user_ID : {1}", pair.Value, pair.Key));

                TcpClient client = pair.Value as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    buffer = Encoding.Unicode.GetBytes(user_ID + " says : " + message);
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

        // clientList에 등록된 사용자에게 보내는 메시지
        public void SendMessageClient(string message, string user_ID)
        {
            foreach (var pair in clientList)
            {
                if (pair.Key.Equals(user_ID))
                {
                    DisplayText("tcpclient : " + pair.Value + " user_ID : " + pair.Key);
                    DisplayText(message);

                    // message 받을 client
                    TcpClient client = pair.Value as TcpClient;
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = null;

                    buffer = Encoding.Unicode.GetBytes(message);

                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
            }
        }

        // clientList에 등록이 안된 client에게 보내는 메시지
        public void SendMessageClient(string message, TcpClient client)
        {
            // message 받을 client
            NetworkStream stream = client.GetStream();
            byte[] buffer = null;

            buffer = Encoding.Unicode.GetBytes(message);

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
    }
}

// 출처: https://it-jerryfamily.tistory.com/80 [IT 이야기]
// 출처: https://yeolco.tistory.com/53 [열코의 프로그래밍 일기]