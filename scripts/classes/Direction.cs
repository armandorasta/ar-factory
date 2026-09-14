using System;
using Godot;

namespace ArFactory;

public enum Direction
{
	North = 0,
	East  = 1,
	South = 2,
	West  = 3,
}

public static class DirectionExtensions
{
	public static Direction Invert(this Direction dir)
	{
		Debug.Assert(Enum.IsDefined(dir));
		return dir switch
		{
			Direction.North => Direction.South,
			Direction.South => Direction.North,
			Direction.East => Direction.West,
			Direction.West => Direction.East,
			_ => (Direction)(-1),
		};
	}

	public static Direction Rotate90(this Direction dir)
	{
		Debug.Assert(Enum.IsDefined(dir));
		return (Direction)(((int)dir + 1) % 4);
	}

	public static Direction Rotate270(this Direction dir)
	{
		Debug.Assert(Enum.IsDefined(dir));
		// We add 4 first to prevent it from going below zero.
		return (Direction)(((int)dir + 4 - 1) % 4);
	}

	public static Vector2I ToGrid(this Direction dir)
	{
		Debug.Assert(Enum.IsDefined(dir));
		return dir switch
		{
			Direction.North => new(0, -1),
			Direction.South => new(0, +1),
			Direction.East => new(+1, 0),
			Direction.West => new(-1, 0),
			_ => new(int.MinValue, int.MinValue),
		};
	}
}