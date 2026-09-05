@abstract class_name Unit extends Node2D

## `Direction_inv` relys on the order of these.
enum Direction {
	NORTH = 0, 
	EAST  = 1,
	SOUTH = 2, 
	WEST  = 3,

	LAST = WEST,
}

const GRID_NORTH := Vector2i(0, -1)
const GRID_SOUTH := Vector2i(0, +1)
const GRID_EAST  := Vector2i(+1, 0)
const GRID_WEST  := Vector2i(-1, 0)


enum TickType {
	STEADY,    # Always counts ticks, even when no item is fed, like sliders.
	ON_DEMAND, # Counts ticks only when all item slots are filled, like updaters.
}

@onready var sprite: Sprite2D = $Sprite2D

var world: WorldPanel
var grid_loc: Vector2i
var rate: int

var _dir: Direction = Direction.EAST
var _dims: Vector2i
var _tick_type: TickType

## Keeps track of the `rate` every tick.
var _count: int = 0

## Commands that need to finish for the unit to keep operating again
var _pending_cmds: Array[Command] = []

## These are checked by the `ON_DEMAND` units
var _holding_tiles: Array[WorldPanel.TlHolder] = []


static func Direction_is_valid(my_dir: Direction) -> bool:
	return 0 <= my_dir && my_dir <= Direction.LAST


static func Direction_to_grid(my_dir: Direction) -> Vector2i:
	assert(Direction_is_valid(my_dir))
	match my_dir:
		Direction.NORTH  : return GRID_NORTH
		Direction.SOUTH  : return GRID_SOUTH
		Direction.EAST   : return GRID_EAST
		Direction.WEST, _: return GRID_WEST


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
	self._tick_type = tick_type_
	self.rate = work_rate
	self.grid_loc = gloc
	self._dims = dims_

	position = world.grid_to_pos(gloc)

	sprite.hide()
	# Scale down to 1x1 by dividing by 128, then scale that to _dims*cell_width.
	# sprite.apply_scale((world_.cell_width/128.0) * dims_)
	# sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	
	assert(Rect2i(Vector2i.ZERO, world.dims - gloc).has_point(_dims))
	for y in dims_.y:
		for x in dims_.x:
			var my_loc := gloc + Vector2i(x, y)
			assert(!world.has_tile(my_loc))
			world.install_tile(WorldPanel.TlSolid.new(my_loc))
	
	build_tiles()


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
	return Rect2i(grid_loc, _dims).has_point(gloc)


func get_tile(gloc: Vector2i) -> WorldPanel.Tile:
	return world.get_tile(grid_loc + gloc)


## `gloc` is in world-space not unit-space.
func is_within(gloc: Vector2i) -> bool:
	return Rect2(grid_loc, _dims).has_point(gloc)


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
	while _dir != new_dir:
		rotate_90()


func rotate_90() -> void:	
	# All asserts
	if _dims.x > _dims.y:
		for y in range(_dims.y, _dims.x):
			for x in _dims.y:
				assert(!world.has_tile(grid_loc + Vector2i(x, y)))
	elif _dims.y > _dims.x:
		for y in _dims.x:
			for x in range(_dims.x, _dims.y):
				assert(!world.has_tile(grid_loc + Vector2i(x, y)))

	# Collect all the tiles and remove them from the world.
	var all_tiles: Array[WorldPanel.Tile] = []
	for y in _dims.y:
		for x in _dims.x:
			all_tiles.push_back(world.extract_tile(grid_loc + Vector2i(x, y)))

	# (new.x, new.y) = (max_y - old.y, old.x)
	var max_y := _dims.y - 1
	for i in all_tiles.size():
		var my_tile := all_tiles[i]
		my_tile.rotate90()
		
		var x := i % _dims.x
		var y := i / _dims.x
		my_tile.set_grid_loc_unchecked(grid_loc + Vector2i(max_y - y, x))
		world.install_tile(my_tile)

	_dims = Vector2i(_dims.y, _dims.x)
	_dir = Unit.Direction_rotate90(_dir)


