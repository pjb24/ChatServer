using System;
using System.Collections.Generic;
using System.Text;

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

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            userID = temp[0];
            userPW = temp[1];
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

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            No = int.Parse(temp[0]);
            userID = temp[1];
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

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            userID = temp[0];
            userPW = temp[1];
            /*
            userID = msg.Substring(0, msg.LastIndexOf("&"));
            userPW = msg.Substring(msg.LastIndexOf("&"));
            */
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

    // 회원목록 반환
    public class ResponseUserList : ISerializable
    {
        public string msg = string.Empty;
        public Dictionary<int, string> userList = new Dictionary<int, string>();

        public ResponseUserList() { }
        public ResponseUserList(byte[] bytes)
        {
            if (bytes.Length != 0)
            {
                msg = Encoding.Unicode.GetString(bytes);

                string[] delimiterChars = { "&" };
                List<string> users = new List<string>(msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));

                string[] chars = { "^" };
                foreach(string user in users)
                {
                    List<string> temp = new List<string>(user.Split(chars, StringSplitOptions.RemoveEmptyEntries));
                    userList.Add(int.Parse(temp[0]), temp[1]);
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

        public ResponseRoomList() { }
        public ResponseRoomList(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "*" };
            string[] room = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            
            foreach(string temp in room)
            {
                string[] chars = { "&" };
                string[] roomInfo = temp.Split(chars, StringSplitOptions.RemoveEmptyEntries);

                // roomNo, Tuple(accessRight, roomName)
                roomList.Add(int.Parse(roomInfo[0]), new Tuple<int, string>(int.Parse(roomInfo[1]), roomInfo[2]));
                string tempUsers = roomInfo[3];

                string[] cuttingChars = { "^^" };
                string[] roomUsers = tempUsers.Split(cuttingChars, StringSplitOptions.RemoveEmptyEntries);

                foreach(string user in roomUsers)
                {
                    string[] splitChars = { "^" };
                    string[] userInfo = user.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    // usersInRoomNo, Tuple(roomNo, userNo, managerRight)
                    usersInRoom.Add(int.Parse(userInfo[0]), new Tuple<int, int, int>(int.Parse(roomInfo[0]), int.Parse(userInfo[1]), int.Parse(userInfo[2])));
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
        public string creator = string.Empty;
        public List<string> users = new List<string>();

        public RequestCreateRoom() { }
        public RequestCreateRoom(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            accessRight = int.Parse(temp[0]);
            roomName = temp[1];
            creator = temp[2];
            string tempUsers = temp[3];

            string[] chars = { ", " };
            users = new List<string>(tempUsers.Split(chars, StringSplitOptions.RemoveEmptyEntries));
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
        public string creator = string.Empty;
        public int usersInRoomNoCreator = 0;
        public Dictionary<int, Tuple<int, int, int>> usersInRoom = new Dictionary<int, Tuple<int, int, int>>();

        public ResponseCreateRoomSuccess() { }
        public ResponseCreateRoomSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            accessRight = int.Parse(temp[1]);
            roomName = temp[2];
            creator = temp[3];
            usersInRoomNoCreator = int.Parse(temp[4]);
            string tempUsers = temp[5];

            string[] chars = { "^^" };
            string[] userInfos = tempUsers.Split(chars, StringSplitOptions.RemoveEmptyEntries);

            foreach(string userInfo in userInfos)
            {
                string[] cutChars = { "^" };
                string[] tmp = userInfo.Split(cutChars, StringSplitOptions.RemoveEmptyEntries);
                usersInRoom.Add(int.Parse(tmp[0]), new Tuple<int, int, int>(roomNo, int.Parse(tmp[1]), int.Parse(tmp[2])));
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
        public string userID = string.Empty;
        public string chatMsg = string.Empty;

        public RequestChat() { }
        public RequestChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            userID = temp[1];
            chatMsg = temp[2];
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
        public string userID = string.Empty;
        public string chatMsg = string.Empty;

        public ResponseChat() { }
        public ResponseChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            userID = temp[1];
            chatMsg = temp[2];
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
        public List<string> invitedUsers = new List<string>();

        public RequestInvitation() { }
        public RequestInvitation(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            string[] chars = { ", " };
            invitedUsers = new List<string>(temp[1].Split(chars, StringSplitOptions.RemoveEmptyEntries));
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

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            string[] chars = { "^^" };
            string[] invitedUsers = temp[1].Split(chars, StringSplitOptions.RemoveEmptyEntries);

            foreach(string tmp in invitedUsers)
            {
                string[] cutChars = { "^" };
                string[] userInfo = tmp.Split(cutChars, StringSplitOptions.RemoveEmptyEntries);
                usersInRoom.Add(int.Parse(userInfo[0]), new Tuple<int, int, int>(roomNo, int.Parse(userInfo[1]), 0));
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
        public string userID = string.Empty;

        public RequestLeaveRoom() { }
        public RequestLeaveRoom(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            userID = temp[1];
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
        public string userID = string.Empty;

        public ResponseLeaveRoomSuccess() { }
        public ResponseLeaveRoomSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            userID = temp[1];
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
        public long pid = 0;
        public string receivedID = string.Empty;

        public RequestBanishUser() { }
        public RequestBanishUser(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
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
    public class ResponseBanishUser : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;

        public ResponseBanishUser() { }
        public ResponseBanishUser(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
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

    // 채팅방 이름 변경 요청
    public class RequestChangeRoomName : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public string roomName = string.Empty;

        public RequestChangeRoomName() { }
        public RequestChangeRoomName(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            roomName = temp[2];
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

    // 채팅방 이름 변경 완료
    public class ResponseChangeRoomName : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public string roomName = string.Empty;

        public ResponseChangeRoomName() { }
        public ResponseChangeRoomName(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            roomName = temp[2];
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

    // 채팅방 접근 제한 변경 요청
    public class RequestChangeAccessRight : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int accessRight = 0;

        public RequestChangeAccessRight() { }
        public RequestChangeAccessRight(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            accessRight = int.Parse(temp[2]);
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

    // 채팅방 접근 제한 변경 완료
    public class ResponseChangeAccessRight : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int accessRight = 0;

        public ResponseChangeAccessRight() { }
        public ResponseChangeAccessRight(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            accessRight = int.Parse(temp[2]);
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

    // 채팅방 관리자 권한 부여 요청
    public class RequestGrantManagementRights : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int managerRight = 0;

        public RequestGrantManagementRights() { }
        public RequestGrantManagementRights(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            managerRight = int.Parse(temp[2]);
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

    // 채팅방 관리자 권한 부여 완료
    public class ResponseGrantManagementRights : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int managerRight = 0;

        public ResponseGrantManagementRights() { }
        public ResponseGrantManagementRights(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            managerRight = int.Parse(temp[2]);
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

    // 채팅방 관리자 권한 해제 요청
    public class RequestTurnOffManagementRights : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int managerRight = 0;

        public RequestTurnOffManagementRights() { }
        public RequestTurnOffManagementRights(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            managerRight = int.Parse(temp[2]);
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

    // 채팅방 관리자 권한 해제 완료
    public class ResponseTurnOffManagementRights : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;
        public int managerRight = 0;

        public ResponseTurnOffManagementRights() { }
        public ResponseTurnOffManagementRights(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            receivedID = temp[1];
            managerRight = int.Parse(temp[2]);
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
        public string userID = string.Empty;
        public long FILESIZE = 0;
        public string FILENAME = string.Empty;
        public string filePath = string.Empty;

        public RequestSendFile() { }
        public RequestSendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            roomNo = int.Parse(temp[0]);
            userID = temp[1];
            FILESIZE = long.Parse(temp[2]);
            FILENAME = temp[3];
            filePath = temp[4];
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
        public string userID = string.Empty;

        public ResponseSendFile() { }
        public ResponseSendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            MSGID = uint.Parse(temp[0]);
            roomNo = int.Parse(temp[1]);
            filePath = temp[2];
            userID = temp[3];
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
