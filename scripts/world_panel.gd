class_name WorldPanel extends Panel

const BusScene := preload("res://scenes/bus.tscn")
const SupplierScene := preload("res://scenes/supplier.tscn")


@onready var cam: Camera2D = $Cam

var elems: Array[Unit] = []
var dims := Vector2i(15, 10)
var cell_width: float = 100.0

var _units: Array[Unit] = []
var _supps: Array[Supplier] = []

# Simulation params
var _tick_millis: float = 1000.0
var _timer: Timer = Timer.new()
var _cmds: Array[Command] = []


func get_unit(coord: Vector2i) -> Unit:
	if coord.x < 0 || coord.x >= dims.x || coord.y < 0 || coord.y >= dims.y:
		return null
	return _units[coord.y * dims.x + coord.x]


func add_op(new_op: Command) -> void:
	_cmds.push_back(new_op)


func _ready() -> void:
	size = cell_width * dims
	cam.position = size * 0.5
	_place_some_units()
	
	add_child(_timer)
	_timer.start(_tick_millis * 0.001)


func _place_some_units() -> void:
	var my_supp := SupplierScene.instantiate()
	_units.push_back(my_supp)
	_supps.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, Vector2i(0, 2), [1, 2, 3, 4])

	for x in range(1, 5):
		var my_bus := BusScene.instantiate()
		_units.push_back(my_bus)
		add_child(my_bus)
		my_bus.setup(self, Vector2i(x,2))


func _process(delta: float) -> void:
	for cmd in _cmds:
		cmd.run(self)


func _on_tick() -> void:
	for cmd in _cmds:
		cmd.on_tick(self)

	_cmds = _cmds.filter(func(x): return !x.is_done)

	for u in _units:
		u.on_tick(self)
	



func _draw() -> void:
	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)
