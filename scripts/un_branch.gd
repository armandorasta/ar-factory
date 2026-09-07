class_name UNBranch extends Unit

var Val: int

var io0: WorldPanel.TlIO
var out0: WorldPanel.TlOutput


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, init_dir: Direction, cmp_val: int) -> void:
	super.init(world_, TickType.ON_DEMAND, work_rate, gloc, Vector2i(1, 2), init_dir)
	assert(Item.MIN_VALUE <= cmp_val && cmp_val <= Item.MAX_VALUE)
	self.Val = cmp_val


func build_tiles() -> void:
	self.io0 = add_io(Vector2i(0, 0), Direction.EAST, Direction.WEST)
	self.out0 = add_output(Vector2i(0, 1), Direction.EAST)


func pend_new_commands() -> void:
	if !is_work_tick():
		return
		
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return

	pend_cmd(CmdCondSlide.from_tiles(io0, io0, _dir, out0, _dir, 
		func(tl): return tl.get_item().get_value() >= self.Val))
