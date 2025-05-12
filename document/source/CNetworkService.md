# CNetworkService.cs

```csharp
* 클래스 맴버 변수 

// 소켓 recv 버퍼를 관리하는 이벤트 객체 POOL
SocketAsyncEventArgsPool receive_event_args_pool;
------------------------------------------

// 소켓 send 버퍼를 관리하는 이벤트 객체 POOL
SocketAsyncEventArgsPool send_event_args_pool;
------------------------------------------

// 클라이언트 접속시 호출되는 Callback 함수
public delegate void SessionHandler(CUserToken token);
public SessionHandler session_created_callback { get; set; }
------------------------------------------

// 
public CLogicMessageEntry logic_entry { get; private set; }

------------------------------------------
// 클라이언트 접속시 관리되는 클래스 
public CServerUserManager usermanager { get; private set; }
```

```csharp

void Initialize()
{
	CUserToken token = new CUserToken();
	// 버퍼 매니저 보유
	this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
	// Stack pool 초기화
	this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
	this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);

	// SocketAsyncEventArgs 초기화 (recv, send 별도 생성)
	arg = new SocketAsyncEventArgs();
	// 1. recv 이벤트 발생
	arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
	// 2. token 공유
	arg.UserToken  = token;
	// 2. buffer 할당
	this.buffer_manager.SetBuffer(arg);
	// 3. socketAsyncEventArgs 객체를  send, recv pool 에 저장
	this.receive_event_args_pool.Push(arg);

	// send arg 1,2,3 과정을 동일하게 수행
}

```

```csharp
void listen(string host, int port, int backlog)
{
	this.client_listener = new CListener();
	// 클라이언트 접속시 callback 함수 호출
	this.client_listener.callback_on_newclient += on_new_client;
	// CListener 참고
	this.client_listener.start(host, port, backlog);
}

```

```csharp
/// 새로운 클라이언트가 접속 성공 했을 때 호출됩니다.
/// AcceptAsync의 콜백 매소드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에 공유자원에 접근할 때는 주의해야 합니다.
void on_new_client(Socket client_socket, object token)
{
	// 풀에서 하나 꺼내와 사용한다
	SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
	SocketAsyncEventArgs send_args =  this.send_event_args_pool.Pop();

	begin_receive(client_socket, receive_args, send_args);
}
```

```csharp
void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
{
	// receive_args, send_args 아무곳에서나 꺼내와도 된다. 둘다 동일한 CUserToken을 물고 있다.
	CUserToken token = receive_args.UserToken as CUserToken;
	token.set_event_args(receive_args, send_args);

	// 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
	token.socket = socket;
	// recv Event 발생시  receive_completed CALL
	socket.ReceiveAsync(receive_args);
}
```

```csharp
// 따로 설명할게 없음
void receive_completed(object sender, SocketAsyncEventArgs e)
{
    if (e.LastOperation == SocketAsyncOperation.Receive)
    {
        process_receive(e);
        return;
    }

    throw new ArgumentException("The last operation completed on the socket was not a receive.");
}
```

```csharp
private void process_receive(SocketAsyncEventArgs e)
{
	// 드디어 패킷 receive
	token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);
	// 비동기 recv 재등록
	token.socket.ReceiveAsync(e);
}
```