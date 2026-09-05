class_name Level extends Node2D

enum PlayMode {
	OFF,
	PLAY,
	DEBUG,
}

@onready var cam: Camera2D = $WorldPanel/Cam
@onready var world: WorldPanel = $WorldPanel
@onready var play_butt: Button = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt
@onready var pause_butt: Button = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt
@onready var debug_butt: Button = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt
@onready var tools_hbox: HBoxContainer = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox
@onready var play_hbox: HBoxContainer = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox
@onready var ticks_label: Label = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel
@onready var speed_slider: HSlider = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider
@onready var tick_speed_label: Label = $WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel


var default_tick_millis: float = 300.0

var _play_mode: PlayMode = PlayMode.OFF
var _tick_timer: Timer
var _tick_count: int = 0 # Number of ticks since the start.
var _tick_millis: float = 0.0


func _ready() -> void:
	cam.position = world.size * 0.5

	_tick_timer = Timer.new()
	_tick_timer.one_shot = true
	add_child(_tick_timer)
	_tick_timer.timeout.connect(_on_tick_timer_time_out)

	play_butt.pressed.connect(_on_play_butt_pressed)
	pause_butt.pressed.connect(_on_pause_butt_pressed)
	debug_butt.pressed.connect(_on_debug_butt_pressed)
	_sync_butt_states()
	
	speed_slider.value_changed.connect(_on_speed_slider_value_changed)
	reset_tick_rate()
	speed_slider.value = get_tick_rate()

	_add_tools()


func _process(dt: float) -> void:
	match _play_mode:
		PlayMode.OFF: pass
		PlayMode.DEBUG: pass
		PlayMode.PLAY:
			world.do_per_frame(dt)


## Returns the number of ticks passed since the play button was pressed.
func get_tick_count_since_start() -> int:
	return _tick_count


## Returns how many ticks pass per second.
func get_tick_rate() -> float:
	return 1000.0 / _tick_millis


## Returns the milliseconds passed since the begining of the tick.
func get_tick_elapsed_millis() -> float:
	return _tick_millis - _tick_timer.time_left*1000


func set_tick_rate(rate_per_second: float) -> void:
	_tick_millis = 1000.0 / rate_per_second


func reset_tick_rate() -> void:
	_tick_millis = default_tick_millis


func _on_tick() -> void:
	world.on_tick()
	ticks_label.text = "ticks: %d" % world._tick_count
	_tick_count += 1


func _on_tick_timer_time_out() -> void:
	assert(_play_mode != PlayMode.OFF)
	_on_tick()
	_tick_timer.start(world._tick_millis * 0.001)


func _on_play_butt_pressed() -> void:
	assert(_play_mode != PlayMode.PLAY)
	_play_mode = PlayMode.PLAY
	_sync_butt_states()
	_on_simulation_start()

	_tick_timer.paused = false
	_on_tick_timer_time_out()


func _on_pause_butt_pressed() -> void:
	assert(_play_mode != PlayMode.OFF)
	_play_mode = PlayMode.OFF
	_sync_butt_states()
	_on_simulation_end()


func _on_debug_butt_pressed() -> void:
	_play_mode = PlayMode.DEBUG
	_sync_butt_states()
	print("debug_butt_pressed")


func _on_speed_slider_value_changed(new_val: float) -> void:
	world.set_tick_rate(new_val)
	tick_speed_label.text = "%.1f tick/s" % (1000.0/world._tick_millis)


func _sync_butt_states() -> void:
	match _play_mode:
		PlayMode.OFF:
			play_butt.disabled = false
			pause_butt.disabled = true
			debug_butt.disabled = false
			play_hbox.hide()
			tools_hbox.show()


		PlayMode.DEBUG:
			play_butt.disabled = false
			pause_butt.disabled = false
			debug_butt.disabled = false
			play_hbox.show()
			tools_hbox.hide()

		PlayMode.PLAY:
			play_butt.disabled = true
			pause_butt.disabled = false
			debug_butt.disabled = false
			play_hbox.show()
			tools_hbox.hide()


func _on_simulation_start() -> void:
	pass


## Stuff to do when the program is about to shut-down, either because it has ended, or halted midway.
func _on_simulation_end() -> void:
	world.clean_up()
	_tick_timer.paused = true
	_tick_count = 0


func _add_tools() -> void:
	pass