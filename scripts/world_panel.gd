class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const BusScene := preload("res://scenes/bus.tscn")
const SupplierScene := preload("res://scenes/supplier.tscn")

var elems: Array[Unit] = []
var dims := Vector2i(15, 10)
var cell_width: float = 100.0

## Simulation
var tick_millis: float = 1000.0
var tick_timer: Timer

var _units: Array[Unit] = []
var _items: Array[Item] = []
var _supps: Array[Supplier] = []
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


func grid_to_pos(gloc: Vector2i) -> Vector2:
	return cell_width * gloc
	# return cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5)


func pos_to_grid(pos: Vector2) -> Vector2i:
	return Vector2i(pos / cell_width)


func add_item(gloc: Vector2i, value: int) -> void:
	assert(get_item(gloc) == null)
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	new_item.setup(self, gloc, value)
	_items.push_back(new_item)


func add_cmd(new_op: Command) -> void:
	_cmds.push_back(new_op)


func do_per_frame(dt: float) -> void:
	for cmd in _cmds:
		cmd.do_per_frame(self)


func _ready() -> void:
	size = cell_width * dims

	tick_timer = Timer.new()
	tick_timer.one_shot = true
	add_child(tick_timer)

	_place_some_units()


func _place_some_units() -> void:
	var my_supp := SupplierScene.instantiate()
	_units.push_back(my_supp)
	_supps.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, Vector2i(0, 2), 2, [1, 2, 3, 4])

	for x in range(1, 5):
		var my_bus := BusScene.instantiate()
		_units.push_back(my_bus)
		add_child(my_bus)
		my_bus.setup(self, Vector2i(x,2), Unit.Direction.EAST)


func on_tick() -> void:
	var old_size := _cmds.size()
	for u in _units: # Add commands
		u.on_tick(self)

	# Commands added this very tick by the last loop
	for i in range(old_size, _cmds.size()):
		_cmds[i].on_spawn_tick(self)
	
	# Old commands
	for i in old_size: # Execute a tick of the commands
		_cmds[i].on_tick(self)

	# Remove the finished commands.
	_cmds = _cmds.filter(func(x): return !x.is_done)


func clean_up() -> void:
	_cmds.clear()
	for it in _items:
		remove_child(it)
	_items.clear()


func _draw() -> void:
	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)
