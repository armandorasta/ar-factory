using Godot;
using System;
using GdUnit4;
using System.Threading.Tasks;

namespace ArFactory.Tests;
using static Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CommandTests
{
	private ISceneRunner m_Runner;
	private Level m_Level;
	private WorldPanel m_World;

	[TestCase]
	public void TestSomething()
	{
		var some = new int[5];
		AssertThat(some).HasSize(5);
	}

	[Before]
	public void Before()
	{
		m_Runner = ISceneRunner.Load("res://scenes/level.tscn");
		m_Level = (Level)m_Runner.Scene();
		m_World = m_Level.World;
	}

	[BeforeTest]
	public void BeforeTest()
	{
		Debug.Assert(!m_Level.IsSimRunning());
		m_World.Reset();
		// m_Level.SetTickRate(50);
		m_Runner.MaximizeView();
	}

	[TestCase]
	public async Task TestWaitForTicks()
	{
		m_Runner.MaximizeView();
		m_Level.StartSimulation();
		{
			await m_Level.WaitForTicks(5);
			AssertThat(m_Level.GetTicksSinceStart()).Equals(5);
			await m_Level.WaitForTicks(5);
			AssertThat(m_Level.GetTicksSinceStart()).Equals(10);
		}
		m_Level.EndSimulation();
	}

	[TestCase]
	public async Task TestCmdSpawn()
	{
		m_World.InstallTile(new TlBlackhole(Vector2I.Zero));
		m_World.PlaceInjector([new CmdSpawn(Vector2I.Zero, 5)]);
		
		m_Level.StartSimulation();
		{
			await m_Level.WaitForTicks(1);
			
			var tl = m_World.GetTile<TlBlackhole>(Vector2I.Zero);
			AssertThat(tl.HasItem()).IsTrue();

			var it = tl.GetItem();
			AssertThat(it.GetValue()).Equals(5);
			AssertThat(it.IsAllowedToMove()).IsTrue();
			AssertThat(it.IsMidAnimation()).IsFalse();	
		}
		m_Level.EndSimulation();
	}

	[TestCase]
	public async Task TestCmdSlide()
	{
		m_World.InstallTile(new TlBlackhole(new(0, 0)));
		m_World.InstallTile(new TlBlackhole(new(1, 0)));
		
		m_World.InstallTile(new TlBlackhole(new(2, 0)));
		m_World.InstallTile(new TlBlackhole(new(2, 1)));
		
		// m_World.InstallTile(new TlBlackhole(new(3, 0)));
		// m_World.InstallTile(new TlBlackhole(new(3, 1)));
		
		// m_World.InstallTile(new TlBlackhole(new(4, 0)));
		// m_World.InstallTile(new TlBlackhole(new(5, 0)));
		
		m_World.PlaceInjector([
			new CmdSpawn(new(0, 0), 1),
			new CmdSlide(new(0, 0), Direction.East),
			new CmdSpawn(new(2, 0), 2),
			new CmdSlide(new(2, 0), Direction.South),
			// new CmdSpawn(new(3, 1), 3),
			// new CmdSlide(new(3, 1), Direction.North),
			// new CmdSpawn(new(5, 0), 4),
			// new CmdSlide(new(5, 0), Direction.West),
		]);

		m_Level.StartSimulation(true);
		{
			await m_Level.WaitForTicks(1);
			AssertThat(m_World.GetTile<TlBlackhole>(new(0, 0)).HasItem()).IsFalse();
			AssertThat(m_World.GetTile<TlBlackhole>(new(1, 0)).HasItem()).IsTrue();
			AssertThat(m_World.GetTile<TlBlackhole>(new(2, 0)).HasItem()).IsFalse();
			AssertThat(m_World.GetTile<TlBlackhole>(new(2, 1)).HasItem()).IsTrue();
			var it1 = m_World.GetTile<TlBlackhole>(new(1, 0)).GetItem();
			AssertThat(it1.GetValue()).IsEqual(1);
			AssertThat(it1.IsAllowedToMove()).IsFalse();
			AssertThat(it1.IsMidAnimation()).IsTrue();
			var it2 = m_World.GetTile<TlBlackhole>(new(2, 1)).GetItem();
			AssertThat(it2.GetValue()).IsEqual(2);
			AssertThat(it2.IsAllowedToMove()).IsFalse();
			AssertThat(it2.IsMidAnimation()).IsTrue();

			await m_Level.WaitForTicks(1);
			AssertThat(m_World.GetTile<TlBlackhole>(new(0, 0)).HasItem()).IsFalse();
			AssertThat(m_World.GetTile<TlBlackhole>(new(1, 0)).HasItem()).IsTrue();
			AssertThat(m_World.GetTile<TlBlackhole>(new(2, 0)).HasItem()).IsFalse();
			AssertThat(m_World.GetTile<TlBlackhole>(new(2, 1)).HasItem()).IsTrue();
			it1 = m_World.GetTile<TlBlackhole>(new(1, 0)).GetItem();
			AssertThat(it1.GetValue()).IsEqual(1);
			AssertThat(it1.IsAllowedToMove()).IsTrue();
			AssertThat(it1.IsMidAnimation()).IsFalse();
			it2 = m_World.GetTile<TlBlackhole>(new(2, 1)).GetItem();
			AssertThat(it2.GetValue()).IsEqual(2);
			AssertThat(it2.IsAllowedToMove()).IsTrue();
			AssertThat(it2.IsMidAnimation()).IsFalse();
		}
		m_Level.EndSimulation();
	}
}