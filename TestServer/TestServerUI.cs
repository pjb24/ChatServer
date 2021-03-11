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

        // <userID, Socket>
        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();
        // <userNo, userID>
        Dictionary<int, string> userList = new Dictionary<int, string>();
        // <roomNo, <accessRight, roomName>>
        Dictionary<int, Tuple<int, string>> roomList = new Dictionary<int, Tuple<int, string>>();
        // <No, <roomNo, userNo, managerRight>>
        Dictionary<int, Tuple<int, int, int>> usersInRoom = new Dictionary<int, Tuple<int, int, int>>();

        uint msgid = 0;

        static readonly string connStr = ConfigurationManager.ConnectionStrings["mariaDBConnStr"].ConnectionString;

        public TestServerUI()
        {
            InitializeComponent();

            if (BitConverter.IsLittleEndian)
                Console.WriteLine("Little Endian");
            else
                Console.WriteLine("Big Endian");

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


                // DB에 userList - 회원 목록 테이블이 없으면 생성
                string sql = "select count(table_rows) from information_schema.tables where table_name = 'userList'";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                int countTable = Convert.ToInt32(cmd.ExecuteScalar());
                if (countTable != 1)
                {
                    sql = "create table userList(No int not null auto_increment primary key, userID varchar(20) not null, userPW char(64) not null)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                // DB에 usersInRoom - 채팅방에 속한 회원 테이블이 없으면 생성
                sql = "select count(table_rows) from information_schema.tables where table_name = 'usersInRoom'";
                cmd.CommandText = sql;
                countTable = Convert.ToInt32(cmd.ExecuteScalar());
                if (countTable != 1)
                {
                    sql = "create table usersInRoom(No int not null auto_increment primary key, roomNo int not null, userNo int not null, managerRight int not null)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                // DB에 roomList - 채팅방 목록 테이블이 없으면 생성
                sql = "select count(table_rows) from information_schema.tables where table_name = 'roomList'";
                cmd.CommandText = sql;
                countTable = Convert.ToInt32(cmd.ExecuteScalar());
                if (countTable != 1)
                {
                    sql = "create table roomList(No int not null auto_increment primary key, accessRight tinyint not null, roomName varchar(20) not null)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                // 회원 목록 가져오기
                sql = "select No, userID from userList";

                cmd.CommandText = sql;
                MySqlDataReader reader = cmd.ExecuteReader();
                // log.Info("mariaDB connected");
                while (reader.Read())
                {
                    userList.Add((int)reader["No"], reader["userID"].ToString());
                }
                /* 동작 확인용
                foreach(string user in userList)
                {
                    Console.WriteLine("ID : " + user);
                } */
                reader.Close();

                // 채팅방 목록 가져오기
                sql = "select * from roomList";
                cmd.CommandText = sql;

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    roomList.Add((int)reader["No"], new Tuple<int, string>((sbyte)reader["accessRight"], reader["roomName"].ToString()));
                }
                // 동작 확인용
                foreach (KeyValuePair<int, Tuple<int, string>> temp in roomList)
                {
                    Console.WriteLine("No : " + temp.Key + " accessRight : " + temp.Value.Item1 + " roomName : " + temp.Value.Item2);
                }
                reader.Close();

                // 채팅방 속한 회원 목록 가져오기
                sql = "select * from usersInRoom";
                cmd.CommandText = sql;

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    usersInRoom.Add((int)reader["No"], new Tuple<int, int, int>((int)reader["roomNo"], (int)reader["userNo"], (int)reader["managerRight"]));
                }
                // 동작 확인용
                foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                {
                    Console.WriteLine("No : " + temp.Key + " roomNo : " + temp.Value.Item1 + " userNo : " + temp.Value.Item2 + " managerRight : " + temp.Value.Item3);
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

        private int SearchUserNoByUserID(string userID)
        {
            int userNo = 0;
            foreach(KeyValuePair<int, string> temp in userList)
            {
                if (temp.Value.Equals(userID))
                {
                    userNo = temp.Key;
                    break;
                }
            }
            return userNo;
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

                            int No = 0;
                            // 중복 확인
                            if (!userList.ContainsValue(reqBody.userID))
                            {
                                // 회원 추가
                                using (MySqlConnection conn = new MySqlConnection(connStr))
                                {
                                    conn.Open();

                                    string sql = string.Format("insert into userList (userID, userPW) values ('{0}', '{1}')", reqBody.userID, reqBody.userPW);

                                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                                    cmd.ExecuteNonQuery();
                                    No = (int)cmd.LastInsertedId;
                                }

                                userList.Add(No, reqBody.userID);
                                DisplayText("Register : " + reqBody.userID);

                                // 회원가입 성공 메시지 작성
                                PacketMessage resMsg = new PacketMessage();
                                resMsg.Body = new ResponseRegisterSuccess()
                                {
                                    msg = No + "&" + reqBody.userID
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
                                // 회원가입 성공 로그 기록
                                log.Info(string.Format("{0}님이 회원가입", reqBody.userID));

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
                            if (!userList.ContainsValue(reqBody.userID))
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
                                        string sql = string.Format("select count(userID) from userList where userID='{0}' and userPW='{1}'", reqBody.userID, reqBody.userPW);

                                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                                        isCorrect = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    if (Convert.ToBoolean(isCorrect))
                                    {
                                        // 로그인 성공 로그 기록
                                        log.Info(string.Format("{0}님이 로그인", reqBody.userID));

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

                                        // 잘못된 비밀번호 로그인 시도 로그 기록
                                        log.Info(string.Format("{0}님에게 잘못된 비밀번호로 로그인 시도", reqBody.userID));

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
                                    // 접속 중 사용자 로그인 시도 로그 기록
                                    log.Info(string.Format("접속 중인 {0}님에 로그인 시도", reqBody.userID));

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

                            // 로그아웃 로그 기록
                            log.Info(string.Format("{0}님이 로그아웃", reqBody.userID));

                            // 다른 회원에게도 알림 추가할 것
                            break;
                        }
                    // 회원목록 요청
                    case CONSTANTS.REQ_USERLIST:
                        {
                            string msg = string.Empty;
                            foreach (KeyValuePair<int, string> temp in userList)
                            {
                                msg = msg + temp.Key + "^" + temp.Value + "&";
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
                    case CONSTANTS.REQ_ROOMLIST:
                        {
                            RequestRoomList reqBody = (RequestRoomList)message.Body;

                            string msg = string.Empty;
                            int userNo = 0;
                            userNo = SearchUserNoByUserID(reqBody.userID);
                            foreach (KeyValuePair<int, Tuple<int, string>> room in roomList)
                            {
                                // 비공개 room 검색
                                if (room.Value.Item1.Equals(0))
                                {
                                    foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                                    {
                                        if (temp.Value.Item2.Equals(userNo) && room.Key.Equals(temp.Value.Item1))
                                        {
                                            // roomNo & accessRight & roomName &
                                            msg = msg + room.Key + "&" + room.Value.Item1 + "&" + room.Value.Item2 + "&";
                                            // 요청자가 속한 room의 정보 검색
                                            foreach (KeyValuePair<int, Tuple<int, int, int>> user in usersInRoom)
                                            {
                                                if (room.Key.Equals(user.Value.Item1))
                                                {
                                                    // usersInRoomNo ^ userNo ^ managerRight ^^
                                                    msg = msg + user.Key + "^" + user.Value.Item2 + "^" + user.Value.Item3 + "^^";
                                                }
                                            }
                                            msg = msg + "*";
                                        }
                                    } 
                                }
                                // 공개 room 검색
                                else
                                {
                                    // roomNo & accessRight & roomName & usersInRoomNo ^ userNo ^ managerRight ... ^&
                                    msg = msg + room.Key + "&" + room.Value.Item1 + "&" + room.Value.Item2 + "&";
                                    // room에 속한 유저 정보
                                    foreach (KeyValuePair<int, Tuple<int, int, int>> user in usersInRoom)
                                    {
                                        if (room.Key.Equals(user.Value.Item1))
                                        {
                                            msg = msg + user.Key + "^" + user.Value.Item2 + "^" + user.Value.Item3 + "^^";
                                        }
                                    }
                                    msg = msg + "*";
                                }
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseRoomList()
                            {
                                msg = msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_ROOMLIST,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };
                            SendMessageClient(resMsg, reqBody.userID);
                            break;
                        }
                    // 채팅방 생성 요청
                    case CONSTANTS.REQ_CREATE_ROOM:
                        {
                            RequestCreateRoom reqBody = (RequestCreateRoom)message.Body;

                            // DB에 채팅방 추가
                            // insert 완료하면 pid 가져와서 저장하기

                            // DB insert
                            int roomNo = 0;
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("insert into roomList (accessRight, roomName) values ('{0}', '{1}')", reqBody.accessRight, reqBody.roomName);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                                roomNo = (int)cmd.LastInsertedId;
                            }
                            // roomList에 추가
                            roomList.Add(roomNo, new Tuple<int, string>(reqBody.accessRight, reqBody.roomName));

                            // 채팅방 생성자 번호 검색
                            int creatorNo = 0;
                            creatorNo = SearchUserNoByUserID(reqBody.creator);
                            // 채팅방 생성자 추가
                            int usersInRoomNoCreator = 0;
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("insert into usersInRoom (roomNo, userNo, managerRight) values ('{0}', '{1}', '{2}')", roomNo, creatorNo, 2);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                                usersInRoomNoCreator = (int)cmd.LastInsertedId;
                            }
                            usersInRoom.Add(usersInRoomNoCreator, new Tuple<int, int, int>(roomNo, creatorNo, 2));

                            // 채팅방 회원 번호 검색, 채팅방 회원 추가
                            int usersInRoomNo = 0;
                            int userNo = 0;
                            foreach (string user in reqBody.users)
                            {
                                userNo = SearchUserNoByUserID(user);
                                using (MySqlConnection conn = new MySqlConnection(connStr))
                                {
                                    conn.Open();
                                    string sql = string.Format("insert into usersInRoom (roomNo, userNo, managerRight) values ('{0}', '{1}', '{2}')", roomNo, userNo, 0);

                                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                                    cmd.ExecuteNonQuery();
                                    usersInRoomNo = (int)cmd.LastInsertedId;
                                }
                                usersInRoom.Add(usersInRoomNo, new Tuple<int, int, int>(roomNo, userNo, 0));
                            }

                            // 채팅방 생성 로그 기록
                            log.Info(string.Format("{0} 채팅방 생성 {1}번 회원 : {2}", reqBody.roomName, roomNo, reqBody.users));

                            string msg = roomNo + "&" + reqBody.accessRight + "&" + reqBody.roomName + "&";

                            foreach(var temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(roomNo))
                                {
                                    msg = msg + temp.Key + "^" + temp.Value.Item2 + "^" + temp.Value.Item3 + "^^";
                                }
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseCreateRoomSuccess()
                            {
                                msg = msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_CREATE_ROOM_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 비공개 채팅방
                            if (reqBody.accessRight == 0)
                            {
                                // 채팅방 생성자에게 메시지 발송
                                SendMessageClient(resMsg, reqBody.creator);
                                // 채팅방 인원에게 채팅방 생성 완료 메시지 발송
                                foreach (string user in reqBody.users)
                                {
                                    SendMessageClient(resMsg, user);
                                }
                            }
                            // 공개 채팅방
                            else
                            {
                                foreach (KeyValuePair<string, TcpClient> temp in clientList)
                                {
                                    SendMessageClient(resMsg, temp.Value);
                                }
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

                            List<string> usersInGroup = new List<string>();
                            // room에 속한 모든 사용자에게 송출
                            /*
                            foreach (var temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    usersInGroup.Add(userList[temp.Value.Item2]);
                                }
                            }
                            */
                            /*
                            foreach (string user in usersInGroup)
                            {
                                SendMessageClient(resMsg, user);
                            }
                            */

                            foreach (KeyValuePair<string, TcpClient> temp in clientList)
                            {
                                SendMessageClient(resMsg, temp.Value);
                            }
                            break;
                        }
                    // 채팅방 초대 요청
                    case CONSTANTS.REQ_INVITATION:
                        {
                            RequestInvitation reqBody = (RequestInvitation)message.Body;

                            // 채팅방 회원 번호 검색, 채팅방 회원 추가
                            int usersInRoomNo = 0;
                            int userNo = 0;
                            string msg = reqBody.roomNo + "&";
                            foreach (string user in reqBody.invitedUsers)
                            {
                                userNo = SearchUserNoByUserID(user);
                                using (MySqlConnection conn = new MySqlConnection(connStr))
                                {
                                    conn.Open();
                                    string sql = string.Format("insert into usersInRoom (roomNo, userNo, managerRight) values ('{0}', '{1}', '{2}')", reqBody.roomNo, userNo, 0);

                                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                                    cmd.ExecuteNonQuery();
                                    usersInRoomNo = (int)cmd.LastInsertedId;
                                }
                                // usersInRoom 추가
                                usersInRoom.Add(usersInRoomNo, new Tuple<int, int, int>(reqBody.roomNo, userNo, 0));
                                msg = msg + usersInRoomNo + "^" + userNo + "^^";
                                // 채팅방 초대 로그 기록
                                log.Info(string.Format("{0}님이 {1}번 {2}채팅방에 초대됨", user, reqBody.roomNo, roomList[reqBody.roomNo].Item2));
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseInvitationSuccess()
                            {
                                msg = msg
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

                            // room에 포함된 인원에게 송출
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(resMsg, userList[temp.Value.Item2]);
                                }
                            }
                            break;
                        }
                    // 채팅방 나가기 요청
                    case CONSTANTS.REQ_LEAVE_ROOM:
                        {
                            RequestLeaveRoom reqBody = (RequestLeaveRoom)message.Body;

                            // 회원 번호 검색
                            int userNo = 0;
                            userNo = SearchUserNoByUserID(reqBody.userID);

                            // DB 변경
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("delete from usersInRoom where roomNo = {0} and userNo = {1}", reqBody.roomNo, userNo);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                            }

                            // usersInRoom 제거
                            int usersInRoomNo = 0;
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo) && temp.Value.Item2.Equals(userNo))
                                {
                                    usersInRoomNo = temp.Key;
                                    break;
                                }
                            }
                            usersInRoom.Remove(usersInRoomNo);

                            // 채팅방 나가기 로그 기록
                            log.Info(string.Format("{0}님이 {1}번 {2}채팅방에서 나감", reqBody.userID, reqBody.roomNo, roomList[reqBody.roomNo].Item2));

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseLeaveRoomSuccess()
                            {
                                msg = reqBody.roomNo + "&" + reqBody.userID
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_LEAVE_ROOM_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 나간 사람에게 송출
                            SendMessageClient(resMsg, reqBody.userID);

                            // room에 속한 모든 사용자에게 송출
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(resMsg, userList[temp.Value.Item2]);
                                }
                            }
                            break;
                        }
                    // 채팅방 추방
                    case CONSTANTS.REQ_BANISH_USER:
                        {
                            RequestBanishUser reqBody = (RequestBanishUser)message.Body;

                            int banishedUserNo = 0;
                            int usersInRoomNo = 0;

                            // 추방될 회원 번호 검색
                            banishedUserNo = SearchUserNoByUserID(reqBody.banishedUser);
                            // DB 변경
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("delete from usersInRoom where roomNo = {0} and userNo = {1}", reqBody.roomNo, banishedUserNo);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                            }
                            // usersInRoom 변경
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo) && temp.Value.Item2.Equals(banishedUserNo))
                                {
                                    usersInRoomNo = temp.Key;
                                    break;
                                }
                            }
                            usersInRoom.Remove(usersInRoomNo);
                            // 로그 기록
                            log.Info(string.Format("{0}님이 {1}번 {2}채팅방에서 추방됨", reqBody.banishedUser, reqBody.roomNo, roomList[reqBody.roomNo].Item2));
                            // 채팅방 회원들에게 송출
                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseLeaveRoomSuccess()
                            {
                                msg = reqBody.roomNo + "&" + reqBody.banishedUser
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_BANISH_USER_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 추방된 사람에게 송출
                            SendMessageClient(resMsg, reqBody.banishedUser);

                            // room에 속한 모든 사용자에게 송출
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(resMsg, userList[temp.Value.Item2]);
                                }
                            }
                            break;
                        }
                    // 채팅방 설정 변경 요청
                    case CONSTANTS.REQ_CHANGE_ROOM_CONFIG:
                        {
                            RequestChangeRoomConfig reqBody = (RequestChangeRoomConfig)message.Body;

                            string accessRightToString = string.Empty;
                            if (reqBody.accessRight == 0)
                            {
                                accessRightToString = "비공개";
                            }
                            else
                            {
                                accessRightToString = "공개";
                            }

                            // DB 변경
                            using (MySqlConnection conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                string sql = string.Format("update roomList set accessRight = {0}, roomName = '{1}' where No = {2}", reqBody.accessRight, reqBody.roomName, reqBody.roomNo);

                                MySqlCommand cmd = new MySqlCommand(sql, conn);
                                cmd.ExecuteNonQuery();
                            }
                            // 로그 기록
                            // 변경점 확인
                            // accessRight 와 roomName 변경
                            if (!reqBody.accessRight.Equals(roomList[reqBody.roomNo].Item1) && !reqBody.roomName.Equals(roomList[reqBody.roomNo].Item2))
                            {
                                log.Info(string.Format("{0}번 채팅방의 공개 여부가 {1}로, 채팅방 이름이 {2}로 변경됨", reqBody.roomNo, accessRightToString, reqBody.roomName));
                            }
                            // accessRight 변경
                            else if (!reqBody.roomName.Equals(roomList[reqBody.roomNo].Item1))
                            {
                                log.Info(string.Format("{0}번 채팅방의 공개 여부가 {1} 변경됨", reqBody.roomNo, accessRightToString));
                            }
                            // roomName 변경
                            else if (!reqBody.roomName.Equals(roomList[reqBody.roomNo].Item2))
                            {
                                log.Info(string.Format("{0}번 채팅방의 이름이 {1}로 변경됨", reqBody.roomNo, reqBody.roomName));
                            }
                            // roomList 변경
                            roomList[reqBody.roomNo] = new Tuple<int, string>(reqBody.accessRight, reqBody.roomName);

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseChangeRoomConfigSuccess()
                            {
                                msg = reqBody.msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_CHANGE_ROOM_CONFIG_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 채팅방에 속한 모든 사용자에게 송출
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(resMsg, userList[temp.Value.Item2]);
                                }
                            }
                            break;
                        }
                    // 관리자 권한 변경 요청
                    case CONSTANTS.REQ_CHANGE_MANAGEMENT_RIGHTS:
                        {
                            RequestChangeManagementRights reqBody = (RequestChangeManagementRights)message.Body;

                            List<int> tempKey = new List<int>();
                            List<int> tempRoomNo = new List<int>();
                            List<int> tempUserNo = new List<int>();
                            List<int> tempRight = new List<int>();

                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                foreach(int userNo in reqBody.changedUsersNo)
                                {
                                    if (temp.Value.Item1.Equals(reqBody.roomNo) && temp.Value.Item2.Equals(userNo) && temp.Value.Item3.Equals(0))
                                    {
                                        // DB 변경
                                        using (MySqlConnection conn = new MySqlConnection(connStr))
                                        {
                                            conn.Open();
                                            string sql = string.Format("update usersInRoom set managerRight = 1 where roomNo = {0} and userNo = {1}", reqBody.roomNo, userNo);

                                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                                            cmd.ExecuteNonQuery();
                                        }
                                        tempKey.Add(temp.Key);
                                        tempRoomNo.Add(reqBody.roomNo);
                                        tempUserNo.Add(userNo);
                                        tempRight.Add(1);
                                        // usersInRoom[temp.Key] = new Tuple<int, int, int>(reqBody.roomNo, userNo, 1);
                                        // 로그 기록
                                        log.Info(string.Format("{0}번 {1} 채팅방에서 {2} 회원에게 관리자 권한이 부여됨", reqBody.roomNo, roomList[reqBody.roomNo].Item2, userList[userNo]));
                                    }
                                    else if (temp.Value.Item1.Equals(reqBody.roomNo) && temp.Value.Item2.Equals(userNo) && temp.Value.Item3.Equals(1))
                                    {
                                        // DB 변경
                                        using (MySqlConnection conn = new MySqlConnection(connStr))
                                        {
                                            conn.Open();
                                            string sql = string.Format("update usersInRoom set managerRight = 0 where roomNo = {0} and userNo = {1}", reqBody.roomNo, userNo);

                                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                                            cmd.ExecuteNonQuery();
                                        }
                                        tempKey.Add(temp.Key);
                                        tempRoomNo.Add(reqBody.roomNo);
                                        tempUserNo.Add(userNo);
                                        tempRight.Add(0);
                                        // usersInRoom[temp.Key] = new Tuple<int, int, int>(reqBody.roomNo, userNo, 0);
                                        // 로그 기록
                                        log.Info(string.Format("{0}번 {1} 채팅방에서 {2} 회원에게 관리자 권한이 해제됨", reqBody.roomNo, roomList[reqBody.roomNo].Item2, userList[userNo]));
                                    }
                                }
                            }

                            for (int i = 0; i < tempKey.Count; i++)
                            {
                                usersInRoom[tempKey[i]] = new Tuple<int, int, int>(tempRoomNo[i], tempUserNo[i], tempRight[i]);
                            }

                            PacketMessage resMsg = new PacketMessage();
                            resMsg.Body = new ResponseChangeManagementRightsSuccess()
                            {
                                msg = reqBody.msg
                            };
                            resMsg.Header = new Header()
                            {
                                MSGID = msgid++,
                                MSGTYPE = CONSTANTS.RES_CHANGE_MANAGEMENT_RIGHTS_SUCCESS,
                                BODYLEN = (uint)resMsg.Body.GetSize(),
                                FRAGMENTED = CONSTANTS.NOT_FRAGMENTED,
                                LASTMSG = CONSTANTS.LASTMSG,
                                SEQ = 0
                            };

                            // 채팅방 회원에게 송출
                            foreach (KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(resMsg, userList[temp.Value.Item2]);
                                }
                            }
                            break;
                        }
                    // 파일 전송 준비 요청
                    case CONSTANTS.REQ_SEND_FILE:
                        {
                            RequestSendFile reqBody = (RequestSendFile)message.Body;

                            string msg = message.Header.MSGID + "&" + reqBody.roomNo + "&" + reqBody.filePath + "&" + reqBody.userID;

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
                                msg = reqBody.roomNo + "&" + reqBody.userID + "&" + fileSize + "&" + fileName + "&" + filePath
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
                            foreach(KeyValuePair<int, Tuple<int, int, int>> temp in usersInRoom)
                            {
                                if (temp.Value.Item1.Equals(reqBody.roomNo))
                                {
                                    SendMessageClient(reqMsg, userList[temp.Value.Item2]);
                                }
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

                                        /*
                                        string[] delimiterChars = { ", " };
                                        List<string> users = new List<string>(roomList[pid].Item2.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                                        foreach (string user in users)
                                        {
                                            SendMessageClient(fileMsg, user);
                                        }
                                        */
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