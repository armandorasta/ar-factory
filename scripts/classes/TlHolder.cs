using System;
using System.Text;
using Godot;

namespace ArFactory;

public abstract partial class TlHolder(Vector2I gloc) : Tile(gloc)
{
	private Item m_Item;
	private bool m_bReserved = false;

	public override bool IsReserved() => m_bReserved;
	public override void SetReserved(bool toWhat) => m_bReserved = toWhat;
	public override bool HasItem() => m_Item != null;
	public override Item GetItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		return m_Item;
	}

	/// <summary>
	/// This doesn't sync the world position as the item doesn't have access to the world, hence
	/// use <see cref="WorldPanel.SpawnItem"/> instead.
	/// </summary>
	public void SetItemUnchecked(Item newItem, bool bOverride = false)
	{
		Debug.Assert(newItem.GetParent() is WorldPanel);
		Debug.Assert(bOverride || !HasItem());
		DestroyItem(true);
		m_Item = newItem;
		m_Item.GridLoc = m_GridLoc;
		m_bReserved = true;
	}

	/// <summary>
	/// Sends the item into oblivion.
	/// </summary>
	public void DestroyItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		if (m_Item != null)
		{
			m_Item.GetParent()?.RemoveChild(m_Item);
			m_Item.QueueFree();
			m_Item = null;
		}
		m_bReserved = false;
	}

	/// <summary>
	/// Removes the item from the block and returns it, the caller has full ownership of the item.
	/// </summary>
	public Item ExtractItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		var myItem = m_Item;
		m_Item = null;
		m_bReserved = false;
		return myItem;
	}

	public override string ToString()
	{
		var bui = new StringBuilder("TlHolder[");
		bui.Append(m_GridLoc);
		if (m_Item is not null) bui.Append($", item({m_Item.GetValue()})");
		return bui.Append(']').ToString();
	}
}

