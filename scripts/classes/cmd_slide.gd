class_name CmdSlide extends Command

var grid_from: Vector2i
var dir: Unit.Direction
var tracked_item: Item

func _init(world_: WorldPanel, from: Vector2i, dir_: Unit.Direction) -> void:
	super(world_, 1)
	self.grid_from = from
	self.dir = dir_
	
	var grid_to := _get_grid_to()
	assert(world_.is_within_bounds(grid_from))
	assert(world_.is_within_bounds(grid_to))
	
	world_.get_tile(grid_to).set_reserved(true)


func do_per_frame(_dt: float) -> void:
	var src_loc := world.grid_to_pos(grid_from)
	var dest_loc := world.grid_to_pos(_get_grid_to())
	var weight := world.get_tick_elapsed_millis() / world.tick_millis
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_spawn() -> void:
	assert(world.get_tile(grid_from).has_item())
	var grid_to := _get_grid_to()
	var dest_tile := world.get_tile(grid_to)
	assert(!dest_tile.has_item() && dest_tile.is_reserved())
	tracked_item = world.teleport_item(grid_from, grid_to)


func _get_grid_to() -> Vector2i:
	return grid_from + Unit.dir_to_grid(dir)
