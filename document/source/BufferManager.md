# BufferManager.cs

- 소켓 버퍼에 데이터가 들어오면  유저 버퍼에 할당 관리를 위한  클래스

```csharp
* 클래스 맴버 변수 

/*
	이 클래스를 서버에서  m_buffer 를 활용하여  
	SocketAsyncEventArgs 객체에 미리 할당합니다  
	TCP 버퍼에 데이터가 존재한다면 (SEND, RECV)   
	m_buffer 에 미리 할당된 공간에 데이터가 들어옵니다
*/
byte[] m_buffer; 
------------------------------------------
/*
	서버에서 최대 접속 클라이언트 수 * 소켓의 최대 버퍼
	계산하여 값에 할당된다
*/
int m_numBytes;
------------------------------------------
/*
 수신/송신에 사용할 버퍼 크기 
 소켓마다 별도로 할당 예정
*/
int m_bufferSize;
------------------------------------------
/*
   m_buffer 배열 하나에 여러개 소켓의 SetBuffer 할당시 
   시작 offset 을 저장하기 위한 변수 
   각 socket 객체 m_buffer offset 이 달라야 서로 겹치지 않고 저장이 된다
*/
int m_currentIndex;
------------------------------------------
/*
	소켓이 종료될때  m_buffer offset 위치를 기억하기 위한 stack 
	재사용이 될때 사용된다
*/
Stack<int> m_freeIndexPool;
```

```csharp
public BufferManager(int totalBytes, int bufferSize)
{
		m_numBytes = totalBytes;   
    m_currentIndex = 0;
    m_bufferSize = bufferSize;
    m_freeIndexPool = new Stack<int>();
}

public void InitBuffer()
{
	/*
		BufferManager 생성자 호출 후  호출되는 매소드
		m_buffer는 SocketAsyncEventArg 객체에 참조된다 (not copy)
	*/
	m_buffer = new byte[m_numBytes];
}
```

```csharp
public bool SetBuffer(SocketAsyncEventArgs args)
{
	/*
		args 객체에 버퍼를 할당 
		할당을 안해주면 socket 에 데이터가 들어오더라도 에러 발생됨
		m_buffer 배열에 맞게 시작 위치를 나누어 할당한다
	*/
	args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
	m_currentIndex += m_bufferSize;
}
```

```csharp
public void FreeBuffer(SocketAsyncEventArgs args)
{
	// OS에게 사용자 메모리를 제공하지 않음 = 소켓 I/O 작업에서 버퍼 없는 작업으로 사용됨
	args.SetBuffer(null, 0, 0);
}
```

## 🧠 참고: TCP 수신 버퍼 vs 사용자 버퍼

| 버퍼 종류 | 위치 | 용도 | 비워지는 시점 |
| --- | --- | --- | --- |
| TCP 수신 버퍼 | 커널(OS 내부) | 네트워크로부터 도착한 원시 데이터 저장 | `recv` 또는 `ReceiveAsync` 호출 시 복사되면서 자동 소비 |
| 사용자 버퍼 | C# 코드에서 제공 (SetBuffer로 지정) | 애플리케이션이 처리할 데이터 저장 | 사용자가 직접 처리/초기화해야 함 |