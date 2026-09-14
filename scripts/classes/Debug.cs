using Godot;
using System;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ArFactory;

#nullable enable

internal static class Debug
{
	public static SceneTree? TreePtr;

	[StackTraceHidden] [DebuggerStepThrough]
	public static void Assert([DoesNotReturnIf(false)] bool cond, string? msg = null)
	{
		HandleImpl(cond, $"Assertion Failed!", msg);
	}

	[StackTraceHidden] [DebuggerStepThrough]
	public static void AssertIs(object lhs, Type type, string? msg = null)
	{
		var lhsTypeName = lhs.GetType().ToString();
		var targetTypeName = type.ToString();
		HandleImpl(lhsTypeName == targetTypeName, 
			$"AssertIs: expected '{lhsTypeName}' but got '{targetTypeName}'.", 
			msg);
	}

	[StackTraceHidden] [DebuggerStepThrough]
	public static void AssertEq<T>(IEquatable<T> testValue, IEquatable<T> target, string? msg = null)
	{
		HandleImpl(testValue == target,
			$"AssertEq: expected '{target}' but got '{testValue}'.", 
			msg);
	}

	[StackTraceHidden] [DebuggerStepThrough]
	public static void AssertNotNull(object testValue, string? msg = null)
	{
		HandleImpl(testValue is not null, 
			$"AssertNotNull: Expected non-null.", 
			msg);
	}

	[StackTraceHidden] [DebuggerStepThrough] [DoesNotReturn]
	public static void AssertUnreachable()
	{
		PrintStackTrace();
		throw new NotImplementedException("AssertUnreachable: Code shouldn't be able to reach here!");
	}

	[StackTraceHidden] [DebuggerHidden]
	private static void HandleImpl(bool bMustBe, string specialMsg, string? userMsg)
	{
#if DEBUG
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
#endif
	}

	[StackTraceHidden]
	[Conditional("DEBUG")]
	private static void PrintStackTrace(int nSkippedFrames = 1, bool includeGodotShit = false)
	{
		GD.PrintErr("Stack:");
		var trace = new System.Diagnostics.StackTrace(nSkippedFrames, fNeedFileInfo: true);
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

			var className = Path.GetFileNameWithoutExtension(frame.GetFileName());
			var file = frame.GetFileName();
			var meth = frame.GetMethod();
			var line = frame.GetFileLineNumber();
			var col = frame.GetFileColumnNumber();

			GD.PrintErr($"[{i + 1}] ({className}) {meth} ({file}:{line}:{col})");
		}
	}
}
