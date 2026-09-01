@abstract class_name Unit extends Node2D

## `Direction_inv` relys on the order of these.
enum Direction {
	NORTH = 0, 
	EAST  = 1,
	WEST  = 2, 
	SOUTH = 3, 
}

enum TickType {
	STEADY,    # Always counts ticks, even when no item is fed, like sliders.
	ON_DEMAND, # Counts ticks only when all item slots are filled, like updaters.
}

@onready var sprite: Sprite2D = $Sprite2D

var tick_type: TickType
var dir: Direction = Direction.EAST
var grid_loc: Vector2i
var rate: int
var world: WorldPanel
var input_slots: Array[InputSlot] = []
var output_slots: Array[OutputSlot] = []
# var tiles: Array[WorldPanel.Tile] = []
var dims: Vector2i

## Keeps track of the `rate` every tick.
var _count: int = 0

## Commands that need to finish for the unit to keep operating again
var _pending_cmds: Array[Command] = []


static func Direction_is_valid(my_dir: Direction) -> bool:
	return 0 <= my_dir && my_dir <= 3


static func Direction_to_grid(my_dir: Direction) -> Vector2i:
	assert(Direction_is_valid(my_dir))
	match my_dir:
		Direction.NORTH  : return Vector2i(0, -1)
		Direction.SOUTH  : return Vector2i(0, +1)
		Direction.EAST   : return Vector2i(+1, 0)
		Direction.WEST, _: return Vector2i(-1, 0)


static func Direction_inv(my_dir: Direction) -> Direction:
	assert(Direction_is_valid(my_dir))
	match my_dir:
		Direction.NORTH  : return Direction.SOUTH
		Direction.SOUTH  : return Direction.NORTH
		Direction.EAST   : return Direction.WEST
		Direction.WEST, _: return Direction.EAST


static func Direction_rotate90(my_dir: Direction) -> Direction:
	assert(Direction_is_valid(my_dir))
	match my_dir:
		Direction.NORTH  : return Direction.EAST
		Direction.SOUTH  : return Direction.WEST
		Direction.EAST   : return Direction.SOUTH
		Direction.WEST, _: return Direction.NORTH


static func Direction_rotate270(my_dir: Direction) -> Direction:
	assert(Direction_is_valid(my_dir))
	match my_dir:
		Direction.NORTH  : return Direction.WEST
		Direction.SOUTH  : return Direction.EAST
		Direction.EAST   : return Direction.NORTH
		Direction.WEST, _: return Direction.SOUTH


func init(world_: WorldPanel, tick_type_: TickType, work_rate: int, gloc: Vector2i, 
	dims_: Vector2i
) -> void:
	self.world = world_
	self.tick_type = tick_type_
	self.rate = work_rate
	self.grid_loc = gloc
	self.dims = dims_

	position = world.grid_to_pos(gloc)

	sprite.hide()
	# Scale down to 1x1 by dividing by 128, then scale that to dims*cell_width.
	# sprite.apply_scale((world_.cell_width/128.0) * dims_)
	# sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	
	assert(Rect2i(Vector2i.ZERO, world.dims - gloc).has_point(dims))
	for y in dims_.y:
		for x in dims_.x:
			var my_loc := gloc + Vector2i(x, y)
			assert(!world.has_tile(my_loc))
			world.install_tile(WorldPanel.TlSolid.new(world, my_loc))
	
	build_tiles()
	assert(!input_slots.is_empty() || !output_slots.is_empty())


## Used to specify:
## what tiles the unit will occupy, 
## where the inputs and outputs are and their directions.
## The tiles should be built as if the unit is facing east.
@abstract func build_tiles() -> void


## Used to add commands to the queue using `pend_cmd`.
## Checks like `is_work_tick`, `has_pending_cmds` and `is_is_just_awaiting_out_sliding_anim` are
## used quite often here.
@abstract func pend_new_commands() -> void


## Called everytime on the tick pending commands queue size goes to zero
func on_finishing_all_pending_cmds() -> void:
	pass


func has_tile(gloc: Vector2i) -> bool:
	return Rect2i(grid_loc, dims).has_point(gloc)


