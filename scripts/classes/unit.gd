@abstract 
class_name Unit extends Node

enum Direction {NORTH, SOUTH, WEST, EAST}

var dir: Direction = Direction.EAST
var grid_loc: Vector2i
var rate: int

var _count: int


@abstract func on_tick(world: WorldPanel) -> void


func init(gloc: Vector2i, direction: Direction) -> void:
	self.dir = direction
	self.grid_loc = gloc





static func dir_to_grid(my_dir: Direction) -> Vector2i:
	match my_dir:
		Direction.NORTH: return Vector2i( 0, +1)
		Direction.SOUTH: return Vector2i( 0, -1)
		Direction.EAST:  return Vector2i(+1,  0)
		Direction.WEST:  return Vector2i(-1,  0)
	return -INF * Vector2i()











