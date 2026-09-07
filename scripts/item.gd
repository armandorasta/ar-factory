class_name Item extends Node2D

const MAX_VALUE: int = 1000
const MIN_VALUE: int = -MAX_VALUE

@onready var Sprite: Sprite2D = $CenterContainer/Sprite2D
@onready var Center_label: Label = $CenterContainer/Sprite2D/CenterContainer/Label

var World: WorldPanel
var Grid_loc: Vector2i

## When this flag is set, it means the item is not allowed to move from it's current tile until next
## tick.
var _Is_disallow_move_this_tick: bool = false

## This flag is checked so the item doesn't get delete by something like [method WorldPanel.remove_item]
## mid-animation.
var _Is_mid_animation: bool = false
var _Value: int


func setup(world: WorldPanel, gloc: Vector2i, val: int) -> void:
	self.World = world
	self.Grid_loc = gloc
	sync_pos_with_grid()
	set_value(val)

	Sprite.apply_scale(World.cell_width / 200 * Vector2.ONE)
	Sprite.translate(World.cell_width * 0.5 * Vector2.ONE)


func get_value() -> int:
	return _Value


## Not mid sliding animation or something.
func is_allowed_to_move() -> bool:
	return !_Is_disallow_move_this_tick


## Items may not be deleted mid-animation! Wait until this function returns [false] if that's what
## you desire. 
func is_mid_animation() -> bool:
	return _Is_mid_animation


func set_value(new_val: int) -> void:
	# assert(MIN_VALUE <= new_val && new_val <= MAX_VALUE)
	_Value = clampi(new_val, MIN_VALUE, MAX_VALUE)
	Center_label.text = str(_Value)


func sync_pos_with_grid() -> void:
	position = World.grid_to_pos(Grid_loc)


func disallow_movement_this_tick() -> void:
	_Is_disallow_move_this_tick = true


## Items are once again allowed to move this tick, this function is not meant to be called by the
## user.
func reset_movement_flag() -> void:
	_Is_disallow_move_this_tick = false


func set_mid_animation_flag(val: bool) -> void:
	_Is_mid_animation = val