using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ArFactory.Serialization;

public class Serializer
{
	private static readonly JsonSerializerOptions sc_Options = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};

        foreach (var tp in Assembly.GetExecutingAssembly().GetTypes())
        {
			if (tp.Namespace != "ArFactory.Serialization")
			{
				continue;
			}

            if (typeof(JsonConverter).IsAssignableFrom(tp))
            {
                var converter = (JsonConverter)Activator.CreateInstance(tp)!;
                options.Converters.Add(converter);
            }
        }
        return options;
    }

	public static string Serialize<T>(T obj)
		=> JsonSerializer.Serialize<T>(obj, sc_Options);
	public static T? Deserialize<T>(string infoStr)
		=> JsonSerializer.Deserialize<T>(infoStr, sc_Options);

	public static JsonElement SerializeToElement<T>(T obj)
		=> JsonSerializer.SerializeToElement<T>(obj, sc_Options);
	public static T? Deserialize<T>(JsonElement elem)
		=> JsonSerializer.Deserialize<T>(elem, sc_Options);
	

	public static void DeserializeUnit(string typeName, JsonElement elem, UnitManager uniMan)
	{
		switch (typeName) {
		case nameof(UNBin):
		{
			var data = Deserialize<UNBinSerializationData>(elem);
			Debug.AssertNotNull(data);
			var bin = uniMan.PlaceBin(data!.GL);
			bin.Restore(data);
			break;
		}
		case nameof(UNCloner):
		{
			var data = Deserialize<UNClonerSerializationData>(elem);
			Debug.AssertNotNull(data);
			var clo = uniMan.PlaceCloner(data!.GL, data.Dir, data.Rate);
			clo.Restore(data);
			break;
		}
		case nameof(UNSlider):
		{
			var data = Deserialize<UNSliderSerializationData>(elem);
			Debug.AssertNotNull(data);
			var slid = uniMan.PlaceSlider(data!.GL, data.Dir, data.Rate);
			slid.Restore(data);
			break;
		}
		// case nameof(UNUpdater):
		// {
		// 	var info = UNUpdater.Deserialize(su["Info"]);
		// 	var upd = PlaceUpdater(info.BaseInfo.GL, info.BaseInfo.Dir, info.BaseInfo.Rate, info.UpType);
		// 	DeserializeBaseInfo(ref info.BaseInfo, upd);
		// 	break;
		// }
		// case nameof(UNSupplier):
		// {
		// 	var info = UNSupplier.Deserialize(su["Info"]);
		// 	var supp = PlaceSupplier(info.BaseInfo.GL, info.BaseInfo.Dir, info.BaseInfo.Rate, info.Sequence);
		// 	DeserializeBaseInfo(ref info.BaseInfo, supp);
		// 	break;
		// }
		// case nameof(UNDemander):
		// {
		// 	var info = UNDemander.Deserialize(su["Info"]);
		// 	var dem = PlaceDemander(info.BaseInfo.GL, info.BaseInfo.Dir, info.Sequence);
		// 	DeserializeBaseInfo(ref info.BaseInfo, dem);
		// 	break;
		// }
		default:
			throw new NotImplementedException($"Forgot to add deserialization code for {typeName}");
		}
	}

	private static void DeserializeUnitBaseInfo(UnitSerializationData baseData, Unit tgtUnit)
	{
		if (baseData.IsLocked)
		{
			tgtUnit.Lock();
		}
	}
}

public record UnitSerializationData
{
	[JsonInclude] public Vector2I GL;
	[JsonInclude] public Direction Dir;
	[JsonInclude] public int Rate;
	[JsonInclude] public bool IsLocked;
}

public record UNBinSerializationData : UnitSerializationData { }
public record UNSliderSerializationData : UnitSerializationData { }
public record UNClonerSerializationData : UnitSerializationData { }

public record UNUpdaterSerializationData : UnitSerializationData
{
	[JsonInclude] public required UNUpdater.UpdateType UpType;
}

public record UNSupplierSerializationData : UnitSerializationData
{
	[JsonInclude] public required int[] Sequence;
}

public record UNDemanderSerializationData : UnitSerializationData
{
	[JsonInclude] public required int[] Sequence;
}
