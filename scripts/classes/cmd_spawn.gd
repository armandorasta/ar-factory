class_name CmdSpawn extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var value: int


func _init(gloc: Vector2i, val: int) -> void:
	super(1)
	self.grid_loc = gloc
	self.value = val


func on_spawn_tick(world: WorldPanel) -> void:
	world.add_item(grid_loc, value)
