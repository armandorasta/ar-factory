class_name Doubler extends Unit

@onready var sprite: Sprite2D = $Sprite2D
var _item: Item

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int) -> void:
	super.init(world, gloc, direction, work_rate)

	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)


func on_tick(world: WorldPanel) -> void:
	if _item == null: # Unit we are fed an item we don't increment the count.
		_item = world.get_item(grid_loc)
		if _item == null: # Nothing? get outta here.
			return
	
	if !is_work_tick():
		inc_count()
		return

	var dest_gloc := grid_loc + dir_to_grid(dir)
	if !world.is_within_bounds(dest_gloc) || world.get_item(dest_gloc) != null:
		return

	world.add_cmd(CmdUpdate.new(grid_loc, func(x): return x * 2))
	world.add_cmd(CmdSlide.new(grid_loc, dir))
	
	_item = null
	inc_count()


func reset() -> void:
	super.reset()
	_item = null


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
