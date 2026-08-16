class_name CmdSpawn extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var value: int
var _is_tick1: bool = false

func _init(world_: WorldPanel, gloc: Vector2i, val: int) -> void:
	super(world_, 1)
	self.grid_loc = gloc
	self.value = val


func on_tick() -> void:
	if world.get_tile(grid_loc).is_reserved():
		pause_this_tick()
		return
		
	if _is_tick1:
		return
	
	world.add_item(grid_loc, value)
	_is_tick1 = true
