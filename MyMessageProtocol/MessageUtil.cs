using System;
using System.IO;

namespace MyMessageProtocol
{
    // 스트림으로부터 메시지를 보내고 받기 위한 클래스
    public class MessageUtil
    {
        // 메시지를 내보냄
        public static void Send(Stream writer, PacketMessage msg)
        {
            writer.Write(msg.GetBytes(), 0, msg.GetSize());
            writer.Flush();
        }

        public static PacketMessage Receive(Stream reader)
        {
            int totalRecv = 0;
            int sizeToRead = 16;
            byte[] hBuffer = new byte[sizeToRead];

            while (sizeToRead > 0)
            {
                byte[] buffer = new byte[sizeToRead];
                int recv = reader.Read(buffer, 0, sizeToRead);
                if (recv == 0)
                    return null;

                buffer.CopyTo(hBuffer, totalRecv);
                totalRecv += recv;
                sizeToRead -= recv;
            }

            Header header = new Header(hBuffer);

            totalRecv = 0;
            byte[] bBuffer = new byte[header.BODYLEN];
            sizeToRead = (int)header.BODYLEN;

            while (sizeToRead > 0)
            {
                byte[] buffer = new byte[sizeToRead];
                int recv = reader.Read(buffer, 0, sizeToRead);
                if (recv == 0)
                    return null;

                buffer.CopyTo(bBuffer, totalRecv);
                totalRecv += recv;
                sizeToRead -= recv;
            }

            ISerializable body = null;

            // 헤더의 MSGTYPE 프로퍼티를 통해 어떤 Body 클래스의 생성자를 호출할지 결정
            switch (header.MSGTYPE)
            {
                case CONSTANTS.REQ_REGISTER:
                    body = new RequestRegister(bBuffer, header);
                    break;
                case CONSTANTS.RES_REGISTER_SUCCESS:
                    body = new ResponseRegisterSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_SIGNIN:
                    body = new RequestSignIn(bBuffer, header);
                    break;
                case CONSTANTS.RES_SIGNIN_SUCCESS:
                    body = new ResponseSignInSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_SIGNOUT:
                    body = new RequestSignOut(bBuffer, header);
                    break;
                case CONSTANTS.RES_SIGNOUT_SUCCESS:
                    body = new ResponseSignOutSuccess(bBuffer, header);
                    break;
                case CONSTANTS.RES_USERLIST:
                    body = new ResponseUserList(bBuffer, header);
                    break;
                case CONSTANTS.REQ_ROOMLIST:
                    body = new RequestRoomList(bBuffer, header);
                    break;
                case CONSTANTS.RES_ROOMLIST:
                    body = new ResponseRoomList(bBuffer, header);
                    break;
                case CONSTANTS.RES_ONLINE_USERLIST:
                    body = new ResponseOnlineUserList(bBuffer, header);
                    break;
                case CONSTANTS.REQ_CREATE_ROOM:
                    body = new RequestCreateRoom(bBuffer, header);
                    break;
                case CONSTANTS.RES_CREATE_ROOM_SUCCESS:
                    body = new ResponseCreateRoomSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_CHAT:
                    body = new RequestChat(bBuffer, header);
                    break;
                case CONSTANTS.RES_CHAT:
                    body = new ResponseChat(bBuffer, header);
                    break;
                case CONSTANTS.REQ_INVITATION:
                    body = new RequestInvitation(bBuffer, header);
                    break;
                case CONSTANTS.RES_INVITATION_SUCCESS:
                    body = new ResponseInvitationSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_LEAVE_ROOM:
                    body = new RequestLeaveRoom(bBuffer, header);
                    break;
                case CONSTANTS.RES_LEAVE_ROOM_SUCCESS:
                    body = new ResponseLeaveRoomSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_BANISH_USER:
                    body = new RequestBanishUser(bBuffer, header);
                    break;
                case CONSTANTS.RES_BANISH_USER_SUCCESS:
                    body = new ResponseBanishUserSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_CHANGE_ROOM_CONFIG:
                    body = new RequestChangeRoomConfig(bBuffer, header);
                    break;
                case CONSTANTS.RES_CHANGE_ROOM_CONFIG_SUCCESS:
                    body = new ResponseChangeRoomConfigSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_CHANGE_MANAGEMENT_RIGHTS:
                    body = new RequestChangeManagementRights(bBuffer, header);
                    break;
                case CONSTANTS.RES_CHANGE_MANAGEMENT_RIGHTS_SUCCESS:
                    body = new ResponseChangeManagementRightsSuccess(bBuffer, header);
                    break;
                case CONSTANTS.REQ_SEND_FILE:
                    body = new RequestSendFile(bBuffer, header);
                    break;
                case CONSTANTS.RES_SEND_FILE:
                    body = new ResponseSendFile(bBuffer, header);
                    break;
                case CONSTANTS.REQ_SEND_FILE_DATA:
                    body = new RequestSendFileData(bBuffer);
                    break;
                case CONSTANTS.RES_FILE_SEND_COMPLETE:
                    body = new ResponseFileSendComplete(bBuffer, header);
                    break;
                default:
                    // throw new Exception(String.Format("Unknown MSGTYPE: {0}" + header.MSGTYPE));
                    break;
            }

            return new PacketMessage() { Header = header, Body = body };
        }
    }
}
