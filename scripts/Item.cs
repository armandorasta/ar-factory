using System.Runtime.CompilerServices;
using System.Text;
using Godot;

namespace ArFactory;

public partial class Item : Node2D
{
	public const int MaxValue = 1000;
	public const int MinValue = -MaxValue;

	// Nodes
	public Sprite2D Sprite { get; private set; }
	public Label CenterLabel { get; private set; }


	// Publics
	public int Value => m_Value;
	public Tile ParentTile => m_ParentTile;
	/// <summary>
	/// Crashes the program when the tile is floating!
	/// </summary>
	public Vector2I GridLoc
	{
		get
		{
			Debug.AssertNotNull(m_ParentTile);
			return m_ParentTile.GridLoc;
		}
	}


	// Privates
	private Tile m_ParentTile;
	private bool m_bDisallowMoveThisTick = false;
	private bool m_bMidAnimation = false;
	private int m_Value;


	public static bool IsValueWithinRange(int val) => MinValue <= val && val <= MaxValue;
	public static int Clamp(int val) => int.Clamp(val, MinValue, MaxValue);


	public override void _Ready()
	{
		Sprite = GetNode<Sprite2D>("CenterContainer/Sprite2D");
		CenterLabel = GetNode<Label>("CenterContainer/Sprite2D/CenterContainer/Label");
	}
	
	public void Setup(WorldPanel world, Vector2I gloc, int val)
	{
		m_ParentTile = world.GetTile(gloc);
		SetValue(val);
		SyncPosWithGrid(world);
		Sprite.ApplyScale(world.CellWidth / 200.0f * Vector2.One);
		Sprite.Translate(0.5f * world.CellWidth * Vector2.One);
	}

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
		// Debug.Assert(IsValueWithinRange(newValue));
		m_Value = Clamp(newVal);
		CenterLabel.Text = m_Value.ToString();
	}

	/// <summary>
	/// This function obviously crashes the program if the item is floating.
	/// Syncs it's actual position on the grid with it's grid location.
	/// <b>This function sets the position attribute, so ovoid using it in a loop.</b>
	/// </summary>
	public void SyncPosWithGrid(WorldPanel world)
	{
		Debug.AssertNotNull(m_ParentTile);
		Position = world.GridToPos(m_ParentTile.GridLoc);
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
		var bui = new StringBuilder($"Item[{m_Value} at {GridLoc}");
		if (IsMidAnimation()) bui.Append(", anim");
		if (!IsAllowedToMove()) bui.Append(", stuck");
		return bui.Append(']').ToString();
	} 
}
