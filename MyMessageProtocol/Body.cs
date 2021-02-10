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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 회원가입 성공
    public class ResponseRegisterSuccess : ISerializable
    {
        public string userID = string.Empty;

        public ResponseRegisterSuccess() { }
        public ResponseRegisterSuccess(byte[] bytes)
        {
            userID = BitConverter.ToString(bytes, 0);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(userID);

            return bytes;
        }

        public int GetSize()
        {
            return userID.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 회원목록 반환
    public class ResponseUserList : ISerializable
    {
        public string msg = string.Empty;
        public List<string> users = null;

        public ResponseUserList() { }
        public ResponseUserList(byte[] bytes)
        {
            if (bytes.Length != 0)
            {
                msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 채팅방 목록 반환
    public class ResponseGroupList : ISerializable
    {
        public string msg = string.Empty;
        public List<string> groups = null;

        public ResponseGroupList() { }
        public ResponseGroupList(byte[] bytes)
        {
            msg = BitConverter.ToString(bytes, 0);

            string[] delimiterChars = { "&" };
            groups = new List<string>(msg.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            bytes = Encoding.Unicode.GetBytes(msg);

            return bytes;
        }

        public int GetSize()
        {
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 채팅방 초대 요청
    public class RequestInvitation : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public List<string> invitedUsers = null;

        public RequestInvitation() { }
        public RequestInvitation(byte[] bytes)
        {
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 채팅방 초대 완료
    public class ResponseInvitationSuccess : ISerializable
    {
        public string msg = string.Empty;
        public long pid = 0;
        public List<string> invitedUsers = null;

        public ResponseInvitationSuccess() { }
        public ResponseInvitationSuccess(byte[] bytes)
        {
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
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
            msg = BitConverter.ToString(bytes, 0);

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
            return msg.Length;
        }
    }

    // 파일 전송 요청 메시지(0x01)에 사용할 본문 클래스
    public class BodyRequest : ISerializable
    {
        public long FILESIZE;
        public byte[] FILENAME;

        public BodyRequest() { }
        public BodyRequest(byte[] bytes)
        {
            FILESIZE = BitConverter.ToInt64(bytes, 0);
            FILENAME = new byte[bytes.Length - sizeof(long)];
            Array.Copy(bytes, sizeof(long), FILENAME, 0, FILENAME.Length);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            byte[] temp = BitConverter.GetBytes(FILESIZE);
            Array.Copy(temp, 0, bytes, 0, temp.Length);
            Array.Copy(FILENAME, 0, bytes, temp.Length, FILENAME.Length);

            return bytes;
        }

        public int GetSize()
        {
            return sizeof(long) + FILENAME.Length;
        }
    }

    public class BodyResponse : ISerializable
    {
        public uint MSGID;
        public byte RESPONSE;
        public BodyResponse() { }
        public BodyResponse(byte[] bytes)
        {
            MSGID = BitConverter.ToUInt32(bytes, 0);
            RESPONSE = bytes[4];
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];
            byte[] temp = BitConverter.GetBytes(MSGID);
            Array.Copy(temp, 0, bytes, 0, temp.Length);
            bytes[temp.Length] = RESPONSE;

            return bytes;
        }

        public int GetSize()
        {
            return sizeof(uint) + sizeof(byte);
        }
    }

    // 실제 파일을 전송하는 메시지(0x03)에 사용할 본문 클래스
    public class BodyData : ISerializable
    {
        public byte[] DATA;

        public BodyData(byte[] bytes)
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
    public class BodyResult : ISerializable
    {
        public uint MSGID;
        public byte RESULT;

        public BodyResult() { }
        public BodyResult(byte[] bytes)
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
}
