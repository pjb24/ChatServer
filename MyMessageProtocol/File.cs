using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessageProtocol
{
    public class File
    {
        private uint no;
        private long size;
        private string name;
        private string path;

        private Relation relation = null;

        public uint No { get => no; set => no = value; }
        public long Size { get => size; set => size = value; }
        public string Name { get => name; set => name = value; }
        public string Path { get => path; set => path = value; }
        public Relation Relation { get => relation; set => relation = value; }
    }
}
