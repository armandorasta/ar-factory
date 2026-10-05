using System;

namespace ArFactory;

public enum BinaryOperation
{
	Add, Sub, Mul, Div,
	And, Or, Xor,
	BitAnd, BitOr, BitXor,
	Eq, NotEq, Greater, GreaterEq, Less, LessEq,
}

public static class BinaryOperationExtensions
{
	public static string ToString(this BinaryOperation op) => op switch
	{
		BinaryOperation.Add => "+",
		BinaryOperation.Sub => "-",
		BinaryOperation.Mul => "*",
		BinaryOperation.Div => "/",
		BinaryOperation.And => "&&",
		BinaryOperation.Or => "||",
		BinaryOperation.Xor => "^^",
		BinaryOperation.BitAnd => "&",
		BinaryOperation.BitOr => "|",
		BinaryOperation.BitXor => "^",
		BinaryOperation.Eq => "==",
		BinaryOperation.NotEq => "!=",
		BinaryOperation.Greater => ">",
		BinaryOperation.GreaterEq => ">=",
		BinaryOperation.Less => "<",
		BinaryOperation.LessEq => "<=",
		_ => throw new NotImplementedException(),
	};

	public static int Apply(this BinaryOperation op, int lhs, int rhs) => op switch
	{
		BinaryOperation.Add => lhs + rhs,
		BinaryOperation.Sub => lhs - rhs,
		BinaryOperation.Mul => lhs * rhs,
		BinaryOperation.Div => lhs / rhs,
		BinaryOperation.And => lhs != 0 && rhs != 0 ? 1 : 0,
		BinaryOperation.Or => lhs != 0 || rhs != 0 ? 1 : 0,
		BinaryOperation.Xor => lhs != 0 ^ rhs != 0 ? 1 : 0,
		BinaryOperation.BitAnd => lhs & rhs,
		BinaryOperation.BitOr => lhs | rhs,
		BinaryOperation.BitXor => lhs ^ rhs,
		BinaryOperation.Eq => lhs == rhs ? 1 : 0,
		BinaryOperation.NotEq => lhs != rhs ? 1 : 0,
		BinaryOperation.Greater => lhs > rhs ? 1 : 0,
		BinaryOperation.GreaterEq => lhs >= rhs ? 1 : 0,
		BinaryOperation.Less => lhs < rhs ? 1 : 0,
		BinaryOperation.LessEq => lhs <= rhs ? 1 : 0,
		_ => throw new NotImplementedException(),
	};
}


