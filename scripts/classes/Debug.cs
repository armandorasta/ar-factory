using Godot;
using System;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace ArFactory;

// When Assertions are enabled, the debugger will allow you to step over them.
// When disabled, the debugger will completely ignore them as it should.
#if ASSERTIONS
using DebugAssertionDebuggerVisibiltyAttribute = DebuggerStepThroughAttribute;
#else
using DebugAssertionDebuggerVisibiltyAttribute = DebuggerHiddenAttribute;
#endif

#nullable enable

internal static class Debug
{
	public static SceneTree? TreePtr;
	
	private static bool s_bEnabled = true;


	public static void DisableAsserts() { s_bEnabled = false; }
	public static void EnableAsserts()  { s_bEnabled = true;  }


	[Conditional("ASSERTIONS")]
	[DebugAssertionDebuggerVisibilty]
	[StackTraceHidden] 
	public static void Assert([DoesNotReturnIf(false)] bool cond, string? msg = null)
	{
		HandleImpl(cond, $"Assertion Failed!", msg);
	}

	/// <summary>
	/// Asserts: <paramref name="subject"/> is an instance of <paramref name="expectedType"/>.
	/// </summary>
	[Conditional("ASSERTIONS")]
	[DebugAssertionDebuggerVisibilty] 
	[StackTraceHidden] 
	public static void AssertIs(object subject, Type expectedType, string? msg = null)
	{
		HandleImpl(expectedType.IsInstanceOfType(subject), 
			$"AssertIs: expected '{subject.GetType()}' but got '{expectedType}'.", 
			msg);
	}

	/// <summary>
	/// Asserts the <paramref name="testValue"/> is equal to <paramref name="target"/> using the
	/// equality operator. Use <seealso cref="AssertRefEq"/> for reference comparison.
	/// </summary>
	[Conditional("ASSERTIONS")]
	[DebugAssertionDebuggerVisibilty] 
	[StackTraceHidden] 
	public static void AssertEq<T>(T testValue, T target, string? msg = null)
	{
		HandleImpl(EqualityComparer<T>.Default.Equals(testValue, target),
			$"AssertEq: expected '{target}' but got '{testValue}'.", 
			msg);
	}

	[Conditional("ASSERTIONS")]
	[DebugAssertionDebuggerVisibilty] 
	[StackTraceHidden] 
	public static void AssertRefEq<T>(T testValue, T target, string? msg = null)
	{
		HandleImpl(object.ReferenceEquals(testValue, target),
			$"AssertEq: expected '{target}' but got '{testValue}'.", 
			msg);
	}

	[Conditional("ASSERTIONS")]
	[DebugAssertionDebuggerVisibilty] 
	[StackTraceHidden] 
	public static void AssertNotNull(object testValue, string? msg = null)
	{
		HandleImpl(testValue is not null, 
			$"AssertNotNull: Expected non-null.", 
			msg);
	}

	/// <summary>
	/// Used in branches meant to be impossible.
	/// </summary>
	[StackTraceHidden] 
	[DebugAssertionDebuggerVisibilty] 
	[DoesNotReturn]
	public static void AssertUnreachable()
	{
		PrintStackTrace();
		throw new NotImplementedException("AssertUnreachable: Code shouldn't be able to reach here!");
	}


	[Conditional("ASSERTIONS")]
	[DebuggerHidden] 
	[StackTraceHidden] 
	private static void HandleImpl([DoesNotReturnIf(false)] bool bMustBe, string specialMsg, string? userMsg)
	{
		if (!s_bEnabled)
		{
			return;
		}

		if (bMustBe)
		{
			return;
		}

		GD.PrintErr($"\n{specialMsg}");
		if (!string.IsNullOrEmpty(userMsg))
		{
			GD.PrintErr($"Message: {userMsg}");
		}
		
		PrintStackTrace(3);
		// GD.PushError($"Assertion Failed! {msg}");
		TreePtr?.Quit();
	}

	[StackTraceHidden]
	[Conditional("ASSERTIONS")]
	private static void PrintStackTrace(int nSkippedFrames = 1, bool includeGodotShit = false)
	{
		GD.PrintErr("Stack:");
		var trace = new StackTrace(nSkippedFrames, fNeedFileInfo: true);
		for (var i = 0; i < trace.FrameCount; ++i)
		{
			var frame = trace.GetFrame(i)!;
			if (frame.GetFileName() is null)
			{
				// We only get here when we are testing so far...
				break;
			}
			
			// Stop as soon as we get to godot stuff.
			if (!includeGodotShit && frame.GetFileName()!.Contains("root/godot/"))
			{
				break;
			}

			var clazz = Path.GetFileNameWithoutExtension(frame.GetFileName());
			var file = frame.GetFileName();
			var meth = frame.GetMethod();
			var line = frame.GetFileLineNumber();
			var col = frame.GetFileColumnNumber();

			GD.PrintErr($"[{i + 1}] (class {clazz}) {meth} ({file}:{line}:{col})");
		}
	}
}
