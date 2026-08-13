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
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, update_type_: UpdateType) -> void:
	super.init(world_, Type.ON_DEMAND, work_rate, 1, gloc)
	self.update_type = update_type_

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)

	item_slots[0].grid_loc = grid_loc


func on_tick() -> void:
	if !is_work_tick():
		return

	if !world.item_can_slide(grid_loc, dir):
		pause_this_tick()
		return

	world.add_cmd(CmdUpdate.new(world, grid_loc, update_type))
	world.add_cmd(CmdSlide.new(world, grid_loc, dir))


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
