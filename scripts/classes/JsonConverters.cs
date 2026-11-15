using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ArFactory.Serialization;

internal class UNBinJsonConverter : JsonConverter<UNBin>
{
	public override void Write(Utf8JsonWriter writer, UNBin value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteUnit("Base", value);
		writer.WriteEndObject();
	}

	public override UNBin? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// reader.GetVector2I();
		return null;
	}
}