# CPacket.cs

- 패킷 데이터를 만드는 클래스
- 예시로  패킷 만드는 과정을 따라  코드 분석 입니다.

```csharp
/* 
	// 하트비트 갱신. C -> S
	const short SYS_UPDATE_HEARTBEAT = -3;
*/
CPacket msg = CPacket.create((short)PROTOCOL.CHAT_MSG_REQ);
msg.push("hello");
```

```csharp

public static CPacket create(Int16 protocol_id)
{
    CPacket packet = new CPacket();
    packet.set_protocol(protocol_id);
    return packet;
}
```

```csharp
// PROTOCOL.CHAT_MSG_REQ 
// position 4 changed
public void set_protocol(Int16 protocol_id)
{
    this.protocol_id = protocol_id;
    // 헤더는 나중에 넣을것이므로 데이터 부터 넣을 수 있도록 위치를 점프시켜놓는다.
    this.position = Defines.HEADERSIZE;
    push_int16(protocol_id);
}
```

```csharp

public void push_int16(Int16 data)
{
    byte[] temp_buffer = BitConverter.GetBytes(data);
    temp_buffer.CopyTo(this.buffer, this.position);
    this.position += temp_buffer.Length;
}
```