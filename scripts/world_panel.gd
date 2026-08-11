class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const UnitSliderScene := preload("res://scenes/unit_slider.tscn")
const UnitSupplierScene := preload("res://scenes/unit_supplier.tscn")
const UnitUpdaterScene := preload("res://scenes/unit_updater.tscn")

var elems: Array[Unit] = []
var dims := Vector2i(5, 5)
var cell_width: float = 100.0

## Simulation
var tick_millis: float = 500.0
var tick_timer: Timer
var tick_count: int = 0 # Number of ticks since the start.

var _units: Array[Unit] = []
var _supps: Array[UnitSupplier] = []
var _cmds: Array[Command] = []
var _tiles: Array[Tile] = []


func get_tile(gloc: Vector2i) -> Tile:
	return _tiles[gloc.y * dims.x + gloc.x]


func get_item(gloc: Vector2i, is_maybe_null: bool = false) -> Item:
	assert(is_maybe_null || has_item(gloc))
	return get_tile(gloc).item


func get_item_or_null(gloc: Vector2i) -> Item:
	return get_tile(gloc).item


func has_item(gloc: Vector2i) -> bool:
	assert(is_within_bounds(gloc))
	return get_tile(gloc).item != null


func get_unit(gloc: Vector2i) -> Unit:
	for u in _units:
		if u.grid_loc == gloc:
			return u
	return null


func get_tick_elapsed_millis() -> float:
	return tick_millis - tick_timer.time_left*1000


func is_within_bounds(gloc: Vector2i) -> bool:
	return Rect2i(Vector2i(), dims).has_point(gloc)


func grid_to_pos(gloc: Vector2i) -> Vector2:
	return cell_width * gloc
	# return cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5)


func pos_to_grid(pos: Vector2) -> Vector2i:
	return Vector2i(pos / cell_width)


func item_can_go_to(gloc: Vector2i) -> bool:
	return is_within_bounds(gloc) && get_tile(gloc).is_free()


func item_can_enter_from_dir(dir: Unit.Direction) -> bool:
	return false


func add_item(gloc: Vector2i, value: int) -> void:
	assert(!has_item(gloc))
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	new_item.setup(self, gloc, value)
	get_tile(gloc).item = new_item


func remove_item(gloc: Vector2i, possible_null: bool = false) -> void:
	assert(possible_null || get_item(gloc) != null)
	var my_tile := get_tile(gloc)
	my_tile.item.queue_free()
	my_tile.item = null


func clone_item(g_from: Vector2i, g_to: Vector2i, is_override: bool = false) -> void:
	assert(get_item(g_from) != null)
	assert(is_override || get_item(g_to) == null)
	add_item(g_to, get_item(g_from).value)


func teleport_item(g_from: Vector2i, g_to: Vector2i) -> void:
	assert(get_item(g_from) != null)
	assert(get_item(g_to) == null)
	var src_tile := get_tile(g_from)
	get_tile(g_to).item = src_tile.item
	src_tile.item = null


func add_cmd(new_cmd: Command) -> void:
	_cmds.push_back(new_cmd)


func do_per_frame(dt: float) -> void:
	for cmd in _cmds:
		cmd.do_per_frame(dt)


func on_tick() -> void:
	var old_size := _cmds.size()
	for u in _units: # Add commands
		u.preprocess_tick()
		u.on_tick()

	for i in _cmds.size():
		var my_cmd := _cmds[i]
		my_cmd.preprocess_tick()
		if i >= old_size: # Commands added this very tick by the last loop
			my_cmd.on_spawn()
		else: # Old commands
			my_cmd.on_tick()

	# Remove the finished commands.
	_cmds = _cmds.filter(func(x: Command): return !x.is_done())

	tick_count += 1
	queue_redraw()


func clean_up() -> void:
	for u in _units:
		u.reset()
	
	for tile in _tiles:
		if tile.item != null:
			remove_child(tile.item)
	
	tick_count = 0
	_cmds.clear()


func _ready() -> void:
	size = cell_width * dims

	tick_timer = Timer.new()
	tick_timer.one_shot = true
	add_child(tick_timer)

	for i in dims.x * dims.y:
		_tiles.push_back(Tile.new())

	_place_some_units()


func _place_some_units() -> void:
	# _place_supplier(Vector2i(2, 3), Unit.Direction.EAST, 2, [1, 2, 3, 4])
	# _place_bus(Vector2i(3, 3), Unit.Direction.EAST)
	# _place_updater(Vector2i(4, 3), Unit.Direction.EAST, 4, UnitUpdater.UpdateType.NEGATE)
	# _place_bus(Vector2i(5, 3), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(5, 4), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(5, 5), Unit.Direction.WEST)
	# _place_bus(Vector2i(4, 5), Unit.Direction.WEST)
	# _place_bus(Vector2i(3, 5), Unit.Direction.NORTH)

	_place_supplier(Vector2i(2, 3), Unit.Direction.EAST, 2, [1])
	_place_bus(Vector2i(3, 3), Unit.Direction.EAST)
	# _place_bus(Vector2i(4, 3), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(4, 4), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(4, 5), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(4, 6), Unit.Direction.SOUTH)
	# _place_bus(Vector2i(4, 7), Unit.Direction.SOUTH)
	
	# _place_supplier(Vector2i(6, 3), Unit.Direction.WEST, 2, [1, 2])
	# _place_bus(Vector2i(5, 3), Unit.Direction.WEST)


func _place_supplier(loc: Vector2i, dir: Unit.Direction, rate: int, seq: Array[int]) -> void:
	var my_supp := UnitSupplierScene.instantiate()
	_units.push_back(my_supp)
	_supps.push_back(my_supp)
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


func _draw() -> void:
	draw_rect(Rect2(Vector2(), size), Color(0, 0.1, 0) if tick_count % 2 == 1 else Color(0, 0.11, 0))

	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)


class Tile:
	enum TileState {
		FREE,
		SOLID,
		SOLID_NEXT_TICK,
	}

	const NORTH_WALL_ID := 0
	const EAST_WALL_ID  := 1
	const SOUTH_WALL_ID := 2
	const WEST_WALL_ID  := 3

	var walls: Array[bool] = [false, false, false, false]
	var item: Item = null
	
	var _state: TileState = TileState.FREE

	func is_free() -> bool:
		return _state == TileState.FREE

	func is_solid() -> bool:
		return _state == TileState.SOLID || _state == TileState.SOLID_NEXT_TICK

	func is_solid_next_tick() -> bool:
		return _state == TileState.SOLID_NEXT_TICK
	
	func make_free() -> void:
		_state = TileState.FREE

	func make_solid() -> void:
		_state = TileState.SOLID

	func make_solid_next_tick() -> void:
		_state = TileState.SOLID_NEXT_TICK
	
