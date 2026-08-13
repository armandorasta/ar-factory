class_name CmdUpdate extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var update_type: UnitUpdater.UpdateType


func _init(world_: WorldPanel, gloc: Vector2i, up_t: UnitUpdater.UpdateType) -> void:
	super(world_, 1)
	self.grid_loc = gloc
	self.update_type = up_t


func on_spawn() -> void:
	assert(world.get_tile(grid_loc).has_item())
	var it := world.get_tile(grid_loc).get_item()
	it.set_value(UnitUpdater.apply(update_type, it.get_value()))
