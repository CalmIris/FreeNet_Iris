# SocketAsyncEventArgsPool.cs

```csharp
/*
	SocketAsyncEventArgs  미리 만들어 필요할때 사용하기 위한 Pool
	코드는 간단해서 자세한 설명은 없습니다.
*/
Stack<SocketAsyncEventArgs> m_pool;

// Method
public void Push(SocketAsyncEventArgs item)
public SocketAsyncEventArgs Pop()

```