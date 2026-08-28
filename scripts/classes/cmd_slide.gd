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
	super(world_, 2)
	self.grid_from = from
	self.dir = dir_

	assert(world_.is_within_bounds(grid_from))


func do_per_frame(_dt: float) -> void:
	if _state_machine.get_state() != _handle_after_animation:
		return

	var src_loc := world.grid_to_pos(grid_from)
	var dest_loc := world.grid_to_pos(get_grid_to())
	var weight := world.get_tick_elapsed_millis() / world._tick_millis
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_tick() -> void:
	_state_machine.call_next_state()


## If the item moved this pass, it will return true, 
## if it didn't, it will try to move it, if it succeeds, it will return true,
## otherwise it will return false.
func is_updated_this_pass() -> bool:
	assert(_state_machine.get_state() == _handle_dest_tile) # Blocked for real?
	assert(tracked_item != null && !tracked_item.cant_move_this_tick)
	
	var grid_to := get_grid_to()
	if world.get_tile(grid_to).is_reserved():
		return false

	tracked_item = world.teleport_item(grid_from, grid_to)
	tracked_item.cant_move_this_tick = true
	count_this_tick() # Undo the pausing from before
	_state_machine.set_state(_handle_after_animation)
	return true


func is_awaiting_anim() -> bool:
	return _state_machine.get_state() == _handle_after_animation


func get_grid_to() -> Vector2i:
	return grid_from + Unit.dir_to_grid(dir)


func _handle_default() -> void:
	assert(tracked_item == null)
	var src_tile := world.get_tile(grid_from)
	if !src_tile.has_item():
		pause_this_tick()
		return
		
	var src_item := src_tile.get_item()
	if src_item.cant_move_this_tick:
		pause_this_tick()
		return
	
	tracked_item = src_item
	_state_machine.set_state_and_call(_handle_dest_tile)


func _handle_dest_tile() -> void:
	var grid_to := get_grid_to()
	var dest_tile := world.get_tile(grid_to)

	var inv_dir := Unit.inv_dir(dir)
	if (dest_tile.is_free() ||
		dest_tile.is_io()    && inv_dir == dest_tile.get_alt_dir() ||
		dest_tile.is_input() && inv_dir == dest_tile.get_dir()):
		if dest_tile.is_reserved():
			# First iteration of this, there will be more later.
			world.blocked_slide_cmds.push_back(self)
			pause_this_tick() # Might be undone in the passes after.
			return
	else: # Solid or output or input facing wrong direction
		pause_this_tick()
		return # Forever and ever stuck...

	# Item must not have been stolen somehow!
	assert(world.get_tile(grid_from).has_item())
	tracked_item = world.teleport_item(grid_from, grid_to)
	tracked_item.cant_move_this_tick = true
	_state_machine.set_state(_handle_after_animation)
	# `do_per_frame` animates until next tick.


func _handle_after_animation() -> void:
	#tracked_item = world.teleport_item(grid_from, get_grid_to())
	tracked_item = null
	_state_machine.set_state(_handle_default)
