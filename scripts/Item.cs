using System.Diagnostics;
using System.Text;
using ArFactory;
using Godot;

namespace ArFactory;

public partial class Item : Node2D
{
	public const int MaxValue = 1000;
	public const int MinValue = -MaxValue;

	// Nodes
	public Sprite2D Sprite {get; private set;}
	public Label CenterLabel {get; private set;}


	// Publics
	public Vector2I GridLoc => m_GridLoc;
	public int Value => m_Value;


	// Privates
	private Vector2I m_GridLoc;
	private bool m_bDisallowMoveThisTick = false;
	private bool m_bMidAnimation = false;
	private int m_Value;


	public override void _Ready()
	{
		Sprite = GetNode<Sprite2D>("CenterContainer/Sprite2D");
		CenterLabel = GetNode<Label>("CenterContainer/Sprite2D/CenterContainer/Label");
	}
	
	public void Setup(WorldPanel world, Vector2I gloc, int val)
	{
		m_GridLoc = gloc;
		SyncPosWithGrid(world);
		SetValue(val);
		Sprite.ApplyScale(world.CellWidth / 200.0f * Vector2.One);
		Sprite.Translate(0.5f * world.CellWidth * Vector2.One);
	}

	public int GetValue() => m_Value;

	/// <summary>
	/// Not mid sliding animation or something.
	/// </summary>
	public bool IsAllowedToMove() => !m_bDisallowMoveThisTick;

	/// <summary>
	/// Items may not be deleted mid-animation. They may only be deleted when this function returns 
	/// false.
	/// </summary>
	public bool IsMidAnimation() => m_bMidAnimation;

	/// <summary>
	/// Clamps the value within range.
	/// </summary>
	public void SetValue(int newVal)
	{
		// Debug.Assert(MinValue <= newVal && newVal <= MaxValue);
		m_Value = int.Clamp(newVal, Item.MinValue, Item.MaxValue);
		CenterLabel.Text = m_Value.ToString();
	}

	/// <summary>
	/// Only use with floating items, otherwise control it through it's parent world and tile.
	/// </summary>
	public void SetGridLocUnsafe(Vector2I gloc) 
	{
		Debug.Assert(IsAllowedToMove());
		m_GridLoc = gloc; 
	}

	/// <summary>
	/// Syncs it's actual position on the grid with it's grid location.
	/// </summary>
	public void SyncPosWithGrid(WorldPanel world)
	{
		Position = world.GridToPos(m_GridLoc);
	}

	/// <summary>
	/// After calling this, changing the grid-loc of the tile crashes the game, until 
	/// <see cref="ResetMovementFlag"/> is called. Changing the position however is fine.
	/// </summary>
	public void DisallowMovementThisTick()
	{
		m_bDisallowMoveThisTick = true;
	}

	/// <summary>
	/// Items are once again allowed to move this tick, this function is not meant to be called by 
	/// the user.
	/// </summary>
	public void ResetMovementFlag()
	{
		m_bDisallowMoveThisTick = false;
	}

	/// <summary>
	/// True means it's mid-animation, false means otherwise.
	/// </summary>
	public void SetMidAnimationFlag(bool newVal)
	{
		m_bMidAnimation = newVal;
	}

	public override string ToString()
	{
		var bui = new StringBuilder($"Item[{m_GridLoc}, val: {m_Value}");
		if (IsMidAnimation()) bui.Append(", anim");
		if (!IsAllowedToMove()) bui.Append(", locked");
		return bui.Append(']').ToString();
	} 
}
