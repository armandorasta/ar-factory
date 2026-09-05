class_name UNCombiner extends Unit

enum Operation {
	ADD,
	SUB,
	MUL,
	DIV,
}

var op: Operation

var in0: WorldPanel.TlInput
var in1: WorldPanel.TlInput
var out: WorldPanel.TlOutput


static func apply(op: Operation, lhs: int, rhs: int) -> int:
	match op:
		Operation.ADD: return lhs + rhs
		Operation.SUB: return lhs - rhs
		Operation.MUL: return lhs * rhs
		Operation.DIV: return lhs / rhs
		_            : return Item.MAX_VALUE + 1


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, op_: Operation) -> void:
	super.init(world_, TickType.ON_DEMAND, work_rate, gloc, Vector2i(2, 2))
	self.op = op_


func build_tiles() -> void:
	self.in0 = add_input(Vector2i(0, 0), Direction.WEST)
	self.in1 = add_input(Vector2i(0, 1), Direction.WEST)
	self.out = add_output(Vector2i(1, 1), Direction.EAST)


func pend_new_commands() -> void:
	if !is_work_tick():
		return
		
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return

	pend_cmd(CmdCombine.new(world, [in0, in1], out, func(vals): return apply(op, vals[0], vals[1])))
	pend_cmd(CmdSlide.new(world, out.get_grid_loc(), _dir))
