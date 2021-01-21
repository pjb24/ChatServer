using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;

namespace TestServer
{
    public partial class TestServerUI : Form
    {
        public static TestServerUI testServerUI;

        Thread workerThread;

        public TestServerUI()
        {
            InitializeComponent();
            testServerUI = this;
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            workerThread.Interrupt();
            this.Close();
        }

        private void TestServerUI_Load(object sender, EventArgs e)
        {
            workerThread = new Thread(AsynchronousSocketListener.StartListening);
            workerThread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
