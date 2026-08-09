class_name Supplier extends Unit

@onready var sprite: Sprite2D = $Sprite2D

var _seq: PackedInt32Array
var _index: int = 0

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int, 
	sequence: PackedInt32Array) -> void:
	super.init(world, gloc, direction, work_rate)
	self._seq = sequence

	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)


func on_tick(world: WorldPanel) -> void:
	if !is_work_tick():
		inc_count()
		return
	
	if _index >= _seq.size():
		return

	if world.get_item(grid_loc + dir_to_grid(dir)) != null:
		# If another item is in the way, just wait for it to go away.
		return

	world.add_cmd(CmdSpawn.new(grid_loc, _seq[_index]))
	world.add_cmd(CmdSlide.new(grid_loc, dir))
	
	_index += 1
	inc_count()


func reset() -> void:
	super.reset()
	_index = 0


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
