class_name CmdMove extends Command

var grid_from: Vector2i
var grid_to: Vector2i
var tracked_item: Item

func _init(from: Vector2i, to: Vector2i) -> void:
	self.grid_from = from
	self.grid_to = to


func do_per_frame(world: WorldPanel) -> void:
	var src_loc := world.grid_to_pos(grid_from)
	var dest_loc := world.grid_to_pos(grid_to)
	var weight := world.get_tick_elapsed_millis() / world.tick_millis
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_spawn_tick(world: WorldPanel) -> void:
	var my_item := world.get_item(grid_from)
	assert(my_item != null)
	self.tracked_item = my_item


func on_tick(world: WorldPanel) -> void:
	# Snap it to final location just in case.
	tracked_item.position = world.grid_to_pos(grid_to)
	tracked_item.grid_loc = grid_to
	is_done = true