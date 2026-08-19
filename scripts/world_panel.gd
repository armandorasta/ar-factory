class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const UnitSliderScene := preload("res://scenes/unit_slider.tscn")
const UnitSupplierScene := preload("res://scenes/unit_supplier.tscn")
const UnitUpdaterScene := preload("res://scenes/unit_updater.tscn")
const UnitBinScene := preload("res://scenes/unit_bin.tscn")
const UnitDemanderScene := preload("res://scenes/unit_demander.tscn")

var elems: Array[Unit] = []
var dims := Vector2i(5, 7)
var cell_width: float = 100.0

## Simulation
var default_tick_millis: float = 300.0
var tick_timer: Timer
var tick_count: int = 0 # Number of ticks since the start.
var _tick_millis: float

var blocked_slide_cmds: Array[CmdSlide] = []

var _units: Array[Unit] = []
var _cmds: Array[Command] = []
var _tiles: Array[Tile] = []


func get_tick_rate() -> float:
	return 1000.0 / _tick_millis


func get_tile(gloc: Vector2i) -> Tile:
	return _tiles[gloc.y * dims.x + gloc.x]


func get_unit(gloc: Vector2i) -> Unit:
	for u in _units:
		if u.grid_loc == gloc:
			return u
	return null


func get_tick_elapsed_millis() -> float:
	return _tick_millis - tick_timer.time_left*1000


func is_within_bounds(gloc: Vector2i) -> bool:
	return Rect2i(Vector2i(), dims).has_point(gloc)


func grid_to_pos(gloc: Vector2i) -> Vector2:
	return cell_width * gloc
	# return cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5)


func pos_to_grid(pos: Vector2) -> Vector2i:
	return Vector2i(pos / cell_width)

## Does not account for whether the tile is reserved or not.
func item_can_be_in(gloc: Vector2i) -> bool:
	if is_within_bounds(gloc): 
		return true
	if get_tile(gloc).is_free(): 
		return true
	return false


func item_can_slide(item_g_loc: Vector2i, item_dir: Unit.Direction) -> bool:
	var dest_tile := get_tile(item_g_loc + Unit.dir_to_grid(item_dir))
	return !dest_tile.is_reserved() && !dest_tile.has_wall(Unit.inv_dir(item_dir))
	

## Returns the item.
func add_item(gloc: Vector2i, value: int) -> Item:
	var my_tile := get_tile(gloc)
	assert(!my_tile.is_reserved()) # Might have to change this to `has_item`, but who knows.
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	new_item.setup(self, gloc, value)
	my_tile.set_item(new_item)
	return new_item


## Returns the clone, not the original
func clone_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	assert(is_maybe_null || get_tile(g_from).has_item())
	assert(is_override || !get_tile(g_to).has_item())
	return add_item(g_to, get_tile(g_from).get_item().value)


## Returns the teleported item.
func teleport_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	var src_tile := get_tile(g_from)
	var dest_tile := get_tile(g_to)
	assert(is_maybe_null || src_tile.has_item())
	assert(is_override || !dest_tile.has_item())
	dest_tile.set_item(src_tile.extract_item())
	return dest_tile.get_item()


func add_cmd(new_cmd: Command) -> void:
	_cmds.push_back(new_cmd)


func reserve_tile(gloc: Vector2i) -> void:
	return get_tile(gloc).set_reserved(true)


func do_per_frame(dt: float) -> void:
	for cmd in _cmds:
		cmd.do_per_frame(dt)


func on_tick() -> void:
	for tile in _tiles:
		if tile.has_item():
			tile.get_item().cant_move_this_tick = false

	for u in _units: # Preprocess all units first
		u.preprocess_tick()

	for u in _units: # Add commands
		u.on_tick()

	# _cmds.shuffle()
	for c in _cmds:
		c.on_tick()

	_handle_slide_cmd_overlapping()

	for c in _cmds:
		c.count_this_tick()
	
	# Remove the finished commands.
	_cmds = _cmds.filter(func(x: Command): return !x.is_done())		

	tick_count += 1
	queue_redraw()


func set_tick_rate(rate_per_second: float) -> void:
	_tick_millis = 1000.0 / rate_per_second


func reset_tick_rate() -> void:
	_tick_millis = default_tick_millis


func clean_up() -> void:
	for u in _units:
		u.reset()
	
	for tile in _tiles:
		tile.destroy_item(true)
	
	tick_count = 0
	_cmds.clear()
	blocked_slide_cmds.clear()


func _ready() -> void:
	size = cell_width * dims

	tick_timer = Timer.new()
	tick_timer.one_shot = true
	add_child(tick_timer)

	for i in dims.x * dims.y:
		_tiles.push_back(Tile.new(false))

	_place_some_units()


func _draw() -> void:
	draw_rect(Rect2(Vector2(), size), Color(0, 0.1, 0) if tick_count % 2 == 1 else Color(0, 0.11, 0))

	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)


func _place_some_units() -> void:
	_place_supplier(Vector2i(0, 3), Unit.Direction.EAST, 1, [1, 2, 3, 4, 5, 6, 7])
	_place_bus(Vector2i(1, 3), Unit.Direction.EAST)
	# _place_bus(Vector2i(2, 3), Unit.Direction.EAST)
	_place_updater(Vector2i(2, 3), Unit.Direction.EAST, 1, UnitUpdater.UpdateType.DOUBLE)
	_place_bus(Vector2i(3, 3), Unit.Direction.NORTH)
	_place_bus(Vector2i(3, 2), Unit.Direction.NORTH)
	_place_bus(Vector2i(3, 1), Unit.Direction.WEST)
	# _place_bus(Vector2i(2, 1), Unit.Direction.WEST)
	_place_updater(Vector2i(2, 1), Unit.Direction.WEST, 1, UnitUpdater.UpdateType.DOUBLE)
	_place_bus(Vector2i(1, 1), Unit.Direction.SOUTH)
	_place_bus(Vector2i(1, 2), Unit.Direction.SOUTH)
	

