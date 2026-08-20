class_name UNDemander extends Unit

@onready var sprite: Sprite2D = $Sprite2D
@onready var label: Label = $Sprite2D/Label

var _seq: PackedInt32Array
var _index: int = -1
var _in_slider: UNSlider


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, sequence: PackedInt32Array) -> void:
	super.init(world_, Type.STEADY, 1, 1, 0, gloc)
	self._seq = sequence

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)
	set_dir(dir)

	_update_text()

	input_slots[0].grid_loc = grid_loc
	
	_in_slider = world_.get_unit(grid_loc + Unit.dir_to_grid(dir))
	if !(_in_slider is UNSlider): # Only allow sliders
		_in_slider = null


func pend_new_commands() -> void:
	if _in_slider == null: # No slider, no work ever
		return

	if _is_done_with_seq():
		print("Done!!1!!")
		return

	if has_pending_cmds():
		pause_this_tick()
		return

	pend_cmd(CmdDemand.new(world, grid_loc, _pop_next_value()))


func set_dir(new_dir: Direction) -> void:
	dir = new_dir
	match dir:
		Direction.EAST:  sprite.rotation = PI * 0.0
		Direction.SOUTH: sprite.rotation = PI * 0.5
		Direction.WEST:  sprite.rotation = PI * 1.0
		Direction.NORTH: sprite.rotation = PI * 1.5


func reset() -> void:
	super.reset()
	_index = -1


func _is_done_with_seq() -> bool:
	return _index >= _seq.size() - 1


func _pop_next_value() -> int:
	assert(!_is_done_with_seq())
	_index += 1
	_update_text()
	return _seq[_index]


func _update_text() -> void:
	label.text = str(_seq[_index])
