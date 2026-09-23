using ArFactory.Tests;
using Godot;
using System;

namespace ArFactory.Tests;

public partial class MyTestHandler : ArTest.TestHandler
{
	public MyTestHandler()
	{
		IsShowPassingTests = true;
		IsPrintOrphanNodes = true;
	}
}
