@abstract class_name Command

var world: WorldPanel

## Number of _ticks it takes to finish.
var _ticks: int

## Keeps track of _ticks passed for this command.
## Not that simple though, because sometimes the command has to wait for something and freezes.
var _count: int = 0


func _init(world_: WorldPanel, tick_count: int) -> void:
	self.world = world_
	self._ticks = tick_count


func get_tick_count() -> int:
	return _ticks


## Called every frame until the tick ends, called after `do_post_tick`.
func do_per_frame(dt: float) -> void:
	pass


## Called every tick
func on_tick() -> void:
	pass


func count_this_tick() -> void:
	_count += 1


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


func is_done() -> bool:
	return _count >= _ticks
