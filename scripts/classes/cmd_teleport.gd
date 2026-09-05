class_name CmdTeleport extends Command

var src_grid_loc: Vector2i
var dest_grid_loc: Vector2i


func _init(gfrom: Vector2i, gto: Vector2i) -> void:
	super(0)
	
	self.src_grid_loc = gfrom
	self.dest_grid_loc = gto


func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(src_grid_loc) is WorldPanel.TlHolder)
	assert(lv.world.get_tile(dest_grid_loc) is WorldPanel.TlHolder)
	var src_tile := lv.world.get_tile(src_grid_loc) as WorldPanel.TlHolder
	# Wait for an item to show up, and make sure it's not mid-animation.
	if !src_tile.has_item() || !src_tile.get_item().is_stationary(): 
		pause_this_tick()
		return

	lv.world.teleport_item(src_grid_loc, dest_grid_loc)
	
	
