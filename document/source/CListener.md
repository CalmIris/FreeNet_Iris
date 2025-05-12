# CListener.cs

```csharp
// 서버 listen 시작 지점 
public void start(string host, int port, int backlog)
{
	// 서버 소켓 초기화
	listen_socket.Bind(endpoint);
	listen_socket.Listen(backlog);
	
	/*
		소켓 accept 비동기 처리 
		클라이언트 접속시 on_accept_completed CALL
	*/
	this.accept_args = new SocketAsyncEventArgs();
	this.accept_args.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);
	
	// accept 재등록
	Thread listen_thread = new Thread(do_listen);
	listen_thread.Start();
}
```