func get_tile(gloc: Vector2i) -> WorldPanel.Tile:
	return world.get_tile(grid_loc + gloc)


## `gloc` is in world-space not unit-space.
func is_within(gloc: Vector2i) -> bool:
	return Rect2(grid_loc, dims).has_point(gloc)


## Returns true if the machine is expected to operate during this tick, `rate` determines how often
## this function returns true, ex. when `rate` is 1, it returns true every single tick.
func is_work_tick() -> bool:
	return _count > 0 && _count % rate == 0


## Returns true if the command queue still has commands to be executed. You usually pend all the 
## commands you want executed at once when the queue is empty, then wait until the whole sequence
## is executed.
func has_pending_cmds() -> bool:
	return !_pending_cmds.is_empty()


## Returns true if we are waiting for the animation of a slide command to end, that slide command
## is on one of the outputs of the unit.
## In this case, we can often just pend new commands before the end of the animation on the next
## tick.
## If there are no pending commands, it will return false!
func is_just_awaiting_out_sliding_anim() -> bool:
	# The cloner has multiple outputs, we need to account for that.
	return (
		!_pending_cmds.is_empty() &&
		_pending_cmds.all(func(x): return x is CmdSlide) && 
		_pending_cmds.all(func(x: CmdSlide): return x.is_awaiting_anim())
	)


func set_dir(new_dir: Direction) -> void:
	while dir != new_dir:
		rotate_90()


func rotate_90() -> void:	
	# All asserts
	if dims.x > dims.y:
		for y in range(dims.y, dims.x):
			for x in dims.y:
				assert(!world.has_tile(grid_loc + Vector2i(x, y)))
	elif dims.y > dims.x:
		for y in dims.x:
			for x in range(dims.x, dims.y):
				assert(!world.has_tile(grid_loc + Vector2i(x, y)))

	# Collect all the tiles and remove them from the world.
	var all_tiles: Array[WorldPanel.Tile] = []
	for y in dims.y:
		for x in dims.x:
			all_tiles.push_back(world.extract_tile(grid_loc + Vector2i(x, y)))

	# (new.x, new.y) = (max_y - old.y, old.x)
	var max_y := dims.y - 1
	for i in all_tiles.size():
		var my_tile := all_tiles[i]
		my_tile.rotate90()
		
		var x := i % dims.x
		var y := i / dims.x
		my_tile.set_grid_loc(grid_loc + Vector2i(max_y - y, x))
		world.install_tile(my_tile)

	dims = Vector2i(dims.y, dims.x)
	dir = Unit.Direction_rotate90(dir)


