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

        // 1회 파일 전송 최대 크기
        const int CHUNK_SIZE = 4096;

        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;

        // <ID, Socket>
        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();
        // <ID, PW> -> ID 정보만 가지고 PW 정보는 DB에만 저장하게 변경
        List<string> userList = new List<string>();
        // <groupname, <ID>>
        Dictionary<long, Tuple<string, string>> groupList = new Dictionary<long, Tuple<string, string>>();

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

                string sql = "select count(table_rows) from information_schema.tables where table_name = 'users'";
                
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                int countTable = Convert.ToInt32(cmd.ExecuteScalar());
                if (countTable == 1)
                {
                    sql = "create table users(userID varchar(20) not null primary key, userPW char(64) not null)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }


                sql = "select count(table_rows) from information_schema.tables where table_name = 'encryptedroom'";
                cmd.CommandText = sql;
                countTable = Convert.ToInt32(cmd.ExecuteScalar());
                if (countTable != 1)
                {
                    sql = "create table encryptedroom(pid int(11) not null auto_increment primary key, roomName varchar(20) not null, userID char(64) not null)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                sql = "select userID from users";

                cmd.CommandText = sql;
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
                    Console.WriteLine(e.StackTrace);
                }
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
            try
            {
                switch (message.Header.MSGTYPE)
                {

                    // 회원가입 요청
                    case CONSTANTS.REQ_REGISTER:
                        {
                            RequestRegister reqBody = (RequestRegister)message.Body;

                            // 중복 확인
                            if (!userList.Contains(reqBody.userID))
                            {
                                // 회원 추가
                                using (MySqlConnection conn = new MySqlConnection(connStr))
                                {
                                    conn.Open();
                                    string sql = string.Format("insert into users values ('{0}', '{1}')", reqBody.userID, reqBody.userPW);

                                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                                    cmd.ExecuteNonQuery();
                                }

                                userList.Add(reqBody.userID);
                                DisplayText("Register : " + reqBody.userID);

                                // 회원가입 성공 메시지 작성
                                PacketMessage resMsg = new PacketMessage();
                                resMsg.Body = new ResponseRegisterSuccess()
                                {
                                    userID = reqBody.userID
                                };
                                resMsg.Header = new Header()
                                {
                                    MSGID = msgid++,
                                    MSGTYPE = CONSTANTS.RES_REGISTER_SUCCESS,
                                    BODYLEN = (uint)resMsg.Body.GetSize(),
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
                                    BODYLEN = 0,
                                    FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                    LASTMSG = CONSTANTS.LASTMSG,
                                    SEQ = 0
                                };
                                // 회원가입 요청자에게 발송
                                SendMessageClient(resMsg, client);
                            }
                            break;
                        }
                    // 로그인 요청
                    case CONSTANTS.REQ_SIGNIN:
                        {
                            RequestSignIn reqBody = (RequestSignIn)message.Body;

                            // 등록된 회원인지 판정
                            if (!userList.Contains(reqBody.userID))
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
                                if (!clientList.ContainsKey(reqBody.userID))
                                {
                                    int isCorrect = 0;
                                    // DB에서 비밀번호 확인 쿼리
                                    using (MySqlConnection conn = new MySqlConnection(connStr))
                                    {
                                        conn.Open();
                                        string sql = string.Format("select count(userID) from users where userID='{0}' and userPW='{1}'", reqBody.userID, reqBody.userPW);

                                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                                        isCorrect = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    if (Convert.ToBoolean(isCorrect))
                                    {
                                        DisplayText(reqBody.userID + " sign in");
                                        // 온라인 사용자 목록에 추가
                                        clientList.Add(reqBody.userID, client);

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

                                        SendMessageClient(resMsg, reqBody.userID);
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
                                // 로그인 시도자에게 회원이 이미 로그인 중 알림
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
                        }
                    // 로그아웃 통보
                    case CONSTANTS.REQ_SIGNOUT:
                        {
                            RequestSignOut reqBody = (RequestSignOut)message.Body;
                            if (clientList.ContainsKey(reqBody.userID))
                            {
                                clientList.Remove(reqBody.userID);
                            }
                            // 다른 회원에게도 알림 추가할 것
                            break;
                        }
                    // 회원목록 요청
                    case CONSTANTS.REQ_USERLIST:
                        {
                            string msg = string.Empty;
                            foreach (string user in userList)
                            {
                                msg = msg + user + "&";
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseUserList()
                            {
                                msg = msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_USERLIST,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };
                            SendMessageClient(resMsg, client);
                            break;
                        }
                    // 채팅방목록 요청
                    case CONSTANTS.REQ_GROUPLIST:
                        {
                            RequestGroupList reqBody = (RequestGroupList)message.Body;

                            string msg = string.Empty;
                            foreach (KeyValuePair<long, Tuple<string, string>> group in groupList)
                            {
                                string[] delimiterChars = { ", " };
                                string[] users = group.Value.Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                                if (users.Contains(reqBody.userID))
                                {
                                    msg = msg + group.Key + "^" + group.Value.Item1 + "^" + group.Value.Item2 + "&";
                                }
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseGroupList()
                            {
                                msg = msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_GROUPLIST,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };
                            SendMessageClient(resMsg, reqBody.userID);
                            break;
                        }
                    // 채팅방 생성 요청
                    case CONSTANTS.REQ_CREATE_GROUP:
                        {
                            RequestCreateGroup reqBody = (RequestCreateGroup)message.Body;
                            // group 부호화
                            string encryptedGroup = AESEncrypt256(reqBody.group, "0");

                            // DB에 채팅방 추가
                            // insert 완료하면 pid 가져와서 저장하기

                            // DB insert
                            long pid = 0;
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("insert into encryptedroom (roomName, userID) values ('{0}', '{1}')", reqBody.groupName, encryptedGroup);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                                pid = cmd.LastInsertedId;
                            }

                            // groupList에 추가
                            groupList.Add(pid, new Tuple<string, string>(reqBody.groupName, reqBody.group));

                            log.Info(groupList[pid]);
                            string msg = string.Empty;
                            msg = pid + "&" + reqBody.groupName + "&" + reqBody.group;

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseCreateGroupSuccess()
                            {
                                msg = msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_CREATE_GROUP_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // encryptedGroup 복호화
                            // string usersInGroup = AESDecrypt256(encryptedGroup, "0");
                            string[] delimiterChars = { ", " };
                            // string[] users = usersInGroup.Split(delimiterChars);
                            string[] users = reqBody.group.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                            // 채팅방 인원에게 채팅방 생성 완료 메시지 발송
                            foreach (string user in users)
                            {
                                SendMessageClient(resMsg, user);
                            }
                            break;
                        }
                    // 채팅 메시지 발송 요청
                    case CONSTANTS.REQ_CHAT:
                        {
                            RequestChat reqBody = (RequestChat)message.Body;

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseChat()
                            {
                                msg = reqBody.msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_CHAT,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // group에 속한 모든 사용자에게 송출
                            string[] delimiterChars = { ", " };
                            List<string> usersInGroup = new List<string>(groupList[reqBody.pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                            foreach (string user in usersInGroup)
                            {
                                SendMessageClient(resMsg, user);
                            }
                            break;
                        }
                    // 채팅방 초대 요청
                    case CONSTANTS.REQ_INVITATION:
                        {
                            RequestInvitation reqBody = (RequestInvitation)message.Body;

                            string[] delimiterChars = { ", " };
                            List<string> users = new List<string>(groupList[reqBody.pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
                            users.AddRange(reqBody.invitedUsers);

                            // sort
                            users.Sort();

                            // Join
                            string usersInGroup = string.Join(", ", users);

                            // 부호화
                            string encryptedGroup = AESEncrypt256(usersInGroup, "0");

                            // 채팅방 이름
                            string roomName = string.Empty;
                            if (usersInGroup.Length > 20)
                            {
                                roomName = usersInGroup.Substring(0, 20);
                            }
                            else
                            {
                                roomName = usersInGroup;
                            }
                            

                            // DB 변경
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("update encryptedroom set userID='{0}', roomName='{1}' where pid={2}", encryptedGroup, roomName , reqBody.pid);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                            }

                            // groupList 변경
                            groupList[reqBody.pid] = new Tuple<string, string>(roomName, usersInGroup);

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseInvitationSuccess()
                            {
                                msg = reqBody.msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_INVITATION_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // group에 포함된 인원에게 송출
                            foreach (string user in users)
                            {
                                SendMessageClient(resMsg, user);
                            }
                            break;
                        }
                    // 채팅방 나가기 요청
                    case CONSTANTS.REQ_LEAVE_GROUP:
                        {
                            RequestLeaveGroup reqBody = (RequestLeaveGroup)message.Body;

                            // DB 변경
                            string[] delimiterChars = { ", " };
                            List<string> users = new List<string>(groupList[reqBody.pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                            users.Remove(reqBody.user);
                            string usersInGroup = string.Join(", ", users);

                            // 채팅방 이름
                            string roomName = string.Empty;
                            if (usersInGroup.Length > 20)
                            {
                                roomName = usersInGroup.Substring(0, 20);
                            }
                            else
                            {
                                roomName = usersInGroup;
                            }

                            string encryptedGroup = AESEncrypt256(usersInGroup, "0");

                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("update encryptedroom set userID='{0}', roomName='{1}' where pid={2}", encryptedGroup, roomName , reqBody.pid);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                            }

                            // groupList 변경
                            groupList[reqBody.pid] = new Tuple<string, string>(roomName, usersInGroup);

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseLeaveGroupSuccess()
                            {
                                msg = reqBody.pid + "&" + reqBody.user
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_LEAVE_GROUP_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 나간 사람에게 송출
                            SendMessageClient(resMsg, reqBody.user);

                            // group에 속한 모든 사용자에게 송출
                            foreach (string user in users)
                            {
                                SendMessageClient(resMsg, user);
                            }

                            break;
                        }
                    // 파일 전송 준비 요청
                    case CONSTANTS.REQ_SEND_FILE:
                        {
                            RequestSendFile reqBody = (RequestSendFile)message.Body;

                            string msg = message.Header.MSGID + "&" + reqBody.pid + "&" + reqBody.filePath + "&" + reqBody.userID;

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseSendFile()
                            {
                                msg = msg
                                // MSGID = message.Header.MSGID,
                                // RESPONSE = CONSTANTS.ACCEPTED,
                                // filePath = reqBody.filePath
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_SEND_FILE,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            SendMessageClient(resMsg, reqBody.userID);

                            long fileSize = reqBody.FILESIZE;
                            string fileName = reqBody.FILENAME;

                            string dir = System.Windows.Forms.Application.StartupPath + "\\file";
                            if (Directory.Exists(dir) == false)
                            {
                                Directory.CreateDirectory(dir);
                            }

                            // 파일 스트림 생성
                            FileStream file = new FileStream(dir + "\\" + fileName, FileMode.Create);
                            uint? dataMsgId = null;
                            ushort prevSeq = 0;
                            while ((message = MessageUtil.Receive(client.GetStream())) != null)
                            {
                                Console.Write("#");
                                if (message.Header.MSGTYPE != CONSTANTS.REQ_SEND_FILE_DATA)
                                    break;

                                if (dataMsgId == null)
                                    dataMsgId = message.Header.MSGID;
                                else
                                {
                                    if (dataMsgId != message.Header.MSGID)
                                        break;
                                }

                                // 메시지 순서가 어긋나면 전송 중단
                                if (prevSeq++ != message.Header.SEQ)
                                {
                                    Console.WriteLine("{0}, {1}", prevSeq, message.Header.SEQ);
                                    break;
                                }

                                file.Write(message.Body.GetBytes(), 0, message.Body.GetSize());

                                // 분할 메시지가 아니면 반복을 한번만하고 빠져나옴
                                if (message.Header.FRAGMENTED == CONSTANTS.NOT_FRAGMENTED)
                                    break;
                                //마지막 메시지면 반복문을 빠져나옴
                                if (message.Header.LASTMSG == CONSTANTS.LASTMSG)
                                    break;
                            }
                            long recvFileSize = file.Length;
                            file.Close();

                            resMsg.Body = new ResponseFileSendComplete()
                            {
                                MSGID = message.Header.MSGID,
                                RESULT = CONSTANTS.SUCCESS
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_FILE_SEND_COMPLETE,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };
                            SendMessageClient(resMsg, reqBody.userID);


                            string filePath = dir + "\\" + fileName;

                            PacketMessage reqMsg = new PacketMessage();
                            reqMsg.Body = new RequestSendFile()
                            {
                                msg = reqBody.pid + "&" + reqBody.userID + "&" + fileSize + "&" + fileName + "&" + filePath
                            };
                            reqMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.REQ_SEND_FILE,
                                BODYLEN = (uint)reqMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 채팅방에 포함된 회원들에게 파일 수신 요청
                            string[] delimiterChars = { ", " };
                            List<string> users = new List<string>(groupList[reqBody.pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                            foreach (string user in users)
                            {
                                SendMessageClient(reqMsg, user);
                            }

                            break;
                        }
                    case CONSTANTS.RES_SEND_FILE:
                        {
                            ResponseSendFile resBody = (ResponseSendFile)message.Body;

                            using (Stream fileStream = new FileStream(resBody.filePath, FileMode.Open))
                            {
                                byte[] rbytes = new byte[CHUNK_SIZE];

                                long readValue = BitConverter.ToInt64(rbytes, 0);

                                int totalRead = 0;
                                ushort msgSeq = 0;
                                byte fragmented = (fileStream.Length < CHUNK_SIZE) ? CONSTANTS.NOT_FRAGMENTED : CONSTANTS.FRAGMENT;

                                while (totalRead < fileStream.Length)
                                {
                                    int read = fileStream.Read(rbytes, 0, CHUNK_SIZE);
                                    totalRead += read;
                                    PacketMessage fileMsg = new PacketMessage();

                                    byte[] sendBytes = new byte[read];
                                    Array.Copy(rbytes, 0, sendBytes, 0, read);

                                    fileMsg.Body = new RequestSendFileData(sendBytes);
                                    fileMsg.Header = new Header()
                                    {
                                        MSGID = msgid,
                                        MSGTYPE = CONSTANTS.REQ_SEND_FILE_DATA,
                                        BODYLEN = (uint)fileMsg.Body.GetSize(),
                                        FRAGMENTED = fragmented,
                                        LASTMSG = (totalRead < fileStream.Length) ? CONSTANTS.NOT_LASTMSG : CONSTANTS.LASTMSG,
                                        SEQ = msgSeq++
                                    };

                                    // 모든 파일의 내용이 전송될 때까지 파일 스트림을 0x03 메시지에 담아 클라이언트로 보냄
                                    SendMessageClient(fileMsg, resBody.userID);
                                }
                            }
                            handleClient.autoEvent.Set();
                            break;
                        }
                    case CONSTANTS.REQ_SEND_FILE_DATA:
                        {
                            break;
                        }
                    case CONSTANTS.RES_FILE_SEND_COMPLETE:
                        {
                            // 서버에서 파일을 제대로 받았는지에 대한 응답을 받음
                            ResponseFileSendComplete resBody = (ResponseFileSendComplete)message.Body;
                            Console.WriteLine("파일 전송 성공");
                            break;
                        }
                    case CONSTANTS.SEND_FILE:
                        {
                            SendFile reqBody = (SendFile)message.Body;

                            long fileSize = reqBody.FILESIZE;
                            string fileName = reqBody.FILENAME;

                            long pid = reqBody.pid;
                            string userID = reqBody.userID;
                            byte[] DATA = reqBody.DATA;

                            string dir = System.Windows.Forms.Application.StartupPath + "\\file";
                            if (Directory.Exists(dir) == false)
                            {
                                Directory.CreateDirectory(dir);
                            }

                            // 파일 스트림 생성
                            FileStream file = new FileStream(dir + "\\" + fileName, FileMode.Append);

                            Console.Write("#");

                            file.Write(reqBody.DATA, 0, reqBody.DATA.Length);
                            file.Close();


                            if (message.Header.LASTMSG == CONSTANTS.LASTMSG)
                            {
                                using (Stream fileStream = new FileStream(dir + "\\" + fileName, FileMode.Open))
                                {
                                    byte[] rbytes = new byte[CHUNK_SIZE];

                                    long readValue = BitConverter.ToInt64(rbytes, 0);

                                    int totalRead = 0;
                                    ushort msgSeq = 0;
                                    byte fragmented = (fileStream.Length < CHUNK_SIZE) ? CONSTANTS.NOT_FRAGMENTED : CONSTANTS.FRAGMENT;

                                    while (totalRead < fileStream.Length)
                                    {
                                        int read = fileStream.Read(rbytes, 0, CHUNK_SIZE);
                                        totalRead += read;
                                        PacketMessage fileMsg = new PacketMessage();

                                        byte[] sendBytes = new byte[read];
                                        Array.Copy(rbytes, 0, sendBytes, 0, read);

                                        fileMsg.Body = new SendFile()
                                        {
                                            msg = pid + "&^%$#&^%$&^%$" + userID + "&^%$#&^%$&^%$" + fileName + "&^%$#&^%$&^%$" + fileSize + "&^%$#&^%$&^%$" + Encoding.Unicode.GetString(sendBytes)
                                        };
                                        fileMsg.Header = new Header()
                                        {
                                            MSGID = msgid,
                                            MSGTYPE = CONSTANTS.SEND_FILE,
                                            BODYLEN = (uint)fileMsg.Body.GetSize(),
                                            FRAGMENTED = fragmented,
                                            LASTMSG = (totalRead < fileStream.Length) ? CONSTANTS.NOT_LASTMSG : CONSTANTS.LASTMSG,
                                            SEQ = msgSeq++
                                        };

                                        string[] delimiterChars = { ", " };
                                        List<string> users = new List<string>(groupList[pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                                        foreach (string user in users)
                                        {
                                            SendMessageClient(fileMsg, user);
                                        }
                                    }
                                }
                            }


                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (NullReferenceException ne)
            {
                Console.WriteLine(ne.StackTrace);
            }
        }
    }
}