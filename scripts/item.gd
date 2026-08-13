class_name Item extends Node2D

@onready var sprite: Sprite2D = $CenterContainer/Sprite2D
@onready var label: Label = $CenterContainer/Sprite2D/CenterContainer/Label

var world: WorldPanel
var grid_loc: Vector2i

var _value: int


func setup(world_: WorldPanel, gloc: Vector2i, val: int) -> void:
	self.world = world_
	self.grid_loc = gloc
	sync_pos_with_grid()
	set_value(val)

	sprite.apply_scale(world.cell_width / 200 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)


func get_value() -> int:
	return _value

func set_value(new_val: int) -> void:
	_value = new_val
	label.text = str(new_val)


func sync_pos_with_grid() -> void:
	position = world.grid_to_pos(grid_loc)