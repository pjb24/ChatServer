using System;
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
using System.Security.Cryptography;

using log4net;
using MySql.Data.MySqlClient;

using MyMessageProtocol;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestServerUI));

        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;

        // <ID, Socket>
        Dictionary<string, TcpClient> clientList = null;
        // <ID, PW> -> ID 정보만 가지고 PW 정보는 DB에만 저장하게 변경
        List<string> userList = null;
        // <groupname, <ID>>
        Dictionary<long, Tuple<string, string>> groupList = null;

        uint msgid = 0;

        static readonly string connStr = ConfigurationManager.ConnectionStrings["mariaDBConnStr"].ConnectionString;

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
            this.Close();
        }

        private void InitSocket()
        {
            IPAddress IP = IPAddress.Parse(ConfigurationManager.AppSettings["IP"]);
            int port = int.Parse(ConfigurationManager.AppSettings["Port"]);

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = "select userID from users";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                // log.Info("mariaDB connected");
                while (reader.Read())
                {
                    userList.Add(reader["userID"].ToString());
                }
                /* 동작 확인용
                foreach(string user in userList)
                {
                    Console.WriteLine("ID : " + user);
                } */
                reader.Close();

                sql = "select * from encryptedroom";
                cmd.CommandText = sql;

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // userID 복호화
                    string usersInGroup = AESDecrypt256(reader["userID"].ToString(), "0");
                    groupList.Add(long.Parse(reader["pid"].ToString()), new Tuple<string, string>(reader["roomName"].ToString(), usersInGroup));
                }
                // 동작 확인용
                foreach (KeyValuePair<long, Tuple<string, string>> temp in groupList)
                {
                    Console.WriteLine("pid : " + temp.Key + " roomName : " + temp.Value.Item1 + " users : " + temp.Value.Item2);
                }
                reader.Close();
            }

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
                if (!userList.Contains(user_ID))
                {
                    // 회원 추가
                    using (MySqlConnection conn = new MySqlConnection(connStr))
                    {
                        conn.Open();
                        string sql = string.Format("insert into users values ('{0}', '{1}')", user_ID, user_PW);

                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }

                    userList.Add(user_ID);
                    DisplayText("Register : " + user_ID);

                    string sendMsg = user_ID + " is register";
                    // clientList에 등록된 사용자가 아니기 때문에 TcpClient 정보를 사용
                    SendMessageClient(sendMsg, client);

                    // 로그인 중인 사용자에게 새로운 회원이 생겼음을 알림
                    sendMsg = user_ID + "&responseUserList";
                    foreach (string user in clientList.Keys)
                    {
                        SendMessageClient(sendMsg, user);
                    }
                }
                else
                {                    
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

                // 등록된 회원인지 판정
                if (!userList.Contains(user_ID))
                {
                    // 로그인 시도자에게 등록되지 않은 회원 알림
                    string sendMsg = user_ID + " is not registered";
                    DisplayText(sendMsg);
                    SendMessageClient(sendMsg, client);
                }
                else
                {
                    // 이미 로그인 중인지 판정
                    if (!clientList.ContainsKey(user_ID))
                    {
                        int isCorrect = 0;
                        // DB에서 비밀번호 확인 쿼리
                        using (MySqlConnection conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = string.Format("select count(userID) from users where userID='{0}' and userPW='{1}'", user_ID, user_PW);

                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            isCorrect = Convert.ToInt32(cmd.ExecuteScalar());
                            Console.WriteLine(isCorrect);
                        }

                        if (Convert.ToBoolean(isCorrect))
                        {
                            DisplayText(user_ID + " sign in");
                            string sendMsg = user_ID + "&allowSignin";
                            clientList.Add(user_ID, client);

                            SendMessageClient(sendMsg, user_ID);
                        }
                        else
                        {
                            // 로그인 시도자에게 비밀번호가 맞지 않음 알림
                            string sendMsg = "incorrect PW";
                            DisplayText(sendMsg);
                            SendMessageClient(sendMsg, client);
                        }
                    }
                    // 회원이 이미 로그인 중
                    else
                    {
                        string sendMsg = user_ID + " is already online";

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
                foreach (KeyValuePair<long, Tuple<string, string>> group in groupList)
                {
                    string[] delimiterChars = { ", " };
                    string[] users = group.Value.Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                    if (users.Contains(user_ID))
                    {
                        sendMsg = sendMsg + group.Key + "^" + group.Value.Item1 + "^" + group.Value.Item2 + "&";
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
                foreach (string user in userList)
                {
                    if (!user.Equals(user_ID))
                    {
                        sendMsg = sendMsg + user + "&";
                    }
                }
                sendMsg = sendMsg + "responseUserList";
                SendMessageClient(sendMsg, user_ID);

                DisplayText(user_ID);
            }
            // 채팅방 생성
            else if (message.Contains("createGroup"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&createGroup"));

                // string encryptedGroup = msg.Substring(msg.LastIndexOf("&") + 1);
                string group = msg.Substring(msg.LastIndexOf("&") + 1);

                string groupName = msg.Substring(0, msg.LastIndexOf("&"));

                // group 부호화
                string encryptedGroup = AESEncrypt256(group, "0");

                // DB에 채팅방 추가
                // insert 완료하면 pid 가져와서 저장하기

                // DB insert
                long pid = 0;
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = string.Format("insert into encryptedroom (roomName, userID) values ('{0}', '{1}')", groupName, encryptedGroup);

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    pid = cmd.LastInsertedId;
                }

                // groupList에 추가
                groupList.Add(pid, new Tuple<string, string>(groupName, group));

                log.Info(groupList[pid]);
                string sendMsg = "&completeCreateGroup";

                // encryptedGroup 복호화
                // string usersInGroup = AESDecrypt256(encryptedGroup, "0");
                string[] delimiterChars = { ", " };
                // string[] users = usersInGroup.Split(delimiterChars);
                string[] users = group.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                // 채팅방 인원에게 채팅방 생성 완료 메시지 발송
                foreach (string user in users)
                {
                    SendMessageClient(sendMsg, user);
                }
            }
            // groupChat
            else if (message.Contains("&groupChat"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&groupChat"));

                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                long pid = long.Parse(msg.Substring(msg.LastIndexOf("&") + 1));
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string chat = msg;

                string sendMsg = chat + "&" + pid + "&" + user_ID + "&groupChat";

                // group에 속한 모든 사용자에게 송출
                string[] delimiterChars = { ", " };
                List<string> usersInGroup = new List<string>(groupList[pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                foreach (string user in usersInGroup)
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
            // 채팅방 나가기 - 채팅방 이름도 변경되게 바꾸기
            else if (message.Contains("&LeaveGroup"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&LeaveGroup"));
                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                long pid = long.Parse(msg.Substring(0, msg.LastIndexOf("&")));

                // DB 변경
                string[] delimiterChars = { ", " };
                List<string> users = new List<string>(groupList[pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                users.Remove(user_ID);
                string usersInGroup = string.Join(", ", users);
                string encryptedGroup = AESEncrypt256(usersInGroup, "0");

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = string.Format("update encryptedroom set userID='{0}' where pid={1}", encryptedGroup, pid);

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }

                // groupList 변경
                groupList[pid] = new Tuple<string, string>(groupList[pid].Item1, usersInGroup);

                string sendMsg = pid + "&" + user_ID + "&LeaveGroup";

                // 나간 사람에게 송출
                SendMessageClient(sendMsg, user_ID);

                // group에 속한 모든 사용자에게 송출
                foreach (string user in users)
                {
                    SendMessageClient(sendMsg, user);
                }
            }
            // 채팅방 초대 - 채팅방 이름도 변경되게 바꾸기
            else if (message.Contains("&Invitation"))
            {
                string msg = message.Substring(0, message.LastIndexOf("&Invitation"));

                string user_ID = msg.Substring(msg.LastIndexOf("&") + 1);
                msg = msg.Substring(0, msg.LastIndexOf("&"));

                string group = msg.Substring(msg.LastIndexOf("&") + 1);

                long pid = long.Parse(msg.Substring(0, msg.LastIndexOf("&")));

                // groupList에서 검색 후 수정
                string[] delimiterChars = { ", " };
                List<string> users = new List<string>(groupList[pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
                string[] invitedUsers = group.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                users.AddRange(invitedUsers);

                // sort
                users.Sort();

                // Join
                string usersInGroup = string.Join(", ", users);

                // 부호화
                string encryptedGroup = AESEncrypt256(usersInGroup, "0");

                // DB 변경
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = string.Format("update encryptedroom set userID='{0}' where pid={1}", encryptedGroup, pid);

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }

                // groupList 변경
                groupList[pid] = new Tuple<string, string>(groupList[pid].Item1, usersInGroup);

                // group에 포함된 인원에게 송출
                foreach (string user in users)
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

        // clientList에 등록된 사용자에게 보내는 메시지 use protocol
        public void SendMessageClient(PacketMessage message, string user_ID)
        {
            foreach (KeyValuePair<string, TcpClient> pair in clientList)
            {
                if (pair.Key.Equals(user_ID))
                {
                    DisplayText("tcpclient : " + pair.Value + " user_ID : " + pair.Key);

                    // message 받을 client
                    TcpClient client = pair.Value as TcpClient;
                    NetworkStream stream = client.GetStream();

                    MessageUtil.Send(stream, message);
                }
            }
        }

        // clientList에 등록이 안된 client에게 보내는 메시지 use protocol
        public void SendMessageClient(PacketMessage message, TcpClient client)
        {
            // message 받을 client
            NetworkStream stream = client.GetStream();

            MessageUtil.Send(stream, message);
        }

        private string AESEncrypt256(string input, string key)
        {
            SHA256Managed sHA256Managed = new SHA256Managed();
            byte[] salt = sHA256Managed.ComputeHash(Encoding.UTF8.GetBytes(key.Length.ToString()));

            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            // aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Key = salt;
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            ICryptoTransform encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] xBuff = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            string Output = Convert.ToBase64String(xBuff);
            return Output;
        }

        private string AESDecrypt256(string input, string key)
        {
            SHA256Managed sHA256Managed = new SHA256Managed();
            byte[] salt = sHA256Managed.ComputeHash(Encoding.UTF8.GetBytes(key.Length.ToString()));

            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            // aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Key = salt;
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            ICryptoTransform decrypt = aes.CreateDecryptor();
            byte[] xBuff = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Convert.FromBase64String(input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            string Output = Encoding.UTF8.GetString(xBuff);
            return Output;
        }

        private void OnReceived(PacketMessage message, TcpClient client)
        {
            switch(message.Header.MSGTYPE)
            {
                // 회원가입 요청
                case CONSTANTS.REQ_REGISTER:
                    RequestRegister reqRegisterBody = (RequestRegister)message.Body;

                    // 중복 확인
                    if (!userList.Contains(reqRegisterBody.userID))
                    {
                        // 회원 추가
                        using (MySqlConnection conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = string.Format("insert into users values ('{0}', '{1}')", reqRegisterBody.userID, reqRegisterBody.userPW);

                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            cmd.ExecuteNonQuery();
                        }

                        userList.Add(reqRegisterBody.userID);
                        DisplayText("Register : " + reqRegisterBody.userID);

                        // 회원가입 성공 메시지 작성
                        PacketMessage resMsg = new PacketMessage();
                        resMsg.Body = new ResponseRegisterSuccess()
                        {
                            userID = reqRegisterBody.userID
                        };
                        resMsg.Header = new Header()
                        {
                            MSGID = msgid++,
                            MSGTYPE = CONSTANTS.RES_REGISTER_SUCCESS,
                            BODYLEN = (uint) resMsg.Body.GetSize(),
                            FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                            LASTMSG = CONSTANTS.LASTMSG,
                            SEQ = 0
                        };
                        // 회원가입 요청자에게 발송
                        SendMessageClient(resMsg, client);

                        // 로그인 중인 사용자에게 새로운 회원이 생겼음을 알림
                        foreach (string user in clientList.Keys)
                        {
                            SendMessageClient(resMsg, user);
                        }
                    }
                    else
                    {
                        // 이미 있는 사용자
                        PacketMessage resMsg = new PacketMessage();
                        resMsg.Header = new Header()
                        {
                            MSGID = msgid++,
                            MSGTYPE = CONSTANTS.RES_REGISTER_FAIL_EXIST,
                            BODYLEN = (uint)resMsg.Body.GetSize(),
                            FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                            LASTMSG = CONSTANTS.LASTMSG,
                            SEQ = 0
                        };
                        // 회원가입 요청자에게 발송
                        SendMessageClient(resMsg, client);
                    }
                    break;
                // 로그인 요청
                case CONSTANTS.REQ_SIGNIN:
                    RequestSignIn reqSignInBody = (RequestSignIn)message.Body;
                    
                    // 등록된 회원인지 판정
                    if (!userList.Contains(reqSignInBody.userID))
                    {
                        // 로그인 시도자에게 등록되지 않은 회원 알림
                        PacketMessage resMsg = new PacketMessage();
                        resMsg.Header = new Header()
                        {
                            MSGID = msgid++,
                            MSGTYPE = CONSTANTS.RES_SIGNIN_FAIL_NOT_EXIST,
                            BODYLEN = 0,
                            FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                            LASTMSG = CONSTANTS.LASTMSG,
                            SEQ = 0
                        };
                        SendMessageClient(resMsg, client);
                    }
                    else
                    {
                        // 이미 로그인 중인지 판정
                        if (!clientList.ContainsKey(reqSignInBody.userID))
                        {
                            int isCorrect = 0;
                            // DB에서 비밀번호 확인 쿼리
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("select count(userID) from users where userID='{0}' and userPW='{1}'", reqSignInBody.userID, reqSignInBody.userPW);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                isCorrect = Convert.ToInt32(cmd.ExecuteScalar());
                                Console.WriteLine(isCorrect);
                            }

                            if (Convert.ToBoolean(isCorrect))
                            {
                                DisplayText(reqSignInBody.userID + " sign in");
                                // 온라인 사용자 목록에 추가
                                clientList.Add(reqSignInBody.userID, client);

                                // 로그인 완료 메시지 작성 & 발송
                                PacketMessage resMsg = new PacketMessage();
                                resMsg.Header = new Header()
                                {
                                    MSGID = msgid++,
                                    MSGTYPE = CONSTANTS.RES_SIGNIN_SUCCESS,
                                    BODYLEN = 0,
                                    FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                    LASTMSG = CONSTANTS.LASTMSG,
                                    SEQ = 0
                                };

                                SendMessageClient(resMsg, reqSignInBody.userID);
                            }
                            else
                            {
                                // 로그인 시도자에게 비밀번호가 맞지 않음 알림
                                PacketMessage resMsg = new PacketMessage();
                                resMsg.Header = new Header()
                                {
                                    MSGID = msgid++,
                                    MSGTYPE = CONSTANTS.RES_SIGNIN_FAIL_WRONG_PASSWORD,
                                    BODYLEN = 0,
                                    FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                    LASTMSG = CONSTANTS.LASTMSG,
                                    SEQ = 0
                                };
                                SendMessageClient(resMsg, client);
                            }
                        }
                        // 회원이 이미 로그인 중
                        else
                        {
                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_SIGNIN_FAIL_ONLINE_USER,
                                BODYLEN = 0,
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };
                            SendMessageClient(resMsg, client);
                        }
                    }
                    break;
                // 로그아웃 통보
                case CONSTANTS.REQ_SIGNOUT:
                    break;
                // 회원목록 요청
                case CONSTANTS.REQ_USERLIST:
                    break;
                // 채팅방목록 요청
                case CONSTANTS.REQ_GROUPLIST:
                    break;
                // 채팅방 생성 요청
                case CONSTANTS.REQ_CREATE_GROUP:
                    break;
                // 채팅 메시지 발송 요청
                case CONSTANTS.REQ_CHAT:
                    break;
                // 채팅방 초대 요청
                case CONSTANTS.REQ_INVITATION:
                    break;
                // 채팅방 나가기 요청
                case CONSTANTS.REQ_LEAVE_GROUP:
                    break;
                default:
                    break;
            }
        }
    }
}

// 출처: https://it-jerryfamily.tistory.com/80 [IT 이야기]
// 출처: https://yeolco.tistory.com/53 [열코의 프로그래밍 일기]