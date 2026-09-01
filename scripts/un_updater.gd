class_name UNUpdater extends Unit

enum UpdateType {
	DOUBLE,
	NEGATE,
	INCREMENT
}


@onready var label: Label = $Sprite2D/CenterContainer/HBoxContainer/Label

var update_type: UpdateType


static func apply(up_t: UpdateType, val: int) -> int:
	match up_t:
		UpdateType.DOUBLE   : return val * 2
		UpdateType.NEGATE   : return -val
		UpdateType.INCREMENT: return val + 1
	
	assert(false, "Unhandled update type")
	return -1


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, update_type_: UpdateType) -> void:
	super.init(world_, TickType.ON_DEMAND, work_rate, gloc, Vector2i.ONE)
	self.update_type = update_type_
	match update_type:
		UpdateType.DOUBLE   : label.text = "2x"
		UpdateType.NEGATE   : label.text = "-x"
		UpdateType.INCREMENT: label.text = "x+1"


func build_tiles() -> void:
	add_io(Vector2i.ZERO, dir, Unit.Direction_inv(dir))


func pend_new_commands() -> void:
	if !is_work_tick():
		return
		
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return

	pend_cmd(CmdUpdate.new(world, grid_loc, update_type))
	pend_cmd(CmdSlide.new(world, grid_loc, dir))
