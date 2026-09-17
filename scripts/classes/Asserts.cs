
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;

namespace ArFactory.Tests;

public static class Asserts
{
	public static SceneTree TreePtr;
	public static string CurrTestClass;
	public static string CurrTestMethod;

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertTrue(bool cond, string msg = null)
	{
		HandleImpl(cond, "Expected 'true' but got 'false'", msg);
	}

	
	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertFalse(bool cond, string msg = null)
	{
		HandleImpl(!cond, "Expected 'false' but got 'true'", msg);
	}

	/// <summary>
	/// Asserts: <paramref name="subject"/> is an instance of <paramref name="expectedType"/>.
	/// </summary>
	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertIs(object subject, Type expectedType, string msg = null)
	{
		HandleImpl(expectedType.IsInstanceOfType(subject), 
			$"Expected '{subject.GetType()}' but got '{expectedType}'", 
			msg);
	}

	/// <summary>
	/// Asserts the <paramref name="testValue"/> is equal to <paramref name="target"/> using the
	/// equality operator. Use <seealso cref="AssertRefEq"/> for reference comparison.
	/// </summary>
	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(EqualityComparer<T>.Default.Equals(testValue, target),
			$"Expected '{testValue}' to equal '{target}'", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNotEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(!EqualityComparer<T>.Default.Equals(testValue, target),
			$"Expected '{testValue}' to not equal '{target}'", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertRefEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(object.ReferenceEquals(testValue, target),
			$"Expected reference '{testValue}' to equal '{target}'", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertRefNotEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(!object.ReferenceEquals(testValue, target),
			$"Expected reference '{testValue}' to not equal '{target}'", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNull(object testValue, string msg = null)
	{
		HandleImpl(testValue is null, 
			$"Expected {testValue} to be null", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNotNull(object testValue, string msg = null)
	{
		HandleImpl(testValue is not null, 
			$"Expected {testValue} to be non-null", 
			msg);
	}


	[StackTraceHidden] 
	[DebuggerHidden]
	private static void HandleImpl(bool bMustBe, string specialMsg, string userMsg)
	{
		if (bMustBe)
		{
			return;
		}

		var bui = new StringBuilder($"{CurrTestMethod} failed: {specialMsg}\n");
		if (!string.IsNullOrEmpty(userMsg))
		{
			bui.Append($"Message: {userMsg}\n");
		}

		var trace = new StackTrace(2, fNeedFileInfo: true);
		var frame = trace.GetFrame(0);
		var file = frame.GetFileName();
		var line = frame.GetFileLineNumber();
		var col = frame.GetFileColumnNumber();

		bui.Append($"On ({file}:{line}:{col})\n");

		throw new AssertFailedException(bui.ToString());
	}
}
