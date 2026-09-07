class_name CmdSpawn extends Command

const ItemScene := preload("res://scenes/item.tscn")

var grid_loc: Vector2i
var value: int


static func from_tiles(tl: WorldPanel.TlHolder, val: int) -> CmdSpawn:
	return CmdSpawn.new(tl.get_grid_loc(), val)


func _init(gloc: Vector2i, val: int) -> void:
	super(0)
	self.grid_loc = gloc
	self.value = val


func on_tick(lv: Level) -> void:
	assert(lv.world.get_tile(grid_loc) is WorldPanel.TlHolder)
	if lv.world.get_tile(grid_loc).is_reserved():
		pause_this_tick()
		return
	
	lv.world.spawn_item(grid_loc, value)
