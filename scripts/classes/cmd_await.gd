class_name CmdAwait extends Command
## Waits for an item to arrive at a specific tile, makes sure if it's sliding in that the animation
## is over.

var grid_loc: Vector2i


func _init(world_: WorldPanel, gloc: Vector2i) -> void:
	super(world_, 1)
	self.grid_loc = gloc


func on_tick() -> void:
	var my_tile := world.get_tile(grid_loc) as WorldPanel.TlHolder
	if !my_tile.has_item() || !my_tile.get_item().is_stationary():
		pause_this_tick()
		return

	# The item should be able to move, this means it did not move this tick, which means we are not
	# sending an item into oblivion mid-animation.
	assert(!my_tile.get_item().cant_move_this_tick)
