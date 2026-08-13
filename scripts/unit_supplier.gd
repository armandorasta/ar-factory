class_name UnitSupplier extends Unit

enum State {
	DEFAULT,
	ITEM_STUCK_ON_TOP,
	SLIDING_AWAY,
}

@onready var sprite: Sprite2D = $Sprite2D

var _seq: PackedInt32Array
var _index: int = 0
var _state: State = State.DEFAULT

## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, sequence: PackedInt32Array) -> void:
	super.init(world_, Type.STEADY, work_rate, 0, gloc)
	self._seq = sequence

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)


func on_tick() -> void:
	if !is_work_tick() || has_pending_cmds():
		return

	match _state:
		State.DEFAULT:
			if is_done_with_seq():
				return
			
			if world.get_tile(grid_loc).is_reserved():
				pause_this_tick()
				return
			
			pend_cmd(world, CmdSpawn.new(world, grid_loc, pop_next_value()))
			_state = State.SLIDING_AWAY
			on_tick()
		
		State.SLIDING_AWAY:
			if !world.item_can_slide(grid_loc, dir):
				pause_this_tick()
				return
			
			pend_cmd(world, CmdSlide.new(world, grid_loc, dir))
			_state = State.DEFAULT


func reset() -> void:
	super.reset()
	_index = 0
	_state = State.DEFAULT


func is_done_with_seq() -> bool:
	return _index >= _seq.size()


func pop_next_value() -> int:
	assert(!is_done_with_seq())
	_index += 1
	return _seq[_index - 1]


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
