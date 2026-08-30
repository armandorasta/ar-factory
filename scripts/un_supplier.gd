class_name UNSupplier extends Unit

var _seq: PackedInt32Array
var _index: int = -1

var output_tile: WorldPanel.Tile


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, work_rate: int, sequence: PackedInt32Array) -> void:
	super.init(world_, TickType.STEADY, work_rate, gloc, Vector2i(2, 3))
	self._seq = sequence


func build_tiles() -> void:
	output_tile = add_output(Vector2i(1, 1), dir)


func pend_new_commands() -> void:
	if _is_done_with_seq():
		return

	if !is_work_tick():
		return

	if has_pending_cmds() && !is_just_awaiting_out_sliding_anim():
		pause_this_tick()
		return
	
	pend_cmd(CmdSpawn.new(world, output_tile.get_grid_loc(), _pop_next_value()))
	pend_cmd(CmdSlide.from_output(world, output_tile))


func reset() -> void:
	super.reset()
	_index = -1


func _is_done_with_seq() -> bool:
	return _index >= _seq.size() - 1


func _pop_next_value() -> int:
	assert(!_is_done_with_seq())
	_index += 1
	return _seq[_index]
