@abstract class_name Unit extends Node2D

enum Direction {NORTH, SOUTH, WEST, EAST}
enum Type {
	CONSTANT, # Always counts ticks, even when no item is fed, like sliders.
	MACHINE,  # Counts ticks only when all item slots are filled, like updaters.
}

var dir: Direction = Direction.EAST
var grid_loc: Vector2i
var rate: int
var world: WorldPanel
var item_slots: Array[ItemSlot] = []

## Keeps track of the `rate` every tick.
var _count: int = 0

func init(world_: WorldPanel, gloc: Vector2i, direction: Direction, work_rate: int, 
	item_count: int) -> void:
	self.world = world_
	self.dir = direction
	self.grid_loc = gloc
	self.rate = work_rate

	position = world.grid_to_pos(gloc)
	
	for i in item_count:
		item_slots.push_back(ItemSlot.new())


@abstract func on_tick() -> void


func is_work_tick() -> bool:
	return _count > 0 && _count % rate == 0


func increment_count() -> void:
	_count += 1


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


## If an item is already there, returns true, otherwise checks for one, 
## if it finds one it assigns it to the slot and returns true, 
## if it doesn't find one it returns false
func check_for_item(slot_index: int) -> bool:
	assert(slot_index >= 0 && slot_index < item_slots.size())
	
	var slot := item_slots[slot_index]
	if slot.item != null: # We have an item, no work needs to be done
		print("already have an item")
		return true
	
	# We don't have an item, so check if one was fed to the slot, and assign if so
	slot.item = world.get_item(slot.grid_loc)
	if slot.item == null:
		print("No item yet")
	else:
		print("Got fed an item just now")
	return slot.item != null


func reset() -> void:
	_count = 1
	for its in item_slots:
		its.item = null


static func dir_to_grid(my_dir: Direction) -> Vector2i:
	match my_dir:
		Direction.NORTH: return Vector2i( 0, -1)
		Direction.SOUTH: return Vector2i( 0, +1)
		Direction.EAST:  return Vector2i(+1,  0)
		Direction.WEST:  return Vector2i(-1,  0)
	return -INF * Vector2i()


class ItemSlot:
	var grid_loc: Vector2i
	var item: Item