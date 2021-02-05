﻿using System;
// List, Dictionary, ...
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
// Encoding
using System.Text;
// 비동기 작업에 사용?
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
// IPAddress
using System.Net;
// TcpListener, TcpClient, NetworkStream
using System.Net.Sockets;
using System.Configuration;
using System.IO;

using log4net;
using MySql.Data.MySqlClient;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestServerUI));

        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;
        
        // <ID, Socket>
        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();
        // <ID, PW>
        Dictionary<string, string> userList = new Dictionary<string, string>();
        // <groupname, <ID>>
        Dictionary<string, List<string>> groupList = new Dictionary<string, List<string>>();

        static readonly string connStr = ConfigurationManager.ConnectionStrings["mariaDBConnStr"].ConnectionString;
        static readonly MySqlConnection conn = new MySqlConnection(connStr);

        public TestServerUI()
        {
            InitializeComponent();

            // Server Thread
            Thread t = new Thread(InitSocket);
            // background option
            t.IsBackground = true;
            // Thread start
            log.Info("server start");
            t.Start();
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            // Thread들의 상태는 어떻게 변경되는가?
            conn.Close();
            this.Close();
        }

        private void InitSocket()
        {
            IPAddress IP = IPAddress.Parse(ConfigurationManager.AppSettings["IP"]);
            int port = int.Parse(ConfigurationManager.AppSettings["Port"]);
            

            // TcpListener class 사용, 11000포트로 들어오는 모든 IP 요청을 받는다
            server = new TcpListener(IP, port);
            // 초기화
            clientSocket = default(TcpClient);
            // Listen start
            server.Start();
            DisplayText(">> Server Started");

            while (true)
            {
                try
                {
                    counter++;
                    // accept된 client socket 정보 저장
                    clientSocket = server.AcceptTcpClient();
                    DisplayText(">> Accept connection from client");

                    /*
                    // 통신에 필요한 정보 TCP - stream, UDP - datagram
                    NetworkStream stream = clientSocket.GetStream();
                    // 버퍼 생성 1024 byte - 1kb
                    byte[] buffer = new byte[1024];
                    // buffer에 들어온 정보를 읽고 그 크기를 반환
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    // buffer에 들어온 정보를 Unicode로 변환
                    string user_name = Encoding.Unicode.GetString(buffer, 0, bytes);
                    // string msg = Encoding.Unicode.GetString(buffer, 0, buffer.Length);
                    // message에서 유효정보 자르기
                    user_name = user_name.Substring(0, user_name.IndexOf("$"));
                    */

                    // send message all user, 현재 의미 없음
                    // SendMessageAll(user_name + " Joined ", "", false);

                    // handleClient 객체 생성
                    handleClient h_client = new handleClient();
                    // 이벤트 할당, 가입?, this.OnReceived 함수를 h_client.OnReceived에 할당
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);
                    // 객체의 함수 사용
                    h_client.startClient(clientSocket);
                }
                // socket 오류
                catch (SocketException se)
                {
                    Console.WriteLine(string.Format("InitSocket - SocketException : {0}", se.Message));
                    break;
                }
                // socket 오류를 제외한 오류들
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("InitSocket - Exception : {0}", ex.Message));
                    break;
                }
            }

            // 오류 발생할 때 종료
            clientSocket.Close();
            server.Stop();
        }

        // clientList가 있으면 해당 자료 제거
        void h_client_OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsValue(clientSocket))
            {
                try
                {
                        foreach (KeyValuePair<string, TcpClient> item in clientList)
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

        // received message 처리
        private void OnReceived(string message, TcpClient client)
        {
            if (message.Contains("register"))
            {
                string msg = message.Substring(0, message.LastIndexOf("register"));

                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));
                string user_PW = msg.Substring(msg.LastIndexOf("&") + 1);
                DisplayText(user_ID + "&" + user_PW);

                // 중복 확인
                if (!userList.ContainsKey(user_ID))
                {
                    userList.Add(user_ID, user_PW);
                    DisplayText("Register : " + user_ID);

                    string sendMsg = user_ID + " is register";
                    // clientList에 등록된 사용자가 아니기 때문에 TcpClient 정보를 사용
                    SendMessageClient(sendMsg, client);

                    sendMsg = user_ID + "&responseUserList";
                    foreach (string user in clientList.Keys)
                    {
                        SendMessageClient(sendMsg, user);
                    }
                }
                else
                {
                    // 사용자에게 보내기도 필요
                    // 이미 있는 사용자
                    string sendMsg = user_ID + " is aleady registered";
                    DisplayText(sendMsg);
                    SendMessageClient(sendMsg, client);
                }
            }
            else if (message.Contains("signin"))
            {
                string msg = message.Substring(0, message.LastIndexOf("signin"));

                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));
                string user_PW = msg.Substring(msg.LastIndexOf("&") + 1);
                DisplayText(user_ID + "&" + user_PW);

                if (!userList.ContainsKey(user_ID))
                {
                    // 사용자에게 보내기 필요
                    string sendMsg = user_ID + " is not registered";
                    DisplayText(sendMsg);
                    SendMessageClient(sendMsg, client);
                }
                else
                {
                    if (userList[user_ID].Equals(user_PW))
                    {
                        if (!clientList.ContainsKey(user_ID))
                        {
                            DisplayText(user_ID + " sign in");
                            string sendMsg = user_ID + "&allowSignin";
                            clientList.Add(user_ID, client);

                            SendMessageClient(sendMsg, user_ID);
                        }
                        else
                        {
                            string sendMsg = user_ID + " is already online";

                            SendMessageClient(sendMsg, client);
                        }
                    }
                    else
                    {
                        // 사용자에게 보내기 필요
                        string sendMsg = "incorrect PW";
                        DisplayText(sendMsg);
                        SendMessageClient(sendMsg, client);
                    }
                }
            } // 동기화 할 때 발생
            else if (message.Contains("requestGroupList"))
            {
                string msg = message.Substring(0, message.LastIndexOf("requestGroupList"));
                string user_ID = msg.Substring(0, msg.LastIndexOf("&"));

                string sendMsg = null;
                // 요청한 user_ID가 들어있는 groupList를 추출
                foreach (KeyValuePair<string, List<string>> group in groupList)
                {
                    List<string> users = group.Value;
                    if (users.Contains(user_ID))
                    {
                        foreach (string user in users)
                        {
                            sendMsg = sendMsg + user + "^";
                        }
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
                foreach (KeyValuePair<string, string> pair in userList)
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

                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);

                // user_ID 자르기
                List<string> users = new List<string>(msg.Split('&'));
                users.Sort();


                List<string> usersInGroup = new List<string>();
                foreach (string user in users)
                {
                    usersInGroup.Add(user);
                }

                string group = string.Join("+", users);
                //string group = msg.Replace('&', '+');

                if (!groupList.ContainsKey(group + "Group"))
                {
                    // groupList에 추가
                    groupList.Add(group + "Group", usersInGroup);

                    // group 생성 완료 message 전송, 현재는 생성 요청한 user에게만 보냄
                    // group에 포함된 모든 사용자에게 보내도록 수정하자
                    DisplayText(msg);
                    string sendMsg = user_ID + "&completeCreateGroup";

                    foreach (string temp in usersInGroup)
                    {
                        SendMessageClient(sendMsg, temp);
                    }
                }
                else
                {
                    string sendMsg = group + "Group" + "&existGroup";
                    SendMessageClient(sendMsg, user_ID);
                }

            }
            // groupChat
            else if (message.Contains("&groupChat"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&groupChat"));

                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string group = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string chat = msg;

                string sendMsg = chat + "&" + group + "&" + user_ID + "&groupChat";

                // group에 속한 모든 사용자에게 송출
                foreach (string user in groupList[group])
                {
                    SendMessageClient(sendMsg, user);
                }
            }
            // 로그아웃
            else if (message.Contains("&SignOut"))
            {
                DisplayText(message);
                string msg = message.Substring(0, message.LastIndexOf("&SignOut"));

                string user_ID = msg;

                if (clientList.ContainsKey(user_ID))
                {
                    clientList.Remove(user_ID);
                }
            }
            // 채팅방 나가기
            else if (message.Contains("&LeaveGroup"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&LeaveGroup"));
                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                string group = msg.Substring(0, msg.LastIndexOf("&"));

                groupList[group].Remove(user_ID);

                string sendMsg = group + "&" + user_ID + "&LeaveGroup";

                // 나간 사람에게 송출
                SendMessageClient(sendMsg, user_ID);

                // group에 속한 모든 사용자에게 송출
                foreach (string user in groupList[group])
                {
                    SendMessageClient(sendMsg, user);
                }
            }
            // 채팅방 초대
            else if (message.Contains("&Invitation"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&Invitation"));

                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string group = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                List<string> InvitedUsers = msg.Split('&').ToList<string>();

                foreach(string user in InvitedUsers)
                {
                    groupList[group].Add(user);
                }

                foreach(string user in groupList[group])
                {
                    SendMessageClient(message, user);
                }
            }
            // 파일 전송
            else if (message.Contains("&requestSendFile"))
            {
                Console.WriteLine(message);

                string msg = message.Substring(0, message.LastIndexOf("&requestSendFile"));

                string user_ID = msg.Substring(msg.LastIndexOf("&"));

                msg = msg.Substring(0, msg.LastIndexOf("&"));

                long fileSize = long.Parse(msg.Substring(msg.LastIndexOf("&")));

                string fileName = msg.Substring(0, msg.LastIndexOf("&"));

                string dir = System.Windows.Forms.Application.StartupPath + "\\file";
                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }

                FileStream file = new FileStream(dir + "\\" + fileName, FileMode.Create);

                /*
                string sendMsg = "&responseSendFile";

                SendMessageClient(sendMsg, user_ID);

                ushort prevSeq = 0;
                while ((reqMsg = MessageUtil.Receive(stream)) != null)
                {
                    Console.Write("#");

                    // 메시지 순서가 어긋나면 전송 중단
                    if (prevSeq++ != reqMsg.Header.SEQ)
                    {
                        Console.WriteLine("{0}, {1}", prevSeq, reqMsg.Header.SEQ);
                        break;
                    }

                    file.Write(reqMsg.Body.GetBytes(), 0, reqMsg.Body.GetSize());

                    // 분할 메시지가 아니면 반복을 한번만하고 빠져나옴
                    if (reqMsg.Header.FRAGMENTED == CONSTANTS.NOT_FRAGMENTED)
                        break;
                    //마지막 메시지면 반복문을 빠져나옴
                    if (reqMsg.Header.LASTMSG == CONSTANTS.LASTMSG)
                        break;
                }
                long recvFileSize = file.Length;
                file.Close();

                Console.WriteLine();
                Console.WriteLine("수신 파일 크기 : {0} bytes", recvFileSize);

                Message rstMsg = new Message();
                rstMsg.Body = new BodyResult()
                {
                    MSGID = reqMsg.Header.MSGID,
                    RESULT = CONSTANTS.SUCCESS
                };
                rstMsg.Header = new Header()
                {
                    MSGID = msgid++,
                    MSGTYPE = CONSTANTS.FILE_SEND_RES,
                    BODYLEN = (uint)rstMsg.Body.GetSize(),
                    FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                    LASTMSG = CONSTANTS.LASTMSG,
                    SEQ = 0
                };

                if (fileSize == recvFileSize)
                    // 파일 전송 요청에 담겨온 파일 크기와 실제로 받은 파일 크기를 비교
                    // 같으면 성공 메지시를 보냄
                    MessageUtil.Send(stream, rstMsg);
                else
                {
                    rstMsg.Body = new BodyResult()
                    {
                        MSGID = reqMsg.Header.MSGID,
                        RESULT = CONSTANTS.FAIL
                    };

                    // 파일 크기에 이상이 있다면 실패 메시지를 보냄
                    MessageUtil.Send(stream, rstMsg);
                }
                Console.WriteLine("파일 전송을 마쳤습니다.");
                */
            }
        }

        // clientList에 있는 모든 사용자에게 보내는 메시지, 이게 필요한가?
        public void SendMessageAll(string message, string user_ID, bool flag)
        {
            foreach (KeyValuePair<string, TcpClient> pair in clientList)
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

        // 크로스스레드 문제
        private void DisplayText(string text)
        {
            if (lb_Result.InvokeRequired)
            {
                lb_Result.BeginInvoke(new MethodInvoker(delegate
                {
                    lb_Result.Items.Add(text + Environment.NewLine);
                    // 마지막 줄로 이동
                    lb_Result.SelectedIndex = lb_Result.Items.Count - 1;
                }));
            }
            else
            {
                lb_Result.Items.Add(text + Environment.NewLine);
                lb_Result.SelectedIndex = lb_Result.Items.Count - 1;
            }
        }

        // clientList에 등록된 사용자에게 보내는 메시지
        public void SendMessageClient(string message, string user_ID)
        {
            foreach (KeyValuePair<string, TcpClient> pair in clientList)
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