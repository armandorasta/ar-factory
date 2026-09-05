class_name CmdClone extends Command

const ItemScene := preload("res://scenes/item.tscn")

var src_grid_loc: Vector2i
var dest_grid_locs: Array[Vector2i] = []


static func from_tiles(src_tile: WorldPanel.TlHolder, dest_tiles: Array[WorldPanel.TlHolder]) -> CmdClone:
	return CmdClone.new(src_tile.get_grid_loc(), dest_tiles.map(func(x): return x.get_grid_loc()))


func _init(src_loc: Vector2i, dest_locs: Array[Vector2i]) -> void:
	super(1)
	self.src_grid_loc = src_loc
	for gloc in dest_locs:
		dest_grid_locs.push_back(gloc)


func on_tick(lv: Level) -> void:
	# TODO: make it clone incrementaly, that is whenever one of the dest tiles is free, an item is
	# is spawned there immediately instead of waiting for all dest tiles to be free at the same time
	# first!

	assert(lv.world.get_tile(src_grid_loc) is WorldPanel.TlHolder)
	assert(dest_grid_locs.all(func(x): return lv.world.get_tile(x) is WorldPanel.TlHolder))
	var src_tile := lv.world.get_tile(src_grid_loc) as WorldPanel.TlHolder
	# Wait for an item to show up, and make sure it's not mid-animation.
	if !src_tile.has_item() || !src_tile.get_item().is_stationary(): 
		pause_this_tick()
		return

	for gloc in dest_grid_locs:
		lv.world.clone_item(src_grid_loc, gloc)
	
	
