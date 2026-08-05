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


# Simulation params
var _play_mode: PlayMode = PlayMode.OFF


func _ready() -> void:
	cam.position = world.size * 0.5
	world.tick_timer.timeout.connect(_on_world_tick_timer_time_out)

	play_butt.pressed.connect(_on_play_butt_pressed)
	pause_butt.pressed.connect(_on_pause_butt_pressed)
	debug_butt.pressed.connect(_on_debug_butt_pressed)
	_sync_butt_states()


func _process(dt: float) -> void:
	match _play_mode:
		PlayMode.OFF: pass
		PlayMode.DEBUG: pass
		PlayMode.PLAY:
			world.do_per_frame(dt)


func _on_tick() -> void:
	world.on_tick()


func _on_world_tick_timer_time_out() -> void:
	assert(_play_mode != PlayMode.OFF)
	_on_tick()
	print("boobies")
	world.tick_timer.start(world.tick_millis * 0.001)


func _on_play_butt_pressed() -> void:
	assert(_play_mode != PlayMode.PLAY)
	_play_mode = PlayMode.PLAY
	_sync_butt_states()
	world.tick_timer.paused = false
	_on_world_tick_timer_time_out()


func _on_pause_butt_pressed() -> void:
	assert(_play_mode != PlayMode.OFF)
	_play_mode = PlayMode.OFF
	_sync_butt_states()
	_finalize_runtime()


func _on_debug_butt_pressed() -> void:
	_play_mode = PlayMode.DEBUG
	_sync_butt_states()
	print("debug_butt_pressed")


func _sync_butt_states() -> void:
	match _play_mode:
		PlayMode.OFF:
			play_butt.disabled = false
			pause_butt.disabled = true
			debug_butt.disabled = false

		PlayMode.DEBUG:
			play_butt.disabled = false
			pause_butt.disabled = false
			debug_butt.disabled = false

		PlayMode.PLAY:
			play_butt.disabled = true
			pause_butt.disabled = false
			debug_butt.disabled = false


## Stuff to do when the program is about to shut-down, either because it has ended, or halted midway.
func _finalize_runtime() -> void:
	world.clean_up()
	world.tick_timer.paused = true
