class_name CmdAwait extends Command
## Waits for an item to arrive at a specific tile, makes sure if it's sliding in that the animation
## is over.

var grid_loc: Vector2i


func _init(gloc: Vector2i) -> void:
	super(1)
	self.grid_loc = gloc


func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(grid_loc) is WorldPanel.TlHolder)
	var my_tile := lv.world.get_tile(grid_loc) as WorldPanel.TlHolder
	if !my_tile.has_item() || my_tile.get_item().is_mid_animation():
		pause_this_tick()
		return
	
	assert(my_tile.get_item().is_allowed_to_move())
