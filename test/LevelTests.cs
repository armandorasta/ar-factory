using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class LevelTests : ArTest.TestSuit
{
	private Level m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(m_Lv);
		m_Lv.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		m_Lv.World.Reset();
		m_Lv.World.SetDims(new(15, 10));
	}

	public override void AfterEach()
	{
		AssertFalse(m_Lv.IsSimRunning());
	}

	public override void AfterAll()
	{
		m_Lv.QueueFree();
		RemoveNode(m_Lv);
	}

	[ArTest.Test] public async Task TestWaitForTicks()
	{
		using (m_Lv.PlayButPauseOnEntry())
		{
			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 1); 
			await m_Lv.TickNTimesAsync(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 6);
			await m_Lv.TickNTimesAsync(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 11);
		}
	}
}