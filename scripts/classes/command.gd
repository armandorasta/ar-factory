@abstract class_name Command

## Checked every tick, once true, the command is removed.
var is_done: bool = false


## Called every frame until the tick ends, in which after `on_tick` is called.
func do_per_frame(_world: WorldPanel) -> void:
	pass


## Called on the ticks after the command spawns
func on_spawn_tick(_world: WorldPanel) -> void:
	pass


## Called on the ticks after the command spawns
func on_tick(_world: WorldPanel) -> void:
	# This is a good default to prevent new commands from executing infinitely.
	is_done = true