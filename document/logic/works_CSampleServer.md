# CSampleServer 분석

- 패킷 내부 구조는 CMessageResolver.cs  분석 코드 참고 하시면 됩니다

- 서버 흐름도를 간단하게 정리해 보았습니다
1. 채팅서버 시작  및  초기화 
2. 클라이언트 접속  과정
3. 패킷 도착 및  응답 처리 과정

```csharp
service.initialize(10000, 1024);
{

	// tcp 버퍼 데이터를 받아와 관리하는 buffer_manager  인자값으로 버퍼의 크기 할당)
	BufferManager buffer_manager = new BufferManager(max_connections * buffer_size * pre_alloc_count, buffer_size);
	// socket event pool 객체 관리
	this.receive_event_args_pool = new SocketAsyncEventArgsPool(max_connections);
	this.send_event_args_pool = new SocketAsyncEventArgsPool(max_connections);
	// 버퍼 공간 할당
	buffer_manager.InitBuffer();
	
	/*
		socketEvent 발생시 receive_completed 호출
		버퍼 메니저에 공간 할당 
		receive pool 에 저장
	*/
	SocketAsyncEventArgs arg;
	for (int i = 0; i < max_connections; i++)
	{
		// recive pool
		arg = new SocketAsyncEventArgs();
		arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
		arg.UserToken = null;
		buffer_manager.SetBuffer(arg);
		this.receive_event_args_pool.Push(arg);
		
		// send pool
		// receive버퍼만 할당해 놓는다.
		// send버퍼는 보낼때마다 할당하든 풀에서 얻어오든 하기 때문에.
		arg = new SocketAsyncEventArgs();
		arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
		arg.UserToken = null;
		arg.SetBuffer(null, 0, 0);
		this.send_event_args_pool.Push(arg);
	}
}

```

```csharp
service.listen("0.0.0.0", 7979, 100)
{
	CListener client_listener = new CListener();
	client_listener.callback_on_newclient += on_new_client;
	// accept 비동기 상태에서 클라이언트 접속시  on_accept_completed CALL
	client_listener.start(host, port, backlog);	
}
```

클라이언트 접속 

```csharp
void on_accept_completed(object sender, SocketAsyncEventArgs e)
{
	// listen 접속된 소켓이므로  이걸로 send, recv 할 수 있습니다.
	Socket client_socket = e.AcceptSocket;
	// on_new_client CALL
  this.callback_on_newclient(client_socket, e.UserToken);
}

```

```csharp
/*
	CListen -> CNetworkService
	CUserToken 한개로 receive_args, send_args, userManager 공용으로 사용
*/
void on_new_client(Socket client_socket, object token)
{
	// receive event 발생 => receive_completed CALL
	SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
	// send event 발생 => send_completed CALL 
	SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

	// UserToken은 매번 새로 생성하여 깨끗한 인스턴스로 넣어준다.
	// socket event 에  user_token 할당
	CUserToken user_token = new CUserToken(this.logic_entry);
	receive_args.UserToken = user_token;
	send_args.UserToken = user_token;
	
	this.usermanager.add(user_token);
	// token SET status IDLE 
	user_token.on_connected();
	
	begin_receive(client_socket, receive_args, send_args);
}

```

```csharp
void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
{
	// token 에  send, recv 이벤트 등록
	CUserToken token = receive_args.UserToken as CUserToken;
	token.set_event_args(receive_args, send_args);
	// 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
	token.socket = socket;

	// receive 비동기 처리 receive_completed Callback
	socket.ReceiveAsync(receive_args);
}

```

```csharp
void receive_completed(object sender, SocketAsyncEventArgs e)
{
	process_receive(e);
}
```

```csharp
private void process_receive(SocketAsyncEventArgs e)
{

	/*
	  e.Buffer == BufferManager에 등록된 m_buffer 일정 부분 할당된 여역입니다
		
		example)
		byte[] buffer = new byte[1024];
		e.SetBuffer(buffer, 100, 200);
		Console.WriteLine(e.Buffer == buffer); // true
		Console.WriteLine(e.Offset); // 100
		Console.WriteLine(e.Count);  // 200
	*/
	CUserToken token = e.UserToken as CUserToken;
	token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);
}
```

```csharp
public void on_receive(byte[] buffer, int offset, int transfered)
{
	this.message_resolver.on_receive(buffer, offset, transfered, on_message_completed);
}
```

- receive  데이터 읽기는 별도 테스트 예제 만들 예정

```csharp
public void on_receive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
{
	// 헤더를 먼저 읽는다
	if (this.current_position < Defines.HEADERSIZE)
	{
		completed = read_until(buffer, ref src_position);
	}
	...
	...
	// 메시지를 읽는다.
	completed = read_until(buffer, ref src_position);
	if (completed)
	{
    // 패킷 하나를 완성 했다.
    byte[] clone = new byte[this.position_to_read];
    Array.Copy(this.message_buffer, clone, this.position_to_read);
    clear_buffer();
    // on_message_completed Callback
    callback(new ArraySegment<byte>(clone, 0, this.position_to_read));
	}
}

```

```csharp
void on_message_completed(ArraySegment<byte> buffer)
{
	// buffer 에 클라에서 보내준 데이터를 복사하여 가지고 있음 (ECHO)
	CPacket msg = new CPacket(buffer, this);
	on_message(msg);
}

```

```csharp
public void on_message(CPacket msg)
{
	// CGameUser.on_message Callback
	this.peer.on_message(msg);
}
```

```csharp
void IPeer.on_message(CPacket msg)
{
	// 클라이언트에 응답 처리
	case PROTOCOL.CHAT_MSG_REQ:
		string text = msg.pop_string();
		Console.WriteLine(string.Format("text {0}", text));

		CPacket response = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
		response.push(text);
		send(response);
}
```