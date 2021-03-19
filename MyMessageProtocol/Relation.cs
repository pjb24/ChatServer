using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessageProtocol
{
    public class Relation
    {
        private int no;
        private int roomNo;
        private int userNo;
        private int managerRight;

        public int No { get => no; set => no = value; }
        public int RoomNo { get => roomNo; set => roomNo = value; }
        public int UserNo { get => userNo; set => userNo = value; }
        public int ManagerRight { get => managerRight; set => managerRight = value; }
    }
}
