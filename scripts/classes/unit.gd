@abstract class_name Unit extends Node2D

enum Direction {NORTH, SOUTH, WEST, EAST}

var dir: Direction = Direction.EAST
var grid_loc: Vector2i
var rate: int

## Keeps track of the `rate` every tick.
var _count: int = 0


func init(world: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int) -> void:
	self.dir = direction
	self.grid_loc = gloc
	self.rate = work_rate

	position = world.grid_to_pos(gloc)


@abstract func on_tick(world: WorldPanel) -> void


func inc_count() -> void:
	_count += 1


func dec_count() -> void:
	_count -= 1


func is_work_tick() -> bool:
	return _count > 0 && _count % rate == 0


func reset() -> void:
	_count = 1


static func dir_to_grid(my_dir: Direction) -> Vector2i:
	match my_dir:
		Direction.NORTH: return Vector2i( 0, -1)
		Direction.SOUTH: return Vector2i( 0, +1)
		Direction.EAST:  return Vector2i(+1,  0)
		Direction.WEST:  return Vector2i(-1,  0)
	return -INF * Vector2i()
