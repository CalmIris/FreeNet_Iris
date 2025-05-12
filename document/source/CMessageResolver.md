# CMessageResolver.cs

- socket receive  에 관련된  클래스  
TCP 를 이용하며 스트림 데이터를 가져와  패킷 구조에 맞게 재조립 합니다.

예시)   클라이언트에서  “Hello World” Send 상황 

- 패킷 구조
- [0] ~ [3]  :  패킷 사이즈
- [4] ~ [5]  :  패킷 번호   (CHAT_MSG_REQ ⇒ 1)
- [6] ~ [7]  :  body 사이즈  (”Hello World” ⇒ 11 )
- [8] ~ [18] :  “Hello World” 저장

```csharp
* 클래스 맴버 변수

// 메시지 사이즈.
int message_size;
// 진행중인 버퍼.
byte[] message_buffer = new byte[1024];
// 현재 진행중인 버퍼의 인덱스를 가리키는 변수
// 패킷 하나를 완성한 뒤에는 0으로 초기화 시켜줘야 한다.
int current_position;
// 읽어와야 할 목표 위치.
int position_to_read;
// 남은 사이즈.
int remain_bytes;
----------------------------------------------

/*
	buffer 는 큰 배열로 이루어 져 있으며 클라이언트 마다 시작 index 를 설정해놓은 상태입니다
	현재 클라이언트에 속한 BufferManager.m_buffer 시작 인덱스는 10238976
	transffered = 19 값이 도착  ==>  m_buffer 에 시작 인덱스 기준 19바이트에 데이터를 읽었다는 의미
	m_buffer 에 있는 데이터를  message_buffer 복사 하는 과정입니다.
*/ 

/*
	argument buffer == BufferManager.m_buffer 
	
	헤더 사이즈를 읽기 위한 함수가 완료되면 
	buffer[sIndex] + 4byte (HEADER_SIZE) COPY => message_buffer[0] ~ [3] 
	message_buffer[0] => 19  
	클라이언트에서 보낸 "Hello World" 패킷이 totalByte 는 19바이트 확인함
*/
completed = read_until(buffer, ref src_position);

/*
	헤더 하나를 온전히 읽어왔으므로 메시지 사이즈를 구한다.
	this.message_size = 19 
*/
this.message_size = get_total_message_size();

// position_to_read = 19
this.position_to_read = this.message_size;

/*
	메시지를 읽는다.
	buffer[4] ~ [18]  COPY => message_buffer[4] ~ [18]
*/
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

void on_message_completed(ArraySegment<byte> buffer)
{
	// IO스레드에서 직접 호출.
	CPacket msg = new CPacket(buffer, this);
	on_message(msg);
	..
	// 코드 따라가보면 void IPeer.on_message(CPacket msg)  호출됨
}

void IPeer.on_message(CPacket msg)
{
	// protocol 확인 후
	PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();

	// 클라이언트에 패킷 전달	
	case PROTOCOL.CHAT_MSG_REQ:
	{
		string text = msg.pop_string();
		Console.WriteLine(string.Format("text {0}", text));
		CPacket response = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
		response.push(text);
		send(response);
	}
}

```

```csharp
bool read_until(byte[] buffer, ref int src_position)
{
	// 읽어와야 할 바이트.
	// 데이터가 분리되어 올 경우 이전에 읽어놓은 값을 빼줘서 부족한 만큼 읽어올 수 있도록 계산해 준다.
	int copy_size = this.position_to_read - this.current_position;
	
	// 앗! 남은 데이터가 더 적다면 가능한 만큼만 복사한다.
	if (this.remain_bytes < copy_size)
	{
    copy_size = this.remain_bytes;
	}
	
	// 버퍼에 복사
	Array.Copy(buffer, src_position, this.message_buffer, this.current_position, copy_size);
	// buffer[src_position] -> this.message_buffer[current_position] + copy_size
	
	// 원본 버퍼 포지션 이동
	src_position += copy_size;  
	// 타켓 버퍼 포지션도 이동
	this.current_position += copy_size; 
	// 남은 바이트 수
	this.remain_bytes -= copy_size; 
	// 목표지점에 도달 못했으면 false
	if (this.current_position < this.position_to_read)
	{
    return false;
	}
	return true;
}

```