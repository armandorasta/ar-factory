class_name UNCloner extends Unit

var io0: WorldPanel.TlIO
var out0: WorldPanel.TlOutput


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int) -> void:
	super.init(world_, TickType.ON_DEMAND, work_rate, gloc, Vector2i(1, 2))


func build_tiles() -> void:
	self.io0 = add_io(Vector2i(0, 0), Direction.EAST, Direction.WEST)
	self.out0 = add_output(Vector2i(0, 1), Direction.EAST)


func pend_new_commands() -> void:
	if !is_work_tick():
		return
		
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return

	pend_cmd(CmdClone.new(world, io0, [out0]))
	pend_cmd(CmdSlide.new(world, io0.get_grid_loc(), _dir))
	pend_cmd(CmdSlide.new(world, out0.get_grid_loc(), _dir))
