using System;
using System.Text.Json;
using Godot;

namespace ArFactory.Serialization;

internal static class Utf8JsonWriterExtensions
{
	public static void WriteVector2IValue(this Utf8JsonWriter writer, Vector2I vec)
	{
		writer.WriteStartArray();
		writer.WriteNumberValue(vec.X);
		writer.WriteNumberValue(vec.Y);
		writer.WriteEndArray();
	}

	public static void WriteVector2I(this Utf8JsonWriter writer, string name, Vector2I vec)
	{
		writer.WritePropertyName(name);
		writer.WriteVector2IValue(vec);
	}

	public static void WriteUnitValue(this Utf8JsonWriter writer, Unit u)
	{
		writer.WriteStartObject();
		writer.WriteVector2I("GL", u.GLocation);
		writer.WriteNumber("Direction", (Int32)u.Dir);
		writer.WriteNumber("Rate", u.Rate);
		writer.WriteBoolean("IsLocked", u.IsLocked());
		writer.WriteEndObject();
	}

	public static void WriteUnit(this Utf8JsonWriter writer, string name, Unit u)
	{
		writer.WritePropertyName(name);
		writer.WriteUnitValue(u);
	}
}
