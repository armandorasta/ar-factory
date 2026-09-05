class_name CmdKill extends Command

var grid_loc: Vector2i


func _init(gloc: Vector2i) -> void:
	super(0)
	self.grid_loc = gloc


func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(grid_loc) is WorldPanel.TlHolder)
	var my_tile := lv.world.get_tile(grid_loc) as WorldPanel.TlHolder
	if !my_tile.has_item():
		pause_this_tick()
		return
	
	my_tile.destroy_item()
