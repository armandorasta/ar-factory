class_name CmdSlide extends Command

enum State {
	DEFAULT,
	DEST_TILE,
	AFTER_ANIMATION,
}

var grid_from: Vector2i
var dir: Unit.Direction
var tracked_item: Item

var _state_machine: StateMachine = StateMachine.new(_handle_default)


func _init(world_: WorldPanel, from: Vector2i, dir_: Unit.Direction) -> void:
	super(world_, 1)
	self.grid_from = from
	self.dir = dir_

	assert(world_.is_within_bounds(grid_from))


func do_per_frame(_dt: float) -> void:
	if _state_machine.get_state() != _handle_after_animation:
		return

	var src_loc := world.grid_to_pos(grid_from)
	var dest_loc := world.grid_to_pos(_get_grid_to())
	var weight := world.get_tick_elapsed_millis() / world.tick_millis
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_tick() -> void:
	_state_machine.call_next_state()


func _get_grid_to() -> Vector2i:
	return grid_from + Unit.dir_to_grid(dir)


func _handle_default() -> void:
	assert(tracked_item == null)
	if !world.get_tile(grid_from).has_item():
		pause_this_tick()
		return
	
	_state_machine.set_state_and_call(_handle_dest_tile)


func _handle_dest_tile() -> void:
	var grid_to := _get_grid_to()
	var dest_tile := world.get_tile(grid_to)
	if dest_tile.is_reserved():
		pause_this_tick()
		return

	# Item must not have been stolen somehow!
	assert(world.get_tile(grid_from).has_item())
	tracked_item = world.teleport_item(grid_from, _get_grid_to())
	_state_machine.set_state(_handle_after_animation)
	# `do_per_frame` animates until next tick.


func _handle_after_animation() -> void:
	#tracked_item = world.teleport_item(grid_from, _get_grid_to())
	tracked_item = null
	_state_machine.set_state(_handle_default)
