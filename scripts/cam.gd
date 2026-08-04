extends Camera2D

@export var speed := 1000.0
@export var zoom_step := 0.2
@export var min_zoom := 0.5
@export var max_zoom := 3.0

func _process(dt: float) -> void:
	var dx := Input.get_vector("cam_left", "cam_right", "cam_up", "cam_down")
	position += dx / zoom.x * speed * dt

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom -= Vector2.ONE * zoom_step
		elif event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom += Vector2.ONE * zoom_step

		zoom.x = clamp(zoom.x, min_zoom, max_zoom)
		zoom.y = clamp(zoom.y, min_zoom, max_zoom)
