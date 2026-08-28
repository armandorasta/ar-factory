class_name UNSlider extends Unit


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int) -> void:
	super.init(world_, TickType.STEADY, work_rate, gloc, Vector2i.ONE)


func build_tiles() -> void:
	add_io(Vector2i.ZERO, Unit.inv_dir(dir))


## Same as `UNSupplier` except no spawning, and no `CmdMove` if no item.
func pend_new_commands() -> void:
	if !is_work_tick():
		return
	
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return
	
	pend_cmd(CmdSlide.new(world, grid_loc, dir))
