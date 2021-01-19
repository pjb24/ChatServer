using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        private Socket mh_listen_socket;

        public TestServerUI()
        {
            InitializeComponent();

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            mh_listen_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            mh_listen_socket.Bind(localEndPoint);
            
            mh_listen_socket.Listen(10);

            mh_listen_socket.Accept();
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TestServerUI_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AsynchronousSocketListener.StartListening();
        }
    }
}
