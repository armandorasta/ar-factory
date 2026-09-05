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


static func from_output(output: WorldPanel.TlOutput) -> CmdSlide:
	return CmdSlide.new(output.get_grid_loc(), output.get_dir())


func _init(gfrom: Vector2i, dir_: Unit.Direction) -> void:
	super(2)
	self.grid_from = gfrom
	self.dir = dir_


func do_per_frame(_dt: float, lv: Level) -> void:
	if _state_machine.get_state() != _handle_after_animation:
		return

	var src_loc := lv.world.grid_to_pos(grid_from)
	var dest_loc := lv.world.grid_to_pos(_get_grid_to())
	var weight := lv.get_tick_elapsed_millis() / lv.get_tick_elapsed_millis()
	tracked_item.position = src_loc.lerp(dest_loc, weight)


func on_tick(lv: Level) -> void:
	_state_machine.call_next_state(lv.world)


## If the item moved this pass, it will return true, 
## if it didn't, it will try to move it, if it succeeds, it will return true,
## otherwise it will return false.
func is_updated_this_pass(world: WorldPanel) -> bool:
	assert(_state_machine.get_state() == _handle_dest_tile) # Blocked for real?
	assert(tracked_item != null && !tracked_item.cant_move_this_tick)
	
	var grid_to := _get_grid_to()
	if world.get_tile(grid_to).is_reserved():
		return false

	tracked_item = world.teleport_item(grid_from, grid_to)
	tracked_item.cant_move_this_tick = true
	count_this_tick() # Undo the pausing from before
	_state_machine.set_state(_handle_after_animation)
	return true


func is_awaiting_anim() -> bool:
	return _state_machine.get_state() == _handle_after_animation


func _get_grid_to() -> Vector2i:
	return grid_from + Unit.Direction_to_grid(dir)


func _handle_default(world: WorldPanel) -> void:
	assert(tracked_item == null)
	assert(world.get_tile(grid_from) is WorldPanel.TlHolder)
	assert(world.get_tile(_get_grid_to()) is WorldPanel.TlHolder)
	var src_tile := world.get_tile(grid_from) as WorldPanel.TlHolder
	if !src_tile.has_item():
		pause_this_tick()
		return
		
	var src_item := src_tile.get_item()
	if src_item.cant_move_this_tick:
		pause_this_tick()
		return
	
	tracked_item = src_item
	_state_machine.set_state_and_call(_handle_dest_tile, world)


func _handle_dest_tile(world: WorldPanel) -> void:
	var grid_to := _get_grid_to()
	if !world.has_tile(grid_to): # Items are not allowed to be scattered in the wild
		pause_this_tick() # Forever and ever
		return
	
	var src_tile := world.get_tile(grid_from)
	var dest_tile := world.get_tile(grid_to)
	if dest_tile.can_item_enter_in_dir(src_tile.get_item(), dir):
		if dest_tile.is_reserved():
			# First iteration of this, there will be more later.
			world.blocked_slide_cmds.push_back(self)
			pause_this_tick() # Might be undone in the passes after.
			return
	else: # Solid or any other type facing wrong direction.
		pause_this_tick()
		return # Forever and ever stuck...

	# Item must not have been stolen somehow!
	assert(world.get_tile(grid_from).has_item())
	tracked_item = world.teleport_item(grid_from, grid_to)
	tracked_item.cant_move_this_tick = true
	_state_machine.set_state(_handle_after_animation)
	# `do_per_frame` animates until next tick.


func _handle_after_animation(_world: WorldPanel) -> void:
	#tracked_item = world.teleport_item(grid_from, _get_grid_to())
	tracked_item = null
	_state_machine.set_state(_handle_default)
