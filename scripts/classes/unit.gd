@abstract class_name Unit extends Node2D

## `inv_dir` relys on the order of these.
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
var tiles: Array[WorldPanel.Tile] = []
var dims: Vector2i

## Keeps track of the `rate` every tick.
var _count: int = 0

## Commands that need to finish for the unit to keep operating again
var _pending_cmds: Array[Command] = []


static func dir_to_grid(my_dir: Direction) -> Vector2i:
	match my_dir:
		Direction.NORTH: return Vector2i(0, -1)
		Direction.SOUTH: return Vector2i(0, +1)
		Direction.EAST : return Vector2i(+1, 0)
		Direction.WEST : return Vector2i(-1, 0)
	return -INF * Vector2i()


static func inv_dir(my_dir: Direction) -> Direction:
	return 3 - my_dir as Direction


static func rotate_dir_90(my_dir: Direction) -> Direction:
	match my_dir:
		Direction.NORTH: return Direction.EAST
		Direction.SOUTH: return Direction.WEST
		Direction.EAST : return Direction.SOUTH
		Direction.WEST : return Direction.NORTH
	return Direction.EAST


static func rotate_dir_neg_90(my_dir: Direction) -> Direction:
	match my_dir:
		Direction.NORTH: return Direction.WEST
		Direction.SOUTH: return Direction.EAST
		Direction.EAST : return Direction.NORTH
		Direction.WEST : return Direction.SOUTH
	return Direction.EAST


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
	
	assert(0 < dims_.x && dims_.x + gloc.x < world.dims.x)
	assert(0 < dims_.y && dims_.y + gloc.y < world.dims.y)
	for y in dims_.y:
		for x in dims_.x:
			var my_tile := world.get_tile(gloc + Vector2i(x, y))
			my_tile.make_solid()
			tiles.push_back(my_tile)
	
	build_tiles()
	assert(!input_slots.is_empty() || !output_slots.is_empty())


## Used to specify:
## what tiles the unit will occupy, 
## where the inputs and outputs are and their directions.
## The tiles should be built as if the unit is facing east.
@abstract func build_tiles() -> void
@abstract func pend_new_commands() -> void


func has_tile(gloc: Vector2i) -> bool:
	return Rect2i(grid_loc, dims).has_point(gloc)


func get_tile(gloc: Vector2i) -> WorldPanel.Tile:
	return world.get_tile(grid_loc + gloc)


## `gloc` is in world-space not unit-space.
func is_within(gloc: Vector2i) -> bool:
	return Rect2(grid_loc, dims).has_point(gloc)


func is_work_tick() -> bool:
	return _count > 0 && _count % rate == 0


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
	dir = new_dir
	assert(tiles.size() == 1)
	get_tile(Vector2i.ZERO).set_dir(new_dir, true)
	# match dir:
	# 	Direction.EAST:  sprite.rotation = PI * 0.0
	# 	Direction.SOUTH: sprite.rotation = PI * 0.5
	# 	Direction.WEST:  sprite.rotation = PI * 1.0
	# 	Direction.NORTH: sprite.rotation = PI * 1.5


func add_input(gloc: Vector2i, dir_: Unit.Direction) -> void:
	assert(is_within(grid_loc + gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == gloc))

	var my_tile := get_tile(gloc)
	assert(my_tile.is_solid())

	var adj_loc := gloc + Unit.dir_to_grid(dir_)
	assert(!is_within(grid_loc + adj_loc))

	my_tile.make_input()
	my_tile.set_dir(dir_)
	input_slots.push_back(InputSlot.new(self, gloc))


func add_output(gloc: Vector2i, dir_: Unit.Direction) -> void:
	assert(is_within(grid_loc + gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == gloc))

	var my_tile := get_tile(gloc)
	assert(my_tile.is_solid())

	var adj_loc := gloc + Unit.dir_to_grid(dir_)
	assert(!is_within(grid_loc + adj_loc))

	my_tile.make_output()
	my_tile.set_dir(dir_)
	output_slots.push_back(OutputSlot.new(self, gloc))


## The direction is for the input not output, the output is gonna be automatically set to the 
## reverse direction.
func add_io(gloc: Vector2i, dir_: Unit.Direction) -> void:
	assert(is_within(grid_loc + gloc))
	assert(!input_slots.any(func(x): return x.grid_loc == gloc))
	assert(!output_slots.any(func(x): return x.grid_loc == gloc))

	var my_tile := get_tile(gloc)
	assert(my_tile.is_solid())

	var adj_loc1 := gloc + Unit.dir_to_grid(dir_)
	assert(!is_within(grid_loc + adj_loc1))
	
	var adj_loc2 := gloc + Unit.dir_to_grid(Unit.inv_dir(dir_))
	assert(!is_within(grid_loc + adj_loc2))

	my_tile.make_io()
	my_tile.set_dir(dir_, true)
	input_slots.push_back(InputSlot.new(self, gloc))
	output_slots.push_back(OutputSlot.new(self, gloc))


## This HAS to be called before `pend_new_commands`, because it increments the tick counter.
func preprocess_tick() -> void:
	for cmd in _pending_cmds:
		cmd.count_this_tick()
	
	# `is_done` depends on the loop just above.
	_pending_cmds = _pending_cmds.filter(func(x: Command): return !x.is_done())
	
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
	var grid_loc: Vector2i
	var _parent: Unit


	func _init(parent: Unit, gloc: Vector2i) -> void:
		self._parent = parent
		self.grid_loc = gloc


	func has_item() -> bool:
		return _parent.get_tile(grid_loc).has_item()


	func extract_item(is_maybe_null: bool) -> Item:
		return _parent.get_tile(grid_loc).extract_item(is_maybe_null)


	func destroy_item(is_maybe_null: bool) -> void:
		return _parent.get_tile(grid_loc).destroy_item(is_maybe_null)


class InputSlot extends Slot:
	func _init(parent: Unit, gloc: Vector2i) -> void:
		super(parent, gloc)
		assert(parent.get_tile(gloc).is_input())


class OutputSlot extends Slot:
	func _init(parent: Unit, gloc: Vector2i) -> void:
		super(parent, gloc)
		assert(parent.get_tile(gloc).is_output())
