class_name UNBin extends Unit

@onready var sprite: Sprite2D = $Sprite2D


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i) -> void:
	super.init(world_, TickType.ON_DEMAND, 1, 1, 0, gloc)

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)

	input_slots[0].grid_loc = grid_loc


func pend_new_commands() -> void:
	if !is_work_tick():
		return

	if has_pending_cmds():
		pause_this_tick()
		return
	
	pend_cmd(CmdKill.new(world, grid_loc))
