class_name Item extends Node2D

@onready var sprite: Sprite2D = $CenterContainer/Sprite2D
@onready var label: Label = $CenterContainer/Sprite2D/CenterContainer/Label

var world: WorldPanel
var grid_loc: Vector2i
var value: int:
	set(new_val):
		value = new_val
		label.text = str(value)


func setup(world_: WorldPanel, gloc: Vector2i, val: int) -> void:
	self.world = world_
	self.value = val
	self.grid_loc = gloc
	sync_pos_with_grid()

	sprite.apply_scale(world.cell_width / 200 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)


func sync_pos_with_grid() -> void:
	position = world.grid_to_pos(grid_loc)
