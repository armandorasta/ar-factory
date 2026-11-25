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
			WriteIndented = true,
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
	
	public static JsonElement SerializeUnitToElement(Unit u) => u.SerializeToElement();
	public static void DeserializeUnit(string typeName, JsonElement elem, UnitManager uniMan)
	{
		switch (typeName) {
		case nameof(UNBin):
		{
			var data = Deserialize<UNBinSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceBin(data!.GL);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNCloner):
		{
			var data = Deserialize<UNClonerSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceCloner(data!.GL, data.Dir, data.Rate);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNSlider):
		{
			var data = Deserialize<UNSliderSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceSlider(data!.GL, data.Dir, data.Rate);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNUpdater):
		{
			var data = Deserialize<UNUpdaterSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceUpdater(data!.GL, data.Dir, data.Rate, data.UpType);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNSupplier):
		{
			var data = Deserialize<UNSupplierSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceSupplier(data!.GL, data.Dir, data.Rate, data.Sequence!);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNDemander):
		{
			var data = Deserialize<UNDemanderSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceDemander(data!.GL, data.Dir, data.Sequence!);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNBranch):
		{
			var data = Deserialize<UNBranchSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceBranch(data!.GL, data.Dir, data.Rate, data.Check, data.CmpValue);
			u.DeserializeAfterReady(data);
			break;
		}
		case nameof(UNCombiner):
		{
			var data = Deserialize<UNCombinerSerializationData>(elem);
			Debug.AssertNotNull(data);
			var u = uniMan.PlaceCombiner(data!.GL, data.Dir, data.Rate, data.Operation);
			u.DeserializeAfterReady(data);
			break;
		}
		default:
			throw new NotImplementedException($"Forgot to add deserialization code for {typeName}");
		}
	}

	public static JsonElement SerializeUnitExtensionToElement(UnitManager.UnitExtension ue) => ue.SerializeToElement();
	public static void DeserializeUnitExtension(string typeName, JsonElement elem, UnitManager uniMan)
	{
		switch (typeName) {
		case nameof(UnitManager.UERuntimeLock):
		{
			var (data, parent) = DeserializeUnitExtensionBase<UERuntimeLockSerializationData>(elem, uniMan);
			uniMan.AttachExtension(new UnitManager.UERuntimeLock(parent!));
			break;
		}
		case nameof(UnitManager.UEDirectionToggle):
		{
			var (data, parent) = DeserializeUnitExtensionBase<UEDirectionToggleSerializationData>(elem, uniMan);
			uniMan.AttachExtension(new UnitManager.UEDirectionToggle(parent, data.Kind));
			break;
		}
		case nameof(UnitManager.UEConnector):
		{
			var (data, parent) = DeserializeUnitExtensionBase<UEConnectorSerializationData>(elem, uniMan);
			var partnerUnit = uniMan.GetUnit(data!.PartnerGL);
			Debug.AssertNotNull(parent);
			uniMan.AttachExtension(new UnitManager.UEConnector(parent!, partnerUnit!));
			break;
		}
		default:
			throw new NotImplementedException($"Forgot to add deserialization code for {typeName}");
		}
	}

	private static (TData, Unit) DeserializeUnitExtensionBase<TData>(JsonElement elem, UnitManager uniMan)
		where TData : UnitExtensionSerializationData
	{
		var data = Deserialize<TData>(elem);
		Debug.AssertNotNull(data);
		var parent = uniMan.GetUnit(data!.ParentGL);
		Debug.AssertNotNull(parent);
		return (data!, parent!);
	}

	public static JsonElement SerializeWorldPanel(WorldPanel world) => world.SerializeToElement();
	public static void DeserializeWorldPanel(JsonElement elem, WorldPanel world) => world.DeserializeAfterReady(elem);

	public static JsonElement SerializeUnitManager(UnitManager uniMan) => uniMan.SerializeToElement();
	public static void DeserializeUnitManager(JsonElement elem, UnitManager uniMan) => uniMan.Deserialize(elem);
}

#region .Unit

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
	[JsonInclude] public UNUpdater.UpdateType UpType;
}

public record UNSupplierSerializationData : UnitSerializationData
{
	[JsonInclude] public int[]? Sequence;
}

public record UNDemanderSerializationData : UnitSerializationData
{
	[JsonInclude] public int[]? Sequence;
}

public record UNBranchSerializationData : UnitSerializationData
{
	[JsonInclude] public CmdIf.CheckType Check;
	[JsonInclude] public int CmpValue;
}

public record UNCombinerSerializationData : UnitSerializationData
{
	[JsonInclude] public BinaryOperation Operation;
}

#endregion // .Unit
#region .UnitExtension

public record UnitExtensionSerializationData
{
	[JsonInclude] public Vector2I ParentGL;
}

public record UERuntimeLockSerializationData : UnitExtensionSerializationData { }
public record UEDirectionToggleSerializationData : UnitExtensionSerializationData
{
	[JsonInclude] public UnitManager.UEDirectionToggle.Kind Kind;
}
public record UEConnectorSerializationData : UnitExtensionSerializationData
{
	[JsonInclude] public Vector2I PartnerGL;
}

#endregion // .UnitExtension
#region .Other


#endregion