## Returns the tile adjusted.
func add_input(in_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.TlInput:
	assert(is_within(grid_loc + in_gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == in_gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == in_gloc))
	assert(get_tile(in_gloc) is WorldPanel.TlSolid)
	# Inputs must be facing out of the unit	
	assert(!is_within(grid_loc + in_gloc + Unit.Direction_to_grid(dir_)))
	
	var new_tile := WorldPanel.TlInput.new(world, self.grid_loc + in_gloc, dir_)
	world.override_tile(new_tile)
	input_slots.push_back(InputSlot.new(new_tile))
	return new_tile


## Returns the tile adjusted.
func add_output(out_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.TlOutput:
	assert(is_within(grid_loc + out_gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == out_gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == out_gloc))
	assert(get_tile(out_gloc) is WorldPanel.TlSolid)
	# Output must be facing out of the unit	
	assert(!is_within(grid_loc + out_gloc + Unit.Direction_to_grid(dir_)))
	
	var new_tile := WorldPanel.TlOutput.new(world, self.grid_loc + out_gloc, dir_)
	world.override_tile(new_tile)
	output_slots.push_back(OutputSlot.new(new_tile))
	return new_tile


## Returns the tile adjusted.
## The direction is for the input not output, the output is gonna be automatically set to the 
## reverse direction.
func add_io(io_gloc: Vector2i, out_dir: Unit.Direction, in_dir: Unit.Direction) -> WorldPanel.Tile:
	assert(is_within(grid_loc + io_gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == io_gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == io_gloc))
	assert(get_tile(io_gloc) is WorldPanel.TlSolid)
	# Both input and output should be facing out of the unit.
	assert(!is_within(grid_loc + io_gloc + Unit.Direction_to_grid(out_dir)))
	assert(!is_within(grid_loc + io_gloc + Unit.Direction_to_grid(in_dir)))

	var my_tile := WorldPanel.TlIO.new(world, self.grid_loc + io_gloc, out_dir, in_dir)
	world.override_tile(my_tile)

	input_slots.push_back(InputSlot.new(my_tile))
	output_slots.push_back(OutputSlot.new(my_tile))
	return my_tile


## Returns the tile adjusted.
## The direction is for the input not output, the output is gonna be automatically set to the 
## reverse direction.
func add_slider(sl_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.Tile:
	assert(is_within(grid_loc + sl_gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == sl_gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == sl_gloc))
	assert(get_tile(sl_gloc) is WorldPanel.TlSolid)
	# Both input and output should be facing out of the unit.
	assert(!is_within(grid_loc + sl_gloc + Unit.Direction_to_grid(dir_)))
	assert(!is_within(grid_loc + sl_gloc + Unit.Direction_to_grid(Unit.Direction_inv(dir_))))
	assert(!is_within(grid_loc + sl_gloc + Unit.Direction_to_grid(Unit.Direction_rotate90(dir_))))
	assert(!is_within(grid_loc + sl_gloc + Unit.Direction_to_grid(Unit.Direction_rotate270(dir_))))

	var my_tile := WorldPanel.TlSlider.new(world, self.grid_loc + sl_gloc, dir_)
	world.override_tile(my_tile)

	input_slots.push_back(InputSlot.new(my_tile))
	output_slots.push_back(OutputSlot.new(my_tile))
	return my_tile


## This HAS to be called before `pend_new_commands`, because it increments the tick counter.
func preprocess_tick() -> void:
	if !_pending_cmds.is_empty():
		for cmd in _pending_cmds:
			cmd.count_this_tick()
		
		# `is_done` depends on the loop just above.
		_pending_cmds = _pending_cmds.filter(func(x: Command): return !x.is_done())
		if _pending_cmds.is_empty():
			on_finishing_all_pending_cmds()
	
	match tick_type:
		TickType.STEADY:    
			_count += 1
		
		TickType.ON_DEMAND:
			if input_slots.all(func(x: InputSlot): return x.has_item()):
				_count += 1
			else:
				_count = 0
		
		_: assert(false)


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


## If an item is already there, returns true, otherwise checks for one, 
## if it finds one it assigns it to the slot and returns true, 
## if it doesn't find one it returns false
func check_for_item(slot_index: int) -> bool:
	assert(slot_index >= 0 && slot_index < input_slots.size())
	
	var slot := input_slots[slot_index]
	if slot.is_full(): # We have an item, no work needs to be done
		print("already have an item")
		return true
	
	# We don't have an item, so check if one was fed to the slot, and assign if so
	slot.item = world.get_item(slot.grid_loc)
	return slot.is_full()


## Adds a command to the queue, when this function is called outside of `pend_new_cmds` the 
## behaviour is undefined.
## I wanted to add a flag and stuff, but meh...
func pend_cmd(cmd: Command) -> void:
	_pending_cmds.push_back(cmd)


func do_per_frame(dt: float) -> void:
	for cmd in _pending_cmds:
		cmd.do_per_frame(dt)


func handle_cmd_tick() -> void:
	for cmd in _pending_cmds:
		cmd.on_tick()


func reset() -> void:
	_count = 0
	_pending_cmds.clear()


class Slot:
	var _tile: WorldPanel.Tile

	func _init(tile_: WorldPanel.Tile) -> void:
		self._tile = tile_


	func has_item() -> bool:
		return _tile.has_item()


	func extract_item(is_maybe_null: bool) -> Item:
		return _tile.extract_item(is_maybe_null)


	func destroy_item(is_maybe_null: bool) -> void:
		return _tile.destroy_item(is_maybe_null)


class InputSlot extends Slot:
	pass


class OutputSlot extends Slot:
	pass
