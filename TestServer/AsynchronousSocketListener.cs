using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestServer
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Size of receive buffer.
        public const int BufferSize = 1024;

        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }

    static class Constants
    {
        public const int MAX_CLIENT_COUNT = 10;
    }

    class AsynchronousSocketListener
    {
        private static Socket[] client_list = new Socket[Constants.MAX_CLIENT_COUNT];
        static int client_count;
        static string[] client_ip = new string[Constants.MAX_CLIENT_COUNT];

        // delegate 생성
        private Action StartListeningDelegate;

        // Tread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
            client_count = 0;
            this.StartListeningDelegate = StartListening;
        }

        // Window Form ListBox 사용 메서드
        private static void WriteListBoxSafe(String text)
        {
            if (TestServerUI.testServerUI.lb_Result.InvokeRequired)
            {
                TestServerUI.testServerUI.lb_Result.Invoke((MethodInvoker) delegate()
                {
                    WriteListBoxSafe(text);
                });
            } else
            {
                TestServerUI.testServerUI.lb_Result.Items.Add(text);
                TestServerUI.testServerUI.lb_Result.SetSelected(TestServerUI.testServerUI.lb_Result.Items.Count - 1, true);
            }
        }

        // End메서드 호출할 Callback 메서드
        public void StartListeningCallback(IAsyncResult ar)
        {
            var async = ar.AsyncState as AsynchronousSocketListener;
            async.EndStartListening(ar);
        }

        // BeginInvoke할 메서드
        public IAsyncResult BeginStartListening(AsyncCallback asyncCallback, object state)
        {
            return StartListeningDelegate.BeginInvoke(asyncCallback, state);
        }

        // EndInvoke할 메서드
        public void EndStartListening(IAsyncResult asyncResult)
        {
            this.StartListeningDelegate.EndInvoke(asyncResult);
        }

        // 작업 진행 메서드
        public static void StartListening()
        {
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listenner is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    WriteListBoxSafe("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    
                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            WriteListBoxSafe("\nPress ENTER to continue...");
            Console.Read();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.
                allDone.Set();

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = handler;

                client_list[client_count] = state.workSocket;
                client_ip[client_count] = state.workSocket.RemoteEndPoint.ToString();
                WriteListBoxSafe("새로운 클라이언트가 접속했습니다. : " + client_ip[client_count]);

                client_count += 1;

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    WriteListBoxSafe("Read " + content.Length + " bytes from socket. \n Data : " + content);
                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);

                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                WriteListBoxSafe("Sent " + bytesSent + " bytes to client.");

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void ClientCloseProcess(Socket socket)
        {
            socket.Close();

            for (int i = 0; i < client_count; i++)
            {
                if(client_list[i] == socket)
                {
                    client_count--;
                    if(i != client_count)
                    {
                        client_list[i] = client_list[client_count];
                        client_ip[i] = client_ip[client_count];
                    }
                }
            }
        }
    }
}
