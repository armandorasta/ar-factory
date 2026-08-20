class_name UNSlider extends Unit

@onready var sprite: Sprite2D = $Sprite2D

## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int) -> void:
	super.init(world_, Type.STEADY, work_rate, 1, 1, gloc)
	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)

	input_slots[0].grid_loc = grid_loc
	output_slots[0].grid_loc = grid_loc


## Same as `UNSupplier` except no spawning, and no `CmdMove` if no item.
func pend_new_commands() -> void:
	if !is_work_tick():
		return
	
	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return
	
	pend_cmd(CmdSlide.new(world, grid_loc, dir))


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
