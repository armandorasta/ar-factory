class_name Bus extends Unit

@onready var sprite: Sprite2D = $Sprite2D

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int) -> void:
	super.init(world, gloc, direction, work_rate)
	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(direction)


func _ready() -> void:
	pass


## Same as `Supplier` except no spawning, and no `CmdMove` if no item.
func on_tick(world: WorldPanel) -> void:
	if !is_work_tick():
		inc_count()
		return

	var my_item := world.get_item(grid_loc)
	if my_item == null: # No item? just keep counting ticks.
		inc_count()
		return

	var dest_gloc := grid_loc + dir_to_grid(dir)
	if !Rect2i(Vector2i(), world.dims).has_point(dest_gloc):
		# Stuck against the world panel boundaries?
		return

	if world.get_item(dest_gloc) != null:
		# If another item is in the way, just wait for it and shift the work to the next tick.
		return
	
	world.add_cmd(CmdMove.new(grid_loc, dest_gloc))
	inc_count()


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
