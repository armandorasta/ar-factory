class_name Bus extends Unit

@onready var sprite: Sprite2D = $Sprite2D

## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction) -> void:
	super.init(gloc, direction)
	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5))
	set_dir(direction)


func _ready() -> void:
	pass


func on_tick(world: WorldPanel) -> void:
	pass


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
