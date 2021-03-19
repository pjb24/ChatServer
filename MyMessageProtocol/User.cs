using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessageProtocol
{
    public class User
    {
        private int no;
        private string userID;
        private string userPW;

        public int No { get => no; set => no = value; }
        public string UserID { get => userID; set => userID = value; }
        public string UserPW { get => userPW; set => userPW = value; }
    }
}
