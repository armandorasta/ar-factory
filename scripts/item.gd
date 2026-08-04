class_name Item extends Node2D

@onready var sprite: Sprite2D = $Sprite2D
@onready var label: Label = $Label

var grid_loc: Vector2i
var value: int


func setup(world: WorldPanel, gloc: Vector2i, val: int) -> void:
	self.grid_loc = gloc
	self.value = val

	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * Vector2(grid_loc.x + 0.5, grid_loc.y + 0.5))