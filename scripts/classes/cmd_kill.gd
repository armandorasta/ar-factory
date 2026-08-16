class_name CmdKill extends Command

var grid_loc: Vector2i


func _init(world_: WorldPanel, gloc: Vector2i) -> void:
	super(world_, 0)
	self.grid_loc = gloc


func on_tick() -> void:
	var my_tile := world.get_tile(grid_loc)
	if !my_tile.has_item():
		pause_this_tick()
		return
	
	my_tile.destroy_item()
