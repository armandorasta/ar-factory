class_name CmdUpdate extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var update_type: UnitUpdater.UpdateType


func _init(gloc: Vector2i, up_t: UnitUpdater.UpdateType) -> void:
	super(1)
	self.grid_loc = gloc
	self.update_type = up_t


func on_spawn(world: WorldPanel) -> void:
	var it := world.get_item(grid_loc)
	assert(it != null)
	it.value = UnitUpdater.apply(update_type, it.value)
