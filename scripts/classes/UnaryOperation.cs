using System;

namespace ArFactory;

public enum UnaryOperation
{
	Add,
	Mul,
	Override,
}

public static class UnaryOperationExtensions
{
	public static int Apply(this UnaryOperation op, int itVal, int updateVal) => op switch
	{
		UnaryOperation.Add => itVal + updateVal,
		UnaryOperation.Mul => itVal * updateVal,
		UnaryOperation.Override => updateVal,
		_ => throw new NotImplementedException(),
	};
}

