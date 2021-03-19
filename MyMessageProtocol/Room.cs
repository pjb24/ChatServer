using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessageProtocol
{
    public class Room
    {
        private int no;
        private int accessRight;
        private string name;
        private List<Relation> relation;

        public int No { get => no; set => no = value; }
        public int AccessRight { get => accessRight; set => accessRight = value; }
        public string Name { get => name; set => name = value; }
        public List<Relation> Relation { get => relation; set => relation = value; }
    }
}
