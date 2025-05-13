using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

namespace FreeNet
{
    class CListener
    {
        Socket listen_socket;
        // 비동기 Accept를 위한 EventArgs.
        private readonly ConcurrentQueue<SocketAsyncEventArgs> accept_pool;
        // 새로운 클라이언트가 접속했을 때 호출되는 콜백.
        public delegate void NewclientHandler(Socket client_socket, object token);
        public NewclientHandler callback_on_newclient;

        public CListener()
        {
            this.callback_on_newclient = null;
            this.accept_pool = new ConcurrentQueue<SocketAsyncEventArgs>();
            // TODO: Pool 갯수 따로 관리하는  객체를 만들어야 됩니다
            for (int i = 0; i < 10; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);
                this.accept_pool.Enqueue(args);
            }
        }

        public void start(string host, int port, int backlog)
        {
            this.listen_socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            IPAddress address;
            if (host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endpoint = new IPEndPoint(address, port);

            try
            {
                listen_socket.Bind(endpoint);
                listen_socket.Listen(backlog);
                while (accept_pool.TryDequeue(out var args))
                {
                    // 재사용시 AcceptSocket 초기화
                    args.AcceptSocket = null;
                    bool pending = listen_socket.AcceptAsync(args);
                    if (!pending)
                    {
                        on_accept_completed(null, args);
                    }
                }
            }
            catch (Exception /*e*/)
            {
                //Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// AcceptAsync의 콜백 매소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">AcceptAsync 매소드 호출시 사용된 EventArgs</param>
		void on_accept_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 새로 생긴 소켓을 보관해 놓은뒤~
                Socket client_socket = e.AcceptSocket;
                client_socket.NoDelay = true;

                // 이 클래스에서는 accept까지의 역할만 수행하고 클라이언트의 접속 이후의 처리는
                // 외부로 넘기기 위해서 콜백 매소드를 호출해 주도록 합니다.
                // 이유는 소켓 처리부와 컨텐츠 구현부를 분리하기 위함입니다.
                // 컨텐츠 구현부분은 자주 바뀔 가능성이 있지만, 소켓 Accept부분은 상대적으로 변경이 적은 부분이기 때문에
                // 양쪽을 분리시켜주는것이 좋습니다.
                // 또한 클래스 설계 방침에 따라 Listen에 관련된 코드만 존재하도록 하기 위한 이유도 있습니다.
                if (this.callback_on_newclient != null)
                {
                    this.callback_on_newclient(client_socket, e.UserToken);
                }

                this.accept_pool.Enqueue(e);
                if (accept_pool.TryDequeue(out var args))
                {
                    args.AcceptSocket = null;
                    bool pending = listen_socket.AcceptAsync(args);
                    if (!pending)
                    {
                        on_accept_completed(null, args);
                    }
                }                
            }
            else
            {
                //todo:Accept 실패 처리.
                Console.WriteLine("Failed to accept client. " + e.SocketError);
            }
        }
    }
}
