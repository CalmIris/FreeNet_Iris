# 1. backlog ,  acceptAsync 관계

| 항목 | backlog | AcceptAsync pending 수 |
| --- | --- | --- |
| 큐 관리 위치 | 커널(TCP) | 애플리케이션(.NET) |
| 최대 연결 | `backlog` 값까지 | 앱이 동시에 꺼내 처리할 최대 동시 Accept 수 |
| 과소 설정 시 | 연결 요청이 드롭됨 | 처리 지연으로 backlog 큐에 오래 머뭄 |
| 과다 설정 시 | OS 메모리/리소스 낭비 가능 | `SocketAsyncEventArgs` 풀 메모리 과다, GC 부담 |

acceptAsync 비동기 이벤트를 사용시   SocketAsyncEventArgs   한개 사용하는 방식을 변경하였습니다

기존 :  Thread 에서  Accept 소켓 이벤트 발생시   SocketAsyncEventArgs   하나로 대응

변경 :  SocketAsyncEventArgs  Pool  만들어   Accept 소켓 이벤트 발생시  event  다중화 처리
TODO:  Pool  갯수  따로 관리하는  객체를 만들어야 됩니다

- 극단적인 상황 예시로 테스트
- backlog  = 1   , acceptQueue = 1
client = 5000
repeat  ping/pong = 1
⇒  5000  client 동시 접속시  일부 접속 못하는 상황

- backlog  = 1   , acceptQueue = 10
client = 5000
repeat  ping/pong = 1
⇒ 5000 client 동시 접속시  정상동작