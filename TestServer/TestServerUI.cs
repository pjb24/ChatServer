﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        public TestServerUI()
        {
            InitializeComponent();
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TestServerUI_Load(object sender, EventArgs e)
        {
            AsynchronousSocketListener.StartListening();
        }
    }
}
