@abstract class_name Command

var world: WorldPanel

## Number of _ticks it takes to finish.
var _ticks: int = 1

## Keeps track of _ticks passed for this command.
## Not that simple though, because sometimes the command has to wait for something and freezes.
var _count: int = 0


func _init(world_: WorldPanel, tick_count: int) -> void:
	assert(tick_count > 0)
	self.world = world_
	self._ticks = tick_count


func get_tick_count() -> int:
	return _ticks


## Called every frame until the tick ends, in which after `on_tick` is called.
func do_per_frame(dt: float) -> void:
	pass


## Called on the _ticks after the command spawns, should not call `inc_count` here!
func on_spawn() -> void:
	pass


## Called on the _ticks after the command spawns
func on_tick() -> void:
	pass


func preprocess_tick() -> void:
	_count += 1


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


func is_done() -> bool:
	assert(_ticks > 0)
	return _count >= _ticks
