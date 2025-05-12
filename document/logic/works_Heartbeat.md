# HeartBeat 동작 방식

- 패킷 내부 구조는 CMessageResolver.cs  분석 코드 참고 하시면 됩니다

- HeartBeat 검사

```csharp
// 서버 초기화

byte check_interval = 10;
this.usermanager.start_heartbeat_checking(check_interval, check_interval);

public void start_heartbeat_checking(uint check_interval_sec, uint allow_duration_sec)
{
		// 10초마다 check_heartbeat 호출
    this.heartbeat_duration = allow_duration_sec * 10000000;
    this.timer_heartbeat = new Timer(check_heartbeat, null, 1000 * check_interval_sec, 1000 * check_interval_sec);
}

/*
	접속된 전체 유저 대상으로 10초가 지나면 유저 종료 처리
	heartbeat_time = 유저 응답이 와야 현재 시간으로 갱신
*/
void check_heartbeat(object state)
{
	// allowed_time = 10초
	for (int i = 0; i < this.users.Count; ++i)
	{
    long heartbeat_time = this.users[i].latest_heartbeat_time;
    if (heartbeat_time >= allowed_time)
    {
        continue;
    }

    this.users[i].disconnect();
	}
}
```

1. S → C   SYS_START_HEARTBEAT 전달
2. C → S   5초마다 주기적으로 서버에게 SYS_UPDATE_HEARTBEAT 전달

```csharp
/*
	클라이언트 접속 후 on_new_client() call
	
*/

// HeartBeat 호출 과정
// 1. INSERT CUserToken.SYS_START_HEARTBEAT = -2
this.protocol_id = CUserToken.SYS_START_HEARTBEAT;
this.position = Defines.HEADERSIZE; // 4 byte

/*
	temp_buffer = 2byte
	buffer 4byte 부터  temp_buffer 복사
	buffer[4], buffer[5] <- SYS_START_HEARTBEAT COPY
	changed position = 6
*/
byte[] temp_buffer = BitConverter.GetBytes(SYS_START_HEARTBEAT);
temp_buffer.CopyTo(this.buffer, this.position);
this.position += temp_buffer.Length;

// 2. body 전달
public void push(byte data)
{
		/*
			data = 5 
			this.buffer[position] = 6 부터 copy
			position[6] = interval(5)
			changed position = 7 
		*/
    byte[] temp_buffer = BitConverter.GetBytes(data);
    temp_buffer.CopyTo(this.buffer, this.position);
    this.position += sizeof(byte);
}

// 3. header 셋팅 
user_token.send(msg)
{
		// position 값을 buffer[0] 저장한다
		byte[] header = BitConverter.GetBytes(this.position);
		// buffer[0] = 7
		header.CopyTo(this.buffer, 0);
}
```

- 서버에서  데이터 전송

```csharp
// 1. 패킷을 queue 저장
public void send(CPacket msg)
{
	this.sending_list.Add(data);
	if (this.sending_list.Count > 1)
	{
    // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴한다.
    // 현재 수행중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
    return;
	}
	start_send();
}

// 2. 비동기 전송
bool pending = this.socket.SendAsync(this.send_event_args);
if (!pending)
{
    process_send(this.send_event_args);
}

// 3 pending 값과 상관없이 아래 매소드 호출됨
public void process_send(SocketAsyncEventArgs e)
{
	/*
		서버 입장에서는 비동기로 얼마만큼 보냈는지 확인하고
		전송이 완료된 패킷은 sending_list 에서 제거한다
	*/
}
```

- 서버에서 데이터 수신
- C -> S   SYS_UPDATE_HEARTBEAT    예시

```csharp
// 1. 비동기 recv
bool pending = socket.ReceiveAsync(receive_args);
if (!pending)
{
    process_receive(receive_args);
}

// 2. pending 값과 상관없이 아래 매소드 호출됨
void on_message_completed(ArraySegment<byte> buffer)
{
	// IO스레드에서 직접 호출.
	// buffer receive 된 데이터가 존재
	CPacket msg = new CPacket(buffer, this);
	on_message(msg);
}

// 3. 현재 시간으로 셋팅 후 종료됨
public void on_message(CPacket msg)
{
	case SYS_UPDATE_HEARTBEAT:
    //Console.WriteLine("heartbeat : " + DateTime.Now);
    this.latest_heartbeat_time = DateTime.Now.Ticks;
    return;
	
}
```