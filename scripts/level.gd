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


# Simulation params
var _play_mode: PlayMode = PlayMode.OFF


func _ready() -> void:
	cam.position = world.size * 0.5
	world.tick_timer.timeout.connect(_on_world_tick_timer_time_out)

	play_butt.pressed.connect(_on_play_butt_pressed)
	pause_butt.pressed.connect(_on_pause_butt_pressed)
	debug_butt.pressed.connect(_on_debug_butt_pressed)
	_sync_butt_states()
	
	speed_slider.value_changed.connect(_on_speed_slider_value_changed)
	world.reset_tick_rate()
	speed_slider.value = world.get_tick_rate()


func _process(dt: float) -> void:
	match _play_mode:
		PlayMode.OFF: pass
		PlayMode.DEBUG: pass
		PlayMode.PLAY:
			world.do_per_frame(dt)


func _on_tick() -> void:
	world.on_tick()
	ticks_label.text = "ticks: %d" % world.tick_count


func _on_world_tick_timer_time_out() -> void:
	assert(_play_mode != PlayMode.OFF)
	_on_tick()
	world.tick_timer.start(world._tick_millis * 0.001)


func _on_play_butt_pressed() -> void:
	assert(_play_mode != PlayMode.PLAY)
	_play_mode = PlayMode.PLAY
	_sync_butt_states()
	_on_simulation_start()

	world.tick_timer.paused = false
	_on_world_tick_timer_time_out()


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
	world.tick_timer.paused = true
