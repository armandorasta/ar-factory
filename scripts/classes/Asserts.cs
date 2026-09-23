
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Godot;

namespace ArTest;
using static AnsiColors;

public static class Asserts
{
	public static SceneTree TreePtr;
	public static string CurrTestClass;
	public static string CurrTestMethod;

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertTrue(bool cond, string msg = null)
	{
		HandleImpl(cond, $"Expected {Green}'true'{Reset} but got {Red}'false'{Reset}", msg);
	}

	
	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertFalse(bool cond, string msg = null)
	{
		HandleImpl(!cond, $"Expected {Green}'false'{Reset} but got {Red}'true'{Reset}", msg);
	}

	/// <summary>
	/// Asserts: <paramref name="subject"/> is an instance of <paramref name="expectedType"/>.
	/// </summary>
	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertIs(object subject, Type expectedType, string msg = null)
	{
		HandleImpl(expectedType.IsInstanceOfType(subject), 
			$"Expected {Green}'{subject.GetType()}'{Reset} but got {Red}'{expectedType}'{Reset}", 
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
			$"Expected {Red}'{testValue}'{Reset} to equal {Green}'{target}'{Reset}", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNotEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(!EqualityComparer<T>.Default.Equals(testValue, target),
			$"Expected {Red}'{testValue}'{Reset} to not equal {Green}'{target}'{Reset}", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertRefEq<T>(T testValue, T target, string msg = null) where T : class
	{
		HandleImpl(object.ReferenceEquals(testValue, target),
			$"Expected {Red}'{testValue}'{Reset} to equal {Green}'{target}'{Reset}", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertRefNotEq<T>(T testValue, T target, string msg = null)
	{
		HandleImpl(!object.ReferenceEquals(testValue, target),
			$"Expected {Red}'{testValue}'{Reset} to not equal {Green}'{target}'{Reset}", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNull(object testValue, string msg = null)
	{
		HandleImpl(testValue is null, 
			$"Expected {Red}{testValue}{Reset} to be {Green}null{Reset}", 
			msg);
	}

	[StackTraceHidden] 
	[DebuggerStepThrough]
	public static void AssertNotNull(object testValue, string msg = null)
	{
		HandleImpl(testValue is not null, 
			$"Expected {Green}non-null{Reset} but got {Red}null{Reset}", 
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

		var bui = new StringBuilder($"{Cyan}{CurrTestMethod}{Reset} {Red}failed:{Reset} {specialMsg}\n");
		if (!string.IsNullOrEmpty(userMsg))
		{
			bui.Append($"{Yellow}Message:{Reset} {userMsg}\n");
		}

		var trace = new StackTrace(2, fNeedFileInfo: true);
		var frame = trace.GetFrame(0);
		var file = Path.GetRelativePath(Directory.GetCurrentDirectory(), frame.GetFileName());
		var line = frame.GetFileLineNumber();
		var col = frame.GetFileColumnNumber();

		bui.Append($"On ({Cyan}.\\{file}:{line}:{col}{Reset})\n");

		throw new TestAssertFailedException(bui.ToString());
	}
}
