extends Camera2D

const default_zoom := 0.7
const drag_butt := MOUSE_BUTTON_MIDDLE


@export var speed := 1000.0
@export var zoom_step := 0.1
@export var min_zoom := 0.5 # The less the further
@export var max_zoom := 3.0


var is_mouse_landed: bool = false
var drag_offset = Vector2()


func _ready() -> void:
	zoom = Vector2.ONE * default_zoom


func _process(dt: float) -> void:
	var dx := Input.get_vector("cam_left", "cam_right", "cam_up", "cam_down")
	position += dx / zoom.x * speed * dt
	
	if is_mouse_landed:
		position = drag_offset - get_local_mouse_position()


func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton && event.button_index == drag_butt:
		if event.is_pressed():
			is_mouse_landed = true
			drag_offset = position + get_local_mouse_position()
		elif event.is_released():
			is_mouse_landed = false


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom -= Vector2.ONE * zoom_step
		elif event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom += Vector2.ONE * zoom_step

		zoom.x = clamp(zoom.x, min_zoom, max_zoom)
		zoom.y = clamp(zoom.y, min_zoom, max_zoom)