## Returns the tile adjusted.
## Input tiles let items slide in from a specified direction.
func add_input(in_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.TlInput:
	assert(is_within(grid_loc + in_gloc))
	assert(get_tile(in_gloc) is WorldPanel.TlSolid)
	# Inputs must be facing out of the unit	
	assert(!is_within(grid_loc + in_gloc + Unit.Direction_to_grid(dir_)))
	
	var new_tile := WorldPanel.TlInput.new(self.grid_loc + in_gloc, dir_)
	world.install_tile(new_tile, true)
	_holding_tiles.push_back(new_tile)
	return new_tile


## Returns the tile adjusted.
## Output tiles are meant to generate tiles and slide them out in a specified direction.
func add_output(out_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.TlOutput:
	assert(is_within(grid_loc + out_gloc))
	assert(get_tile(out_gloc) is WorldPanel.TlSolid)
	# Output must be facing out of the unit	
	assert(!is_within(grid_loc + out_gloc + Unit.Direction_to_grid(dir_)))
	
	var new_tile := WorldPanel.TlOutput.new(self.grid_loc + out_gloc, dir_)
	world.install_tile(new_tile, true)
	return new_tile


## Returns the tile adjusted.
## IO tiles are an input tile and output tile combined in one, they let items slide in in one 
## direction, and let them slide out in another, the 2 directions may not coincide.
func add_io(io_gloc: Vector2i, out_dir: Unit.Direction, in_dir: Unit.Direction) -> WorldPanel.Tile:
	assert(out_dir != in_dir)
	assert(is_within(grid_loc + io_gloc))
	assert(get_tile(io_gloc) is WorldPanel.TlSolid)
	# Both input and output should be facing out of the unit.
	assert(!is_within(grid_loc + io_gloc + Unit.Direction_to_grid(out_dir)))
	assert(!is_within(grid_loc + io_gloc + Unit.Direction_to_grid(in_dir)))

	var new_tile := WorldPanel.TlIO.new(self.grid_loc + io_gloc, out_dir, in_dir)
	world.install_tile(new_tile, true)
	_holding_tiles.push_back(new_tile)
	return new_tile


## Returns the tile adjusted.
## Slider tiles let in items from 3 directions and are meant to let them out from one.
func add_slider(sl_gloc: Vector2i, dir_: Unit.Direction) -> WorldPanel.Tile:
	var net_gloc := self.grid_loc + sl_gloc
	assert(is_within(net_gloc))
	assert(get_tile(sl_gloc) is WorldPanel.TlSolid)
	# Output and at least one input must be facing out of the unit, otherwise just use input or output.
	assert(!is_within(net_gloc + Unit.Direction_to_grid(dir_)))
	assert(
		!is_within(net_gloc + Unit.Direction_to_grid(Unit.Direction_inv(dir_))) ||
		!is_within(net_gloc + Unit.Direction_to_grid(Unit.Direction_rotate90(dir_))) ||
		!is_within(net_gloc + Unit.Direction_to_grid(Unit.Direction_rotate270(dir_)))
	)

	var new_tile := WorldPanel.TlSlider.new(net_gloc, dir_)
	world.install_tile(new_tile, true)
	_holding_tiles.push_back(new_tile)
	return new_tile


## Returns the tile adjusted.
## Blackhole tiles allow in items from all directions.
func add_blackhole(black_gloc: Vector2i) -> WorldPanel.Tile:
	var net_gloc := self.grid_loc + black_gloc
	assert(is_within(net_gloc))
	assert(get_tile(black_gloc) is WorldPanel.TlSolid)
	# At least one direction must be facing outside
	assert(
		!is_within(net_gloc + Unit.GRID_NORTH) || 
		!is_within(net_gloc + Unit.GRID_SOUTH) ||
		!is_within(net_gloc + Unit.GRID_EAST)  || 
		!is_within(net_gloc + Unit.GRID_WEST)   )

	var new_tile := WorldPanel.TlBlackhole.new(net_gloc)
	world.install_tile(new_tile, true)
	_holding_tiles.push_back(new_tile)
	return new_tile


## This HAS to be called before `pend_new_commands`, because it increments the tick counter.
func preprocess_tick() -> void:
	if !_pending_cmds.is_empty():
		for cmd in _pending_cmds:
			cmd.count_this_tick()
		
		# `is_done` depends on the loop just above.
		_pending_cmds = _pending_cmds.filter(func(x: Command): return !x.is_done())
		if _pending_cmds.is_empty():
			on_finishing_all_pending_cmds()
	
	match _tick_type:
		TickType.STEADY:    
			_count += 1
		
		TickType.ON_DEMAND:
			if _holding_tiles.all(func(x): return x.has_item()):
				_count += 1
			else:
				_count = 0
		
		_: assert(false)


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


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
