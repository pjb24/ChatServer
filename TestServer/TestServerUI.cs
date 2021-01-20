using System;
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
        public static TestServerUI testServerUI;

        public TestServerUI()
        {
            InitializeComponent();
            testServerUI = this;
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TestServerUI_Load(object sender, EventArgs e)
        {
            var listen_socket = new AsynchronousSocketListener();
            listen_socket.BeginStartListening(listen_socket.StartListeningCallback, listen_socket);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
