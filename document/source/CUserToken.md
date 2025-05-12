# CUserToken.cs

```csharp

* 클래스 맴버 변수 

// 클라이언트 상태 정보 저장
State current_state;
------------------------------------------
// 클라이언트 접속 후 socket 저장
public Socket socket { get; set; }
------------------------------------------
// 클라이언트 socket 이벤트 객체 
public SocketAsyncEventArgs receive_event_args { get; private set; }
public SocketAsyncEventArgs send_event_args { get; private set; }
------------------------------------------
// 클라이언트 종료시 호출되기 위한 callback 매소드
public delegate void ClosedDelegate(CUserToken token);
public ClosedDelegate on_session_closed;
```

```csharp
public CUserToken(IMessageDispatcher dispatcher)
{
	/*
		클라이언트 접속 후 맴버변수 초기화
	*/
	this.current_state = State.Idle;
}
```

```csharp
public void on_connected()
{
	// 클라이언트 접속시 상태 변화
	this.current_state = State.Connected;
	this.is_closed = 0
	this.current_state = State.Connected;
}
```