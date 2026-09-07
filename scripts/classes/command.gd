@abstract class_name Command

## Number of _Ticks it takes to finish.
var _Ticks: int

## Keeps track of _Ticks passed for this command.
## Not that simple though, because sometimes the command has to wait for something and freezes.
var _Count: int = 0


func _init(tick_count: int) -> void:
	self._Ticks = tick_count


## Called every tick
@abstract func on_tick(lv: Level) -> void

## Returns the number of ticks the command takes to finish executing without getting interrupted.
## Most commands take 1 or 2 ticks. [br]
## Commands that take 0 ticks execute instantly and don't block any other commands, commands that
## take 1 or more ticks block and push other commands to next ticks. [br]
## For example 100 kill commands on the same tile will all execute in the same tick, while 2 slide 
## commands will take 2 ticks; one will block the other on the first tick.
func get_tick_count() -> int:
	return _Ticks


## Called every frame until the tick ends.
func do_per_frame(dt: float, lv: Level) -> void:
	pass


## Increments the counter, when this counter gets to a specific value, the command gets killed.
func count_this_tick() -> void:
	_Count += 1


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_Count -= 1


func is_done() -> bool:
	return _Count >= _Ticks
