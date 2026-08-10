class_name UnitUpdater extends Unit

enum UpdateType {
	DOUBLE,
	NEGATE,
	INCREMENT
}


@onready var sprite: Sprite2D = $Sprite2D

var update_type: UpdateType


static func apply(up_t: UpdateType, val: int) -> int:
	match up_t:
		UpdateType.DOUBLE   : return val * 2
		UpdateType.NEGATE   : return -val
		UpdateType.INCREMENT: return val + 1
	
	assert(false, "Unhandled update type")
	return -1


## Must be called after _ready
func setup(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int,
	update_type_: UpdateType) -> void:
	super.init(world, gloc, direction, work_rate, 1)
	self.update_type = update_type_

	sprite.apply_scale(world.cell_width / 128 * Vector2.ONE)
	sprite.translate(world.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)


func on_tick() -> void:
	if !check_for_item(0):
		pause_this_tick()
		return
	
	if !is_work_tick():
		return

	var dest_gloc := grid_loc + dir_to_grid(dir)
	if !world.is_within_bounds(dest_gloc) || world.get_item(dest_gloc) != null:
		pause_this_tick()
		return

	world.add_cmd(CmdUpdate.new(grid_loc, update_type))
	world.add_cmd(CmdSlide.new(grid_loc, dir))
	
	item_slots[0].item = null


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5