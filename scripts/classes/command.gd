@abstract class_name Command

## Checked every tick, once true, the command is removed.
var is_done: bool = false

## Called every frame until the tick ends, in which after `on_tick` is called.
@abstract func run(world: WorldPanel) -> void

## Called on the ticks after the command spawns
@abstract func on_tick(world: WorldPanel) -> void