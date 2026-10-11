namespace ArTest;

public abstract class TestSuit
{
	public virtual void BeforeAll()  { }
	public virtual void BeforeEach() { }
	public virtual void AfterAll()   { }
	public virtual void AfterEach()  { }

	public T AddNode<T>(T node) where T : Godot.Node
	{
		TestHandler.GetInstance().AddChild(node);
		return node;
	}

	public T RemoveNode<T>(T node) where T : Godot.Node
	{
		TestHandler.GetInstance().RemoveChild(node);
		return node;
	}
}
