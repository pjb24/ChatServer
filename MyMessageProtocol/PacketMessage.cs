using System;
using System.Text;

namespace MyMessageProtocol
{
    public class CONSTANTS
    {
        // Message Type - 명령어 정의

        // 회원가입 요청
        public const uint REQ_REGISTER = 0X01;
        // 회원가입 성공
        public const uint RES_REGISTER_SUCCESS = 0X02;
        // 회원가입 실패 - 이미 존재하는 사용자
        public const uint RES_REGISTER_FAIL_EXIST = 0X03;

        // 로그인 요청
        public const uint REQ_SIGNIN = 0X04;
        // 로그인 성공
        public const uint RES_SIGNIN_SUCCESS = 0X05;
        // 로그인 실패 - 존재하지 않는 사용자
        public const uint RES_SIGNIN_FAIL_NOT_EXIST = 0X06;
        // 로그인 실패 - 잘못된 비밀번호
        public const uint RES_SIGNIN_FAIL_WRONG_PASSWORD = 0X07;
        // 로그인 실패 - 이미 접속 중인 사용자
        public const uint RES_SIGNIN_FAIL_ONLINE_USER = 0X08;

        // 로그아웃 통보
        public const uint REQ_SIGNOUT = 0X09;

        // 회원목록 요청
        public const uint REQ_USERLIST = 0X10;
        // 회원목록 반환
        public const uint RES_USERLIST = 0X11;

        // 채팅방목록 요청
        public const uint REQ_GROUPLIST = 0X12;
        // 채팅방목록 반환
        public const uint RES_GROUPLIST = 0X13;

        // 채팅방 생성 요청
        public const uint REQ_CREATE_GROUP = 0X14;
        // 채팅방 생성 완료
        public const uint RES_CREATE_GROUP_SUCCESS = 0X15;

        // 채팅 메시지 발송 요청
        public const uint REQ_CHAT = 0X16;
        // 채팅 메시지 발송
        public const uint RES_CHAT = 0X17;

        // 채팅방 초대 요청
        public const uint REQ_INVITATION = 0X18;
        // 채팅방 초대 완료
        public const uint RES_INVITATION_SUCCESS = 0X19;

        // 채팅방 나가기 요청
        public const uint REQ_LEAVE_GROUP = 0X20;
        // 채팅방 나가기 완료
        public const uint RES_LEAVE_GROUP_SUCCESS = 0X21;

        // 파일 전송 준비 요청
        public const uint REQ_SEND_FILE = 0X22;
        // 파일 전송 준비 완료
        public const uint RES_SEND_FILE = 0X23;
        // 파일 DATA 전송
        public const uint REQ_SEND_FILE_DATA = 0X24;
        // 파일 DATA 전송 완료
        public const uint RES_FILE_SEND_COMPLETE = 0X25;

        // 파일 전송
        public const uint SEND_FILE = 0X26;

        // 조각화 유무 정의
        public const byte NOT_FRAGMENTED = 0X00;
        public const byte FRAGMENT = 0X01;

        // 조각화 메시지 - 마지막 판정
        public const byte NOT_LASTMSG = 0X00;
        public const byte LASTMSG = 0X01;

        // 연결 승인 / 거절
        public const byte DENIED = 0X00;
        public const byte ACCEPTED = 0X01;

        // 전송 성공 / 실패
        public const byte FAIL = 0X00;
        public const byte SUCCESS = 0X01;
    }

    public interface ISerializable
    {
        byte[] GetBytes();
        int GetSize();
    }

    public class PacketMessage : ISerializable
    {
        public Header Header { get; set; }
        public ISerializable Body { get; set; }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[GetSize()];

            Header.GetBytes().CopyTo(bytes, 0);
            if (Body != null)
            {
                Body.GetBytes().CopyTo(bytes, Header.GetSize());
            }
            
            return bytes;
        }

        public int GetSize()
        {
            if (Body != null)
            {
                return Header.GetSize() + Body.GetSize();
            }
            else
            {
                return Header.GetSize();
            }
        }
    }
}