func _place_supplier(loc: Vector2i, dir: Unit.Direction, rate: int, seq: Array[int]) -> void:
	var my_supp := UnitSupplierScene.instantiate()
	_units.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, loc, rate, seq)
	my_supp.set_dir(dir)


func _place_bus(loc: Vector2i, dir: Unit.Direction) -> void:
	var my_bus := UnitSliderScene.instantiate()
	_units.push_back(my_bus)
	add_child(my_bus)
	my_bus.setup(self, loc, 1)
	my_bus.set_dir(dir)


func _place_updater(loc: Vector2i, dir: Unit.Direction, rate: int, up_t: 
	UnitUpdater.UpdateType
) -> void:
	var doub := UnitUpdaterScene.instantiate()
	_units.push_back(doub)
	add_child(doub)
	doub.setup(self, loc, rate, up_t)
	doub.set_dir(dir)


func _place_bin(loc: Vector2i) -> void:
	var bin := UnitBinScene.instantiate()
	_units.push_back(bin)
	add_child(bin)
	bin.setup(self, loc)

func _place_demander(loc: Vector2i, seq: Array[int]) -> void:
	var dem := UnitDemanderScene.instantiate()
	_units.push_back(dem)
	add_child(dem)
	dem.setup(self, loc, seq)


## Called after `on_tick` is called on every slide command.
func _handle_slide_cmd_overlapping() -> void:
	# Slide command overlapping.
	# If any update happens in a pass, end that pass and start over,
	# repeat until a full pass happens with no updates.
	while 1 + 1 == 2:
		var dirty_index := -1
		for i in blocked_slide_cmds.size():
			if blocked_slide_cmds[i].is_updated_this_pass():
				dirty_index = i
				break
		if dirty_index == -1: # None has updated? it's over.
			break
		# Something moved? can only move once per tick so bye-bye.
		blocked_slide_cmds.pop_at(dirty_index)
	blocked_slide_cmds.clear()


class Tile:
	enum TileType {
		FREE  = 0x0, # Anything allowed in from any direction.
		INPUT = 0x1, # Item allowed if it's going into the input direction.
		SOLID = 0x2, # No items allowed EVER.
	}

	const _NORTH_WALL_ID := 0
	const _EAST_WALL_ID  := 1
	const _SOUTH_WALL_ID := 2
	const _WEST_WALL_ID  := 3

	## Only `INPUT` tiles have walls.
	var _walls: Array[bool] = []
	var _type: TileType = TileType.FREE
	var _item: Item = null
	
	## The makes the tile inaccessible to items even if it's empty.
	var _is_reserved: bool = false


	func _init(is_solid_: bool) -> void:
		_type = TileType.SOLID if is_solid_ else TileType.FREE


	## Is the tile solid? if not are items not allowed there?
	func is_reserved() -> bool:
		return is_solid() || _is_reserved


	func is_free()  -> bool: return _type == TileType.FREE
	func is_input() -> bool: return _type == TileType.INPUT
	func is_solid() -> bool: return _type == TileType.SOLID

	func has_wall(dir_: Unit.Direction) -> bool:
		if is_solid():
			return true
		
		if is_free():
			return false
		
		match dir_:
			Unit.Direction.NORTH: return _walls[_NORTH_WALL_ID]
			Unit.Direction.EAST : return _walls[_EAST_WALL_ID]
			Unit.Direction.SOUTH: return _walls[_SOUTH_WALL_ID]
			Unit.Direction.WEST : return _walls[_WEST_WALL_ID]
			_: return false


	func has_item() -> bool:
		return _item != null

	func get_item(is_maybe_null: bool = false) -> Item:
		assert(is_maybe_null || has_item())
		return _item


	func make_free()  -> void: 
		_type = TileType.FREE
		_walls.clear()
	
	func make_input() -> void: 
		_type = TileType.INPUT
		_walls.resize(4)
	
	func make_solid() -> void: 
		_type = TileType.SOLID
		_walls.clear()


	func set_wall(dir_: Unit.Direction, to_what: bool) -> void:
		assert(is_input())
		match dir_:
			Unit.Direction.NORTH: _walls[_NORTH_WALL_ID] = to_what
			Unit.Direction.EAST : _walls[_EAST_WALL_ID]  = to_what
			Unit.Direction.SOUTH: _walls[_SOUTH_WALL_ID] = to_what
			Unit.Direction.WEST : _walls[_WEST_WALL_ID]  = to_what


	func set_item(new_item: Item, is_override: bool = false) -> void:
		assert(is_free() || is_input())
		assert(is_override || !has_item())
		destroy_item(true)

		_item = new_item
		_is_reserved = true


	func destroy_item(is_maybe_null: bool = false) -> void:
		assert(is_maybe_null || has_item())
		if _item != null:
			_item.queue_free()
			_item = null
		_is_reserved = false


	## Same as `destroy_item` except it returns the item instead of destroying it.
	func extract_item(is_maybe_null: bool = false) -> Item:
		assert(is_maybe_null || has_item())
		var my_item := _item
		_item = null
		_is_reserved = false
		return my_item

	
	func set_reserved(to_what: bool) -> void:
		_is_reserved = to_what
