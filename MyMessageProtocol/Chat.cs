using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessageProtocol
{
    public class Chat
    {
        private int roomNo;
        private string userID;
        private string chatMsg;

        public int RoomNo { get => roomNo; set => roomNo = value; }
        public string UserID { get => userID; set => userID = value; }
        public string ChatMsg { get => chatMsg; set => chatMsg = value; }
    }
}
