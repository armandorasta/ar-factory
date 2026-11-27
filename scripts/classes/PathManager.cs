using System;
using System.Collections.Generic;

namespace ArFactory;

public enum UnitTypeId
{
	Invalid  = 0,
	Bin      = 1,
	Branch   = 2,
	Cloner   = 3,
	Combiner = 4,
	Demander = 5,
	Platform = 6,
	Slider   = 7,
	Supplier = 8,
	Updater  = 9,
}

public interface IUnitWithTypeId
{
	static abstract UnitTypeId Id { get; }
}

public static class PathManager
{
	public UnitDef GetUnitInfo<TUnit>() where TUnit : Unit
	{
		return UnitTypeToId<TUnit>() switch
		{
			UnitTypeId.Bin => new(
				ScenePath:  "uid://cd2tfygwms3sj", 
				SpritePath: "uid://bet3fwwk1rtv0"),
			UnitTypeId.Branch => new(
				ScenePath:  "uid://clavmrbij5nx3", 
				SpritePath: "uid://c3msqm21kifbu"),
			UnitTypeId.Cloner => new(
				ScenePath:  "uid://br8ke48d0qnr6", 
				SpritePath: "uid://5sx6s3h31nch"),
			UnitTypeId.Combiner => new(
				ScenePath:  "uid://11uh28tktvc1", 
				SpritePath: "uid://qxafac5nyuxe"),
			UnitTypeId.Demander => new(
				ScenePath:  "uid://bphindl43xahl", 
				SpritePath: "uid://cx16bnx8m4co"),
			UnitTypeId.Platform => new(
				ScenePath:  "uid://bqqdrsmg1jayh", 
				SpritePath: ""),
			UnitTypeId.Slider => new(
				ScenePath:  "uid://knx24ed2y4xy", 
				SpritePath: "uid://b34och1pif20w"),
			UnitTypeId.Supplier => new(
				ScenePath:  "uid://dmn4mwly6oqh5", 
				SpritePath: "uid://bo8wn87npegaj"),
			UnitTypeId.Updater => new(
				ScenePath:  "uid://cuiklwu3pepi8", 
				SpritePath: "uid://c8uuervigh84k"),
			UnitTypeId.Invalid => throw new InvalidProgramException("Forgot to add `Id` for one of the children of `Unit`"),
			_ => throw new NotImplementedException(),
		};
	}

	private static UnitTypeId UnitTypeToId<TUnit>() where TUnit : Unit
	{
		var tu = typeof(TUnit);
		if (tu == typeof(UNSlider))   return UnitTypeId.Slider;
		if (tu == typeof(UNUpdater))  return UnitTypeId.Updater;
		if (tu == typeof(UNBranch))   return UnitTypeId.Branch;
		if (tu == typeof(UNCloner))   return UnitTypeId.Cloner;
		if (tu == typeof(UNCombiner)) return UnitTypeId.Combiner;
		if (tu == typeof(UNBin))      return UnitTypeId.Bin;
		if (tu == typeof(UNDemander)) return UnitTypeId.Demander;
		if (tu == typeof(UNSupplier)) return UnitTypeId.Supplier;
		if (tu == typeof(UNPlatform)) return UnitTypeId.Platform;
		else throw new NotImplementedException();
	}

	private readonly record struct UnitDef(string ScenePath, string SpritePath);	
}