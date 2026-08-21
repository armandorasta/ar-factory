class_name UNBin extends Unit


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i) -> void:
	super.init(world_, TickType.ON_DEMAND, 1, gloc, Vector2.ONE)


func build_tiles() -> void:
	add_input(Vector2i.ZERO, Unit.Direction.WEST)


func pend_new_commands() -> void:
	if !is_work_tick():
		return

	if has_pending_cmds():
		pause_this_tick()
		return
	
	pend_cmd(CmdKill.new(world, grid_loc))
