class_name WorldPanel extends Panel

const BusScene := preload("res://scenes/bus.tscn")

var elems: Array[Unit] = []
var dims := Vector2i(15, 10)
var cell_width: float = 100.0

var _units: Array[Unit] = []

func _ready() -> void:
	size = cell_width * dims
	_place_some_units()


func _place_some_units() -> void:
	var my_bus := BusScene.instantiate()
	_units.push_back(my_bus)
	add_child(my_bus)
	my_bus.setup(self, Vector2i(1,2))


func _process(delta: float) -> void:
	pass


func _draw() -> void:
	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color.RED)

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color.RED)
