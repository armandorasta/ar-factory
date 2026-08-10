class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const UnitBusScene := preload("res://scenes/unit_bus.tscn")
const UnitSupplierScene := preload("res://scenes/unit_supplier.tscn")
const UnitUpdaterScene := preload("res://scenes/unit_updater.tscn")

var elems: Array[Unit] = []
var dims := Vector2i(15, 10)
var cell_width: float = 100.0

## Simulation
var tick_millis: float = 500.0
var tick_timer: Timer
var tick_count: int = 0 # Number of ticks since the start.

var _units: Array[Unit] = []
var _items: Array[Item] = []
var _supps: Array[UnitSupplier] = []
var _cmds: Array[Command] = []


func get_item(gloc: Vector2i) -> Item:
	for it in _items:
		if it.grid_loc == gloc:
			return it
	return null


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


func add_item(gloc: Vector2i, value: int) -> void:
	assert(get_item(gloc) == null)
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	_items.push_back(new_item)
	new_item.setup(self, gloc, value)


func add_cmd(new_cmd: Command) -> void:
	_cmds.push_back(new_cmd)


func do_per_frame(dt: float) -> void:
	for cmd in _cmds:
		cmd.do_per_frame(self)


func on_tick() -> void:
	var old_size := _cmds.size()
	for u in _units: # Add commands
		u.on_tick()
		u.increment_count()

	# Commands added this very tick by the last loop
	for i in range(old_size, _cmds.size()):
		_cmds[i].on_spawn(self)
	
	# Old commands
	for i in old_size: # Execute a tick of the commands
		_cmds[i].on_tick(self)

	# Remove the finished commands.
	_cmds = _cmds.filter(func(x: Command): return !x.is_done())
	for c in _cmds:
		c.increment_count()

	tick_count += 1
	queue_redraw()


func clean_up() -> void:
	for u in _units:
		u.reset()
	
	for it in _items:
		remove_child(it)
	
	tick_count = 0
	_items.clear()
	_cmds.clear()


func _ready() -> void:
	size = cell_width * dims

	tick_timer = Timer.new()
	tick_timer.one_shot = true
	add_child(tick_timer)

	_place_some_units()


func _process(dt: float) -> void:
	# var mloc := get_local_mouse_position()
	# var gloc := pos_to_grid(mloc)
	# var norm_loc := grid_to_pos(gloc)
	# print("%s -> %s -> %s" % [mloc, gloc, norm_loc])
	pass


func _place_some_units() -> void:
	_place_supplier(Vector2i(2, 3), Unit.Direction.EAST, 2, [1, 2, 3, 4])
	_place_bus(Vector2i(3, 3), Unit.Direction.EAST)
	# _place_bus(Vector2i(4, 3), Unit.Direction.EAST)
	_place_updater(Vector2i(4, 3), Unit.Direction.EAST, 4, UnitUpdater.UpdateType.NEGATE)
	_place_bus(Vector2i(5, 3), Unit.Direction.SOUTH)
	_place_bus(Vector2i(5, 4), Unit.Direction.SOUTH)
	_place_bus(Vector2i(5, 5), Unit.Direction.WEST)
	_place_bus(Vector2i(4, 5), Unit.Direction.WEST)
	_place_bus(Vector2i(3, 5), Unit.Direction.NORTH)


func _place_supplier(loc: Vector2i, dir: Unit.Direction, rate: int, seq: Array[int]) -> void:
	var my_supp := UnitSupplierScene.instantiate()
	_units.push_back(my_supp)
	_supps.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, loc, dir, rate, seq)


func _place_bus(loc: Vector2i, dir: Unit.Direction) -> void:
	var my_bus := UnitBusScene.instantiate()
	_units.push_back(my_bus)
	add_child(my_bus)
	my_bus.setup(self, loc, dir, 2)


func _place_updater(loc: Vector2i, dir: Unit.Direction, rate: int, up_t: 
	UnitUpdater.UpdateType) -> void:
	var doub := UnitUpdaterScene.instantiate()
	_units.push_back(doub)
	add_child(doub)
	doub.setup(self, loc, dir, rate, up_t)


func _draw() -> void:
	draw_rect(Rect2(Vector2(), size), Color(0, 0.1, 0) if tick_count % 2 == 1 else Color(0, 0.11, 0))

	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)
