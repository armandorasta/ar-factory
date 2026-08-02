class_name Bus extends Unit

@onready var sprite: Sprite2D = $Sprite2D

## Must be called after _ready
func setup(world: WorldPanel, grid_loc: Vector2i) -> void:
	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * Vector2(grid_loc.x + 0.5, grid_loc.y + 0.5))
	dir = Direction.NORTH
	set_dir(dir)


func _ready() -> void:
	pass


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
