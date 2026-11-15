using System;
using System.Text.Json;
using Godot;

namespace ArFactory.Serialization;

internal static class Utf8JsonReaderExtensions
{
	public static Vector2I GetVector2I(this Utf8JsonReader reader)
	{
		var x = reader.GetInt32();
		var y = reader.GetInt32();
		return new(x, y);
	}

	public static UnitSerializationData GetUnit(this Utf8JsonReader reader)
	{
		// var gloc = reader.GetVector2I();
		// var dir = reader.get
		throw new NotImplementedException();
	}
}