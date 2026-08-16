class_name UnitDemander extends Unit

@onready var sprite: Sprite2D = $Sprite2D
@onready var label: Label = $Sprite2D/Label

var _seq: PackedInt32Array
var _index: int = -1


## Must be called after _ready
func setup(world_: WorldPanel, gloc: Vector2i, sequence: PackedInt32Array) -> void:
	super.init(world_, Type.STEADY, 1, 1, gloc)
	self._seq = sequence

	sprite.apply_scale(world_.cell_width / 128 * Vector2.ONE)
	sprite.translate(world_.cell_width * 0.5 * Vector2.ONE)

	_update_text()

	item_slots[0].grid_loc = grid_loc


func on_tick() -> void:
	if _is_done_with_seq():
		print("Done!!1!!")
		return

	if has_pending_cmds():
		pause_this_tick()
		return
	
	pend_cmd(CmdDemand.new(world, grid_loc, _pop_next_value()))


func reset() -> void:
	super.reset()
	_index = 0


func _is_done_with_seq() -> bool:
	return _index >= _seq.size()


func _pop_next_value() -> int:
	assert(!_is_done_with_seq())
	_index += 1
	_update_text()
	return _seq[_index]


func _update_text() -> void:
	label.text = str(_seq[_index])
