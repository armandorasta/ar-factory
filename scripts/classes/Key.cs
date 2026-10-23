using Godot;
using System;
using System.Collections.Generic;

namespace ArFactory;

public class Key(Vector2I gloc, UNSlider tgt)
{
	public Vector2I GLocation = gloc;
	public UNSlider Target = tgt;


	public void OnTick(Editor.OnTickMediator ticker)
	{
		if (HasPendingCmds())
		{
			return;
		}

		ticker.Pender.Pend([
			[ new CmdAwait([GLocation]) ],
			[ new CmdAwait([Target.GLocation]) ],
			[ new CmdSetLocked(Target.GLocation, false) ],
			[ new CmdVacate([Target.GLocation]) ],
			[ new CmdSetLocked(Target.GLocation, true) ],
		]);
	}
}
