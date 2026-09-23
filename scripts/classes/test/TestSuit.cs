using System;
using System.Collections.Generic;
namespace ArTest;

public abstract class TestSuit
{
	public virtual void BeforeAll()  { }
	public virtual void BeforeEach() { }
	public virtual void AfterAll()   { }
	public virtual void AfterEach()  { }

	public void AddNode(Godot.Node node) => TestHandler.GetInstance().AddChild(node);
	public void RemoveNode(Godot.Node node) => TestHandler.GetInstance().RemoveChild(node);
}
