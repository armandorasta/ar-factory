class_name UNSupplier extends Unit

@onready var sprite: Sprite2D = $Sprite2D

var _seq: PackedInt32Array
var _index: int = -1


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, sequence: PackedInt32Array) -> void:
	super.init(world_, TickType.STEADY, work_rate, 0, 1, gloc)
	self._seq = sequence

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)


func pend_new_commands() -> void:
	if _is_done_with_seq():
		return

	if !is_work_tick():
		return

	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return
	
	pend_cmd(CmdSpawn.new(world, grid_loc, _pop_next_value()))
	pend_cmd(CmdSlide.new(world, grid_loc, dir))


func reset() -> void:
	super.reset()
	_index = -1


func _is_done_with_seq() -> bool:
	return _index >= _seq.size() - 1


func _pop_next_value() -> int:
	assert(!_is_done_with_seq())
	_index += 1
	return _seq[_index]


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5
