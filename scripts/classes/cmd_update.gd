class_name CmdUpdate extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var callable: Callable


## `what_to_do` the old value and returns the new value to be assigned to the item.
func _init(gloc: Vector2i, what_to_do: Callable) -> void:
	super(0)
	self.grid_loc = gloc
	self.callable = what_to_do


func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(grid_loc) is WorldPanel.TlHolder)
	var my_tile := lv.world.get_tile(grid_loc) as WorldPanel.TlHolder
	if !my_tile.has_item():
		pause_this_tick()
		return
	
	var it := my_tile.get_item()
	it.set_value(callable.call(it.get_value()))
