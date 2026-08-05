class_name Supplier extends Unit

@onready var sprite: Sprite2D = $Sprite2D

var _seq: PackedInt32Array
var _rate: int

# When it reaches `_rate` we spawn an item and reset.
# It can go past `_rate` when the item is blocked.
var _count: int = 0

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, spawn_rate: int, sequence: PackedInt32Array) -> void:
	super.init(gloc, dir)
	self._seq = sequence
	self._rate = spawn_rate

	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5))
	set_dir(dir)


func _ready() -> void:
	pass


func on_tick(world: WorldPanel) -> void:
	_count += 1
	if _count < _rate:
		return
	
	var dest_gloc := grid_loc + dir_to_grid(dir)
	# If the current item is stuck for some reason, just wait for it to go away.
	if world.get_item(dest_gloc) != null:
		return

	_count = 0
	world.add_cmd(CmdSpawn.new(grid_loc, randi_range(1, 20)))
	world.add_cmd(CmdMove.new(grid_loc, dest_gloc))


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
