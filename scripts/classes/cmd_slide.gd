class_name CmdSlide extends Command

var grid_from: Vector2i
var dir: Unit.Direction
var tracked_item: Item

func _init(from: Vector2i, dir_: Unit.Direction) -> void:
	super(1)
	self.grid_from = from
	self.dir = dir_


func do_per_frame(world: WorldPanel) -> void:
	var src_loc := world.grid_to_pos(grid_from)
	var dest_loc := world.grid_to_pos(_get_grid_to())
	var weight := world.get_tick_elapsed_millis() / world.tick_millis
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_spawn(world: WorldPanel) -> void:
	var my_item := world.get_item(grid_from)
	assert(my_item != null)
	self.tracked_item = my_item
	tracked_item.grid_loc = _get_grid_to()


func _get_grid_to() -> Vector2i:
	return grid_from + Unit.dir_to_grid(dir)