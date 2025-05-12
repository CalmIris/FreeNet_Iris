# CLogicMessageEntry.cs

- 수신된 패킷을 받아 로직 스레드에서 분배하는 역할을 담당한다.

```csharp
CNetworkService service;
// chat message 들어오고  한쪽에서는 receive 하여 출력하는 구조
ILogicQueue message_queue;
// 쓰레드 생성후  event로 쉬고 있다가 데이터가 들어오면 깨워서 데이터 출력을 한
AutoResetEvent logic_event;
```