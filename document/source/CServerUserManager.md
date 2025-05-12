# CServerUserManager.cs

- 

```csharp
* 클래스 맴버 변수 

// lock 변수
object cs_user;
------------------------------------------
// CUserToken 객체는 클라이언트 접속시 관리되는 됩니다
List<CUserToken> users;
------------------------------------------
// 서버에서 timer 객체 관리 
Timer timer_heartbeat;
------------------------------------------
// 클라이언트가 응답을 기다리는 최대 시간
long heartbeat_duration;
```

```csharp
public void start_heartbeat_checking(uint check_interval_sec, uint allow_duration_sec)
{
	/*
		서버 실행시 Timer 셋팅에 맞는 시간이 지나면 callback 함수를 호출합니다
	  ex) 10초 주기로 	check_heartbeat delegate callback
	*/
	this.heartbeat_duration = allow_duration_sec * 10000000;
  this.timer_heartbeat = new Timer(check_heartbeat, null, 1000 * check_interval_sec, 1000 * check_interval_sec);
}
```

```csharp
public void stop_heartbeat_checking()
{
	// timer 에 셋팅된 callback 및 반복 주기를 종료합니다
	this.timer_heartbeat.Dispose();
}
```

```csharp
public void add(CUserToken user)
{
	// 클라이언트 접속 후 호출되는 함수
	this.users.Add(user);
}

```

```csharp
public void remove(CUserToken user)
{
	// 클라이언트 종료시 list 에서 삭제
	this.users.Remove(user);
}
```

```csharp
void check_heartbeat(object state)
{
	/*
		start_heartbeat_checking 매소드에서 주기적으로 (10초) check_heartbeat 호출
		접속되어 있는 클라이언트 유저 응답이 느린 유저는 연결을 끊는다
	*/
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