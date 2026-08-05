class_name CmdSpawn extends Command

var grid_loc: Vector2i
var value: int


func _init(gloc: Vector2i, val: int) -> void:
	self.grid_loc = gloc
	self.value = val


func do_per_frame(world: WorldPanel) -> void:
	pass # Nothing...


func on_spawn_tick(world: WorldPanel) -> void:
	world.add_item(grid_loc, randi_range(1, 20))


func on_tick(_world: WorldPanel) -> void:
	is_done = true
