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
        public string userID = string.Empty;

        public ResponseRegisterSuccess() { }
        public ResponseRegisterSuccess(byte[] bytes)
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
        public List<string> users = new List<string>();

        public ResponseUserList() { }
        public ResponseUserList(byte[] bytes)
        {
            if (bytes.Length != 0)
            {
                msg = Encoding.Unicode.GetString(bytes);

                string[] delimiterChars = { "&" };
                users = new List<string>(msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
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
    public class RequestGroupList : ISerializable
    {
        public string userID = string.Empty;

        public RequestGroupList() { }
        public RequestGroupList(byte[] bytes)
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
    public class ResponseGroupList : ISerializable
    {
        public string msg = string.Empty;

        public ResponseGroupList() { }
        public ResponseGroupList(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);
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
    public class RequestCreateGroup : ISerializable
    {
        public string msg = string.Empty;
        public string groupName = string.Empty;
        public string group = string.Empty;

        public RequestCreateGroup() { }
        public RequestCreateGroup(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            groupName = temp[0];
            group = temp[1];
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
    public class ResponseCreateGroupSuccess : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string roomName = string.Empty;
        public string users = string.Empty;
        
        public ResponseCreateGroupSuccess() { }
        public ResponseCreateGroupSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            roomName = temp[1];
            users = temp[2];
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
        public long pid = 0;
        public string userID = string.Empty;
        public string chatMsg = string.Empty;

        public RequestChat() { }
        public RequestChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
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
        public long pid = 0;
        public string userID = string.Empty;
        public string chatMsg = string.Empty;

        public ResponseChat() { }
        public ResponseChat(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);            
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
        public long pid = 0;
        public List<string> invitedUsers = new List<string>();

        public RequestInvitation() { }
        public RequestInvitation(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
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
        public long pid = 0;
        public List<string> invitedUsers = new List<string>();

        public ResponseInvitationSuccess() { }
        public ResponseInvitationSuccess(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
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

    // 채팅방 나가기 요청
    public class RequestLeaveGroup : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string user = string.Empty;

        public RequestLeaveGroup() { }
        public RequestLeaveGroup(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            pid = long.Parse(temp[0]);
            user = temp[1];
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
    public class ResponseLeaveGroupSuccess : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public string receivedID = string.Empty;

        public ResponseLeaveGroupSuccess() { }
        public ResponseLeaveGroupSuccess(byte[] bytes)
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

    // 파일 전송 요청 메시지(0x01)에 사용할 본문 클래스
    public class RequestSendFile : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
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
            pid = long.Parse(temp[0]);
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
        public long pid = 0;
        public string filePath = string.Empty;
        public string userID = string.Empty;

        public ResponseSendFile() { }
        public ResponseSendFile(byte[] bytes)
        {
            msg = Encoding.Unicode.GetString(bytes);

            string[] delimiterChars = { "&" };
            string[] temp = msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            MSGID = uint.Parse(temp[0]);
            pid = long.Parse(temp[1]);
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
