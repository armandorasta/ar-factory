class_name CmdUpdate extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var update_type: UnitUpdater.UpdateType


func _init(world_: WorldPanel, gloc: Vector2i, up_t: UnitUpdater.UpdateType) -> void:
	super(world_, 0)
	self.grid_loc = gloc
	self.update_type = up_t


func on_tick() -> void:
	var my_tile := world.get_tile(grid_loc)
	if !my_tile.has_item():
		pause_this_tick()
		return
	
	var it := my_tile.get_item()
	var updated_value := UnitUpdater.apply(update_type, it.get_value())
	it.set_value(updated_value)
