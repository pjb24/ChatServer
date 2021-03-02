using System;
using System.Text;

namespace MyMessageProtocol
{
    public class CONSTANTS
    {
        // Message Type - 명령어 정의

        // 회원가입 요청
        public const uint REQ_REGISTER = 0X11;
        // 회원가입 성공
        public const uint RES_REGISTER_SUCCESS = 0X12;
        // 회원가입 실패 - 이미 등록된 사용자
        public const uint RES_REGISTER_FAIL_EXIST = 0X13;

        // 로그인 요청
        public const uint REQ_SIGNIN = 0X21;
        // 로그인 성공
        public const uint RES_SIGNIN_SUCCESS = 0X22;
        // 로그인 실패 - 등록되지 않은 사용자
        public const uint RES_SIGNIN_FAIL_NOT_EXIST = 0X23;
        // 로그인 실패 - 잘못된 비밀번호
        public const uint RES_SIGNIN_FAIL_WRONG_PASSWORD = 0X24;
        // 로그인 실패 - 접속 중인 사용자
        public const uint RES_SIGNIN_FAIL_ONLINE_USER = 0X25;

        // 로그아웃 통보
        public const uint REQ_SIGNOUT = 0X26;

        // 회원목록 요청
        public const uint REQ_USERLIST = 0X31;
        // 회원목록 반환
        public const uint RES_USERLIST = 0X32;

        // 채팅방 목록 요청
        public const uint REQ_ROOMLIST = 0X33;
        // 채팅방 목록 반환
        public const uint RES_ROOMLIST = 0X34;

        // 채팅방 생성 요청
        public const uint REQ_CREATE_ROOM = 0X41;
        // 채팅방 생성 완료
        public const uint RES_CREATE_ROOM_SUCCESS = 0X42;

        // 채팅 메시지 발송 요청
        public const uint REQ_CHAT = 0X51;
        // 채팅 메시지 발송
        public const uint RES_CHAT = 0X52;

        // 채팅방 초대 요청
        public const uint REQ_INVITATION = 0X53;
        // 채팅방 초대 완료
        public const uint RES_INVITATION_SUCCESS = 0X54;

        // 채팅방 나가기 요청
        public const uint REQ_LEAVE_ROOM = 0X55;
        // 채팅방 나가기 완료
        public const uint RES_LEAVE_ROOM_SUCCESS = 0X56;


        // 채팅방 관리자 사용 프로토콜
        // 채팅방 추방 요청
        public const uint REQ_BANISH_USER = 0X61;
        // 채팅방 추방 완료
        public const uint RES_BANISH_USER_SUCCESS = 0X62;

        // 채팅방 이름 변경 요청
        public const uint REQ_CHANGE_ROOM_CONFIG = 0X63;
        // 채팅방 이름 변경 완료
        public const uint RES_CHANGE_ROOM_CONFIG_SUCCESS = 0X64;


        // 채팅방 생성자 사용 프로토콜
        // 채팅방 관리자 권한 부여 요청
        public const uint REQ_CHANGE_MANAGEMENT_RIGHTS = 0X71;
        // 채팅방 관리자 권한 부여 완료
        public const uint RES_CHANGE_MANAGEMENT_RIGHTS_SUCCESS = 0X72;

        // 파일 전송 준비 요청
        public const uint REQ_SEND_FILE = 0X81;
        // 파일 전송 준비 완료
        public const uint RES_SEND_FILE = 0X82;
        // 파일 DATA 전송
        public const uint REQ_SEND_FILE_DATA = 0X83;
        // 파일 DATA 전송 완료
        public const uint RES_FILE_SEND_COMPLETE = 0X84;

        // 파일 전송
        public const uint SEND_FILE = 0X85;

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
