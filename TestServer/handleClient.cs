using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using System.Windows.Forms;

using System.Collections.Generic;

namespace TestServer
{
    class handleClient
    {
        // class에서 사용할 변수 선언
        TcpClient clientSocket = null;

        public void startClient(TcpClient clientSocket)
        {
            // socket 정보 저장
            this.clientSocket = clientSocket;

            // doChat Thread 생성
            Thread t_hanlder = new Thread(doChat);
            t_hanlder.IsBackground = true;
            // doChat Thread 시작
            t_hanlder.Start();
        }

        // delegate (대리자) 설정, 매개변수를 전달
        public delegate void MessageDisplayHandler(string message, TcpClient client);
        // 외부에 이벤트 발생을 알리기 위함
        public event MessageDisplayHandler OnReceived;

        public delegate void DisconnectedHandler(TcpClient clientSocket);
        public event DisconnectedHandler OnDisconnected;

        private void doChat()
        {
            // 함수 내에서 사용할 변수 선언
            NetworkStream stream = null;
            try
            {
                // 초기화
                byte[] buffer = new byte[1024];
                string msg = string.Empty;
                int bytes = 0;
                int MessageCount = 0;

                // client message 대기
                while (true)
                {
                    MessageCount++;
                    stream = clientSocket.GetStream();
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    msg = Encoding.Unicode.GetString(buffer, 0, bytes);
                    msg = msg.Substring(0, msg.IndexOf("$"));

                    // OnReceived에 msg와 socket 전달, OnReceived에서 메시지 처리
                    if (OnReceived != null)
                        OnReceived(msg, clientSocket);
                }
            }
            // 오류 발생 시 OnDisconnected call, socket 닫고, stream 닫기
            catch (SocketException se)
            {
                Console.WriteLine(string.Format("doChat - SocketException : {0}", se.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }
        }

    }
}
