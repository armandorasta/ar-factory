class_name CmdUpdate extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i

## func(old_val: int) -> int
var callable: Callable


func _init(gloc: Vector2i, what_to_do: Callable) -> void:
	super(1)
	self.grid_loc = gloc
	self.callable = what_to_do


func on_spawn_tick(world: WorldPanel) -> void:
	var it := world.get_item(grid_loc)
	assert(it != null)
	it.value = callable.call(it.value)
