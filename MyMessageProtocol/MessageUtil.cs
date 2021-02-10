﻿using System;
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
                    body = new RequestRegister(bBuffer);
                    break;
                case CONSTANTS.REQ_SIGNIN:
                    body = new RequestSignIn(bBuffer);
                    break;
                case CONSTANTS.RES_USERLIST:
                    body = new ResponseUserList(bBuffer);
                    break;
                case CONSTANTS.RES_GROUPLIST:
                    body = new ResponseGroupList(bBuffer);
                    break;
                case CONSTANTS.REQ_CREATE_GROUP:
                    body = new RequestCreateGroup(bBuffer);
                    break;
                case CONSTANTS.RES_CREATE_GROUP_SUCCESS:
                    body = new ResponseCreateGroupSuccess(bBuffer);
                    break;
                case CONSTANTS.REQ_CHAT:
                    body = new RequestChat(bBuffer);
                    break;
                case CONSTANTS.RES_CHAT:
                    body = new ResponseChat(bBuffer);
                    break;
                case CONSTANTS.REQ_INVITATION:
                    body = new RequestInvitation(bBuffer);
                    break;
                case CONSTANTS.RES_INVITATION_SUCCESS:
                    body = new ResponseInvitationSuccess(bBuffer);
                    break;
                case CONSTANTS.REQ_LEAVE_GROUP:
                    body = new RequestLeaveGroup(bBuffer);
                    break;
                case CONSTANTS.RES_LEAVE_GROUP_SUCCESS:
                    body = new ResponseLeaveGroupSuccess(bBuffer);
                    break;
                default:
                    // throw new Exception(String.Format("Unknown MSGTYPE: {0}" + header.MSGTYPE));
                    break;
            }

            return new PacketMessage() { Header = header, Body = body };
        }
    }
}
