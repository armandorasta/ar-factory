class_name CmdCombine extends Command

const ItemScene := preload("res://scenes/item.tscn")

var src_grid_locs: Array[Vector2i] = []
var dest_grid_loc: Vector2i
var callable: Callable


static func from_tiles(src_tls: Array[WorldPanel.TlHolder], dest_tl: WorldPanel.TlHolder, 
	what_to_do: Callable
) -> CmdCombine:
	return CmdCombine.new(src_tls.map(func(x): return x.get_grid_loc()), dest_tl.get_grid_loc(), what_to_do)


## `what_to_do` will take values of items in specified locations is the same order of the locations, 
## and returns the value will be assigned to the generated item.
## `dest_tl` can overlap with `src_tls`; the old items will be destroyed before the new item is
## spawned.
func _init(src_glocs: Array[Vector2i], dest_gloc: Vector2i, what_to_do: Callable) -> void:
	super(0)
	assert(!src_glocs.is_empty())

	self.src_grid_locs = src_glocs
	self.dest_grid_loc = dest_gloc
	self.callable = what_to_do



func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(dest_grid_loc) is WorldPanel.TlHolder)
	assert(src_grid_locs.all(func(x): return lv.world.get_tile(x) is WorldPanel.TlHolder))
	if lv.world.get_tile(dest_grid_loc).is_reserved():
		pause_this_tick()
		return
	
	if src_grid_locs.any(func(x): return !lv.world.get_tile(x).has_item()):
		pause_this_tick()
		return

	for gloc in src_grid_locs:
		lv.world.get_tile(gloc).destroy_item()
	
	var values := src_grid_locs.map(func(x): return lv.world.get_tile(x).get_item().get_value())
	lv.world.spawn_item(dest_grid_loc, callable.call(values))
