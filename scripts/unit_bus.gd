class_name UnitBus extends Unit

@onready var sprite: Sprite2D = $Sprite2D

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int) -> void:
	super.init(world, gloc, direction, work_rate, 1)
	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(direction)


## Same as `UnitSupplier` except no spawning, and no `CmdMove` if no item.
func on_tick() -> void:
	if !is_work_tick():
		return

	var my_item := world.get_item(grid_loc)
	if my_item == null: # No item? just keep counting ticks.
		return

	var dest_gloc := grid_loc + dir_to_grid(dir)
	if !world.is_within_bounds(dest_gloc) || world.get_item(dest_gloc) != null:
		pause_this_tick()
		return
	
	world.add_cmd(CmdSlide.new(grid_loc, dir))


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
