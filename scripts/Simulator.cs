using Godot;
using System;

namespace ArFactory;

public class Simulator
{
	private enum PlayMode
	{
		Play,
		Debug,
	}


	private PlayMode m_CurrPlayMode;
	private Godot.Timer m_TickTimer = new();
	private int m_TickCount = 0; // Number of ticks since the start.
	private float m_TickMillis;
}
