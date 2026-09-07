class_name UNSlider extends Unit


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, init_dir: Direction) -> void:
	super.init(world_, TickType.STEADY, work_rate, gloc, Vector2i.ONE, init_dir)


func build_tiles() -> void:
	add_slider(Vector2i.ZERO, Direction.EAST)


func pend_new_commands() -> void:
	if !is_work_tick():
		return
	
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return
	
	pend_cmd(CmdSlide.new(grid_loc, _dir))


func is_valid_dir(_d: Direction) -> bool:
	return true ## Sliders can face any direction