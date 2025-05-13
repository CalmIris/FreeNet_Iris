using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using FreeNet;

namespace CSampleClient
{
    using GameServer;

    class Program
    {
        static List<IPeer> game_servers = new List<IPeer>();

        static async Task Main(string[] args)
        {
            // CNetworkService객체는 메시지의 비동기 송,수신 처리를 수행한다.
            // 메시지 송,수신은 서버, 클라이언트 모두 동일한 로직으로 처리될 수 있으므로
            // CNetworkService객체를 생성하여 Connector객체에 넘겨준다.
            CNetworkService service = new CNetworkService(false);

            
            int repeat = 1;
            int clientCount = 10;
            var tasks = new Task[clientCount];
            for (int i = 0; i< clientCount; i++)
            {
                tasks[i] = RunClientAsync(service, i + 1, repeat);
            }
            
            await Task.WhenAll(tasks);
            ((CRemoteServerPeer)game_servers[0]).token.disconnect();
            
            Console.ReadKey();
        }

        static async Task RunClientAsync(CNetworkService service, int clientId, int repeat)
        {
            // endpoint정보를 갖고있는 Connector생성. 만들어둔 NetworkService객체를 넣어준다.
            CConnector connector = new CConnector(service);
            // 접속 성공시 호출될 콜백 매소드 지정.
            connector.connected_callback += on_connected_gameserver;
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
            connector.connect(endpoint);
            await Task.Delay(100);

            for (int i = 0; i < repeat; i++)
            {
                //Console.WriteLine($"Client: {clientId} - hello world{i + 1}");
                CPacket msg = CPacket.create((short)PROTOCOL.CHAT_MSG_REQ);
                msg.push($"Client: {clientId} - hello world{i+1}");
                game_servers[0].send(msg);
            }
        }

        /// <summary>
        /// 접속 성공시 호출될 콜백 매소드.
        /// </summary>
        /// <param name="server_token"></param>
        static void on_connected_gameserver(CUserToken server_token)
        {
            lock (game_servers)
            {
                IPeer server = new CRemoteServerPeer(server_token);
                server_token.on_connected();
                game_servers.Add(server);
                Console.WriteLine("Connected!");
            }
        }
    }
}
