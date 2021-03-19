using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MyMessageProtocol
{
    // 회원가입 요청 ID, PW
    public class RequestRegister : ISerializable
    {
        public string msg = string.Empty;
        public string userID = string.Empty;
        public string userPW = string.Empty;

        public RequestRegister() { }
        public RequestRegister(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            User user = JsonConvert.DeserializeObject<User>(msg);
            userID = user.UserID;
            userPW = user.UserPW;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 회원가입 성공
    public class ResponseRegisterSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int No = 0;
        public string userID = string.Empty;

        public ResponseRegisterSuccess() { }
        public ResponseRegisterSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            User user = JsonConvert.DeserializeObject<User>(msg);

            No = user.No;
            userID = user.UserID;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 로그인 요청 ID, PW
    public class RequestSignIn : ISerializable
    {
        public string msg = string.Empty;
        public string userID = string.Empty;
        public string userPW = string.Empty;

        public RequestSignIn() { }
        public RequestSignIn(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            User user = JsonConvert.DeserializeObject<User>(msg);

            userID = user.UserID;
            userPW = user.UserPW;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 로그인 성공
    public class ResponseSignInSuccess : ISerializable
    {
        public string userID = string.Empty;

        public ResponseSignInSuccess() { }
        public ResponseSignInSuccess(byte[] bytes)
        {
            userID = Encoding.Unicode.GetString(bytes);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(userID);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(userID).Length;
        }
    }

    // 로그아웃 요청 ID
    public class RequestSignOut : ISerializable
    {
        public string userID = string.Empty;

        public RequestSignOut() { }
        public RequestSignOut(byte[] bytes)
        {
            userID = Encoding.Unicode.GetString(bytes);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(userID);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(userID).Length;
        }
    }

    // 로그아웃 완료
    public class ResponseSignOutSuccess : ISerializable
    {
        public string userID = string.Empty;

        public ResponseSignOutSuccess() { }
        public ResponseSignOutSuccess(byte[] bytes)
        {
            userID = Encoding.Unicode.GetString(bytes);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(userID);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(userID).Length;
        }
    }

    // 회원목록 반환
    public class ResponseUserList : ISerializable
    {
        public string msg = string.Empty;
        public Dictionary<int, string> userList = new Dictionary<int, string>();
        private List<User> users = new List<User>();

        public ResponseUserList() { }
        public ResponseUserList(byte[] bytes)
        {
            if (bytes.Length != 0)
            {
                msg = Encoding.Unicode.GetString(bytes);

                users = JsonConvert.DeserializeObject<List<User>>(msg);

                foreach(User user in users)
                {
                    if (!userList.ContainsKey(user.No))
                    {
                        userList.Add(user.No, user.UserID);
                    }
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 목록 요청
    public class RequestRoomList : ISerializable
    {
        public string userID = string.Empty;

        public RequestRoomList() { }
        public RequestRoomList(byte[] bytes)
        {
            userID = Encoding.Unicode.GetString(bytes);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(userID);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(userID).Length;
        }
    }

    // 채팅방 목록 반환
    public class ResponseRoomList : ISerializable
    {
        public string msg = string.Empty;
        public Dictionary<int, Tuple<int, string>> roomList = new Dictionary<int, Tuple<int, string>>();
        public Dictionary<int, Tuple<int, int, int>> usersInRoom = new Dictionary<int, Tuple<int, int, int>>();
        private List<Room> rooms = new List<Room>();

        public ResponseRoomList() { }
        public ResponseRoomList(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            rooms = JsonConvert.DeserializeObject<List<Room>>(msg);
            
            foreach(Room room in rooms)
            {
                // roomNo, Tuple(accessRight, roomName)
                if (!roomList.ContainsKey(room.No))
                {
                    roomList.Add(room.No, new Tuple<int, string>(room.AccessRight, room.Name));
                }
                
                if (room.Relation != null)
                {
                    foreach (Relation relation in room.Relation)
                    {
                        // usersInRoomNo, Tuple(roomNo, userNo, managerRight)
                        if (!usersInRoom.ContainsKey(relation.No))
                        {
                            usersInRoom.Add(relation.No, new Tuple<int, int, int>(room.No, relation.UserNo, relation.ManagerRight));
                        }
                    }
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 온라인 회원 목록 반환
    public class ResponseOnlineUserList : ISerializable
    {
        public string msg = string.Empty;
        public List<string> onlineUserList = new List<string>();
        private List<User> onlineUsers = new List<User>();

        public ResponseOnlineUserList() { }
        public ResponseOnlineUserList(byte[] bytes)
        {
            if (bytes.Length != 0)
            {
                msg = Encoding.Unicode.GetString(bytes);

                onlineUsers = JsonConvert.DeserializeObject<List<User>>(msg);

                foreach (User onlineUser in onlineUsers)
                {
                    if (!onlineUserList.Contains(onlineUser.UserID))
                    {
                        onlineUserList.Add(onlineUser.UserID);
                    }
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 생성 요청
    public class RequestCreateRoom : ISerializable
    {
        public string msg = string.Empty;
        public int accessRight = 0;
        public string roomName = string.Empty;
        public int creatorNo = 0;
        public List<int> users = new List<int>();

        public RequestCreateRoom() { }
        public RequestCreateRoom(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            accessRight = room.AccessRight;
            roomName = room.Name;
            creatorNo = room.Relation[0].UserNo;
            foreach(Relation relation in room.Relation)
            {
                if (relation.UserNo != creatorNo)
                {
                    users.Add(relation.UserNo);
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 생성 완료
    public class ResponseCreateRoomSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int accessRight = 0;
        public string roomName = string.Empty;
        public Dictionary<int, Tuple<int, int, int>> usersInRoom = new Dictionary<int, Tuple<int, int, int>>();

        public ResponseCreateRoomSuccess() { }
        public ResponseCreateRoomSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;
            accessRight = room.AccessRight;
            roomName = room.Name;

            foreach(Relation relation in room.Relation)
            {
                usersInRoom.Add(relation.No, new Tuple<int, int, int>(roomNo, relation.UserNo, relation.ManagerRight));
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅 메시지 발송 요청
    public class RequestChat : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public string encrypted = string.Empty;

        public RequestChat() { }
        public RequestChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            encrypted = temp[1];
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅 메시지 발송
    public class ResponseChat : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public string encrypted = string.Empty;

        public ResponseChat() { }
        public ResponseChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            encrypted = temp[1];
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 초대 요청
    public class RequestInvitation : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public List<int> invitedUsers = new List<int>();

        public RequestInvitation() { }
        public RequestInvitation(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;
            foreach (Relation relation in room.Relation)
            {
                invitedUsers.Add(relation.UserNo);
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 초대 완료
    public class ResponseInvitationSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public Dictionary<int, Tuple<int, int, int>> usersInRoom = new Dictionary<int, Tuple<int, int, int>>();

        public ResponseInvitationSuccess() { }
        public ResponseInvitationSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;

            foreach(Relation relation in room.Relation)
            {
                usersInRoom.Add(relation.No, new Tuple<int, int, int>(roomNo, relation.UserNo, 0));
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 나가기 요청
    public class RequestLeaveRoom : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int userNo = 0;

        public RequestLeaveRoom() { }
        public RequestLeaveRoom(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Relation relation = JsonConvert.DeserializeObject<Relation>(msg);

            roomNo = relation.RoomNo;
            userNo = relation.UserNo;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 나가기 완료
    public class ResponseLeaveRoomSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int userNo = 0;

        public ResponseLeaveRoomSuccess() { }
        public ResponseLeaveRoomSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Relation relation = JsonConvert.DeserializeObject<Relation>(msg);

            roomNo = relation.RoomNo;
            userNo = relation.UserNo;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 추방 요청
    public class RequestBanishUser : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int banishedUserNo = 0;

        public RequestBanishUser() { }
        public RequestBanishUser(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Relation relation = JsonConvert.DeserializeObject<Relation>(msg);

            roomNo = relation.RoomNo;
            banishedUserNo = relation.UserNo;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 추방 완료
    public class ResponseBanishUserSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int banishedUserNo = 0;

        public ResponseBanishUserSuccess() { }
        public ResponseBanishUserSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Relation relation = JsonConvert.DeserializeObject<Relation>(msg);

            roomNo = relation.RoomNo;
            banishedUserNo = relation.UserNo;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 설정 변경 요청
    public class RequestChangeRoomConfig : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int accessRight = 0;
        public string roomName = string.Empty;

        public RequestChangeRoomConfig() { }
        public RequestChangeRoomConfig(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;
            accessRight = room.AccessRight;
            roomName = room.Name;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 설정 변경 완료
    public class ResponseChangeRoomConfigSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int accessRight = 0;
        public string roomName = string.Empty;

        public ResponseChangeRoomConfigSuccess() { }
        public ResponseChangeRoomConfigSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;
            accessRight = room.AccessRight;
            roomName = room.Name;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 관리자 권한 변경 요청
    public class RequestChangeManagementRights : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public List<int> changedUsersNo = new List<int>();

        public RequestChangeManagementRights() { }
        public RequestChangeManagementRights(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;
            
            foreach(Relation relation in room.Relation)
            {
                changedUsersNo.Add(relation.UserNo);
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 채팅방 관리자 권한 변경 완료
    public class ResponseChangeManagementRightsSuccess : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public List<int> changedUsersNo = new List<int>();

        public ResponseChangeManagementRightsSuccess() { }
        public ResponseChangeManagementRightsSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            Room room = JsonConvert.DeserializeObject<Room>(msg);

            roomNo = room.No;

            foreach (Relation relation in room.Relation)
            {
                changedUsersNo.Add(relation.UserNo);
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    // 파일 전송 요청 메시지(0x01)에 사용할 본문 클래스
    public class RequestSendFile : ISerializable
    {
        public string msg = string.Empty;
        public int roomNo = 0;
        public int userNo = 0;
        public long FILESIZE = 0;
        public string FILENAME = string.Empty;
        public string filePath = string.Empty;

        public RequestSendFile() { }
        public RequestSendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            File file = JsonConvert.DeserializeObject<File>(msg);

            roomNo = file.Relation.RoomNo;
            userNo = file.Relation.UserNo;
            FILESIZE = file.Size;
            FILENAME = file.Name;
            filePath = file.Path;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }

    public class ResponseSendFile : ISerializable
    {
        public string msg = string.Empty;
        public uint MSGID = 0;
        public int roomNo = 0;
        public string filePath = string.Empty;
        public int userNo = 0;

        public ResponseSendFile() { }
        public ResponseSendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            File file = JsonConvert.DeserializeObject<File>(msg);

            MSGID = file.No;
            roomNo = file.Relation.RoomNo;
            filePath = file.Path;
            userNo = file.Relation.UserNo;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
            // return sizeof(uint) + sizeof(byte);
        }
    }

    // 실제 파일을 전송하는 메시지(0x03)에 사용할 본문 클래스
    public class RequestSendFileData : ISerializable
    {
        public byte[] DATA;

        public RequestSendFileData(byte[] bytes)
        {
            DATA = new byte[bytes.Length];
            bytes.CopyTo(DATA, 0);
        }

        public byte[] GetBytes()
        {
            return DATA;
        }

        public int GetSize()
        {
            return DATA.Length;
        }
    }

    // 파일 전송 결과 메시지(0x04)에 사용할 본문 클래스
    public class ResponseFileSendComplete : ISerializable
    {
        public uint MSGID;
        public byte RESULT;

        public ResponseFileSendComplete() { }
        public ResponseFileSendComplete(byte[] bytes)
        {
            MSGID = BitConverter.ToUInt32(bytes, 0);
            RESULT = bytes[4];
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            byte[] temp = BitConverter.GetBytes(MSGID);
            Array.Copy(temp, 0, bytes, 0, temp.Length);
            bytes[temp.Length] = RESULT;

            return bytes;
        }

        public int GetSize()
        {
            return sizeof(uint) + sizeof(byte);
        }
    }

    public class SendFile : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string userID = string.Empty;
        public string FILENAME = string.Empty;
        public long FILESIZE = 0;
        public byte[] DATA;

        public SendFile() { }
        public SendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&^%$#&^%$&^%$" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            pid = long.Parse(temp[0]);
            userID = temp[1];
            FILENAME = temp[2];
            FILESIZE = long.Parse(temp[3]);
            DATA = Encoding.Unicode.GetBytes(temp[4]);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return Encoding.Unicode.GetBytes(msg).Length;
        }
    }
}
