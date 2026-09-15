using System;

namespace ArFactory;

public static class Utilz
{
	/// <summary>
	/// Base[other, stuff] + Sub[some, thing] = Sub[other, stuff, some, thing]
	/// </summary>
	/// <param name="strBase"><c>base.toString()</c></param>
	/// <param name="strNew">The version ToString would produce had there was no base class</param>
	public static string AppendToBaseToString(string strBase, string strNew)
	{
		var nBaseBra = strBase.IndexOf('[');
		var baseInnerStr = strBase[(nBaseBra + 1) .. (strBase.Length-1)];
		var nNewBra = strNew.IndexOf('[');
		var newStuffStr = strNew[(nNewBra + 1) .. (strNew.Length-1)];
		return $"{strNew[..nNewBra]}[{baseInnerStr}; {newStuffStr}]";
	}

	/// <summary>
	/// Some[some, stuff] => SomeOther[some, stuff]
	/// </summary>
	/// <param name="strBase">base.ToString()</param>
	/// <param name="newName">The replacement name</param>
	/// <returns></returns>
	public static string ReplaceBaseNameInToString(string strBase, string newName)
	{
		return $"{newName}[{strBase[(strBase.IndexOf('[') + 1) .. (strBase.Length-1)]}]";		
	}
}
