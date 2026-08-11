@abstract class_name Unit extends Node2D

enum Direction {NORTH, SOUTH, WEST, EAST}
enum Type {
	STEADY,       # Always counts ticks, even when no item is fed, like sliders.
	ON_DEMAND, # Counts ticks only when all item slots are filled, like updaters.
}

var type: Type
var dir: Direction = Direction.EAST
var grid_loc: Vector2i
var rate: int
var world: WorldPanel
var item_slots: Array[ItemSlot] = []

## Keeps track of the `rate` every tick.
var _count: int = 0

func init(world_: WorldPanel, type_: Type, work_rate: int, item_count: int, gloc: Vector2i) -> void:
	self.type = type_
	self.world = world_
	self.grid_loc = gloc
	self.rate = work_rate

	position = world.grid_to_pos(gloc)
	
	for i in item_count:
		item_slots.push_back(ItemSlot.new())


@abstract func on_tick() -> void


func is_work_tick() -> bool:
	return _count > 0 && _count % rate == 0


func preprocess_tick() -> void:
	for its in item_slots:
		its.update(world)

	match type:
		Type.STEADY:    
			_count += 1
		Type.ON_DEMAND:
			if item_slots.all(func(x): return x.is_full()):
				_count += 1
			else:
				_count = 0
		_:
			assert(false)


## Pauses the tick counter for this tick
func pause_this_tick() -> void:
	_count -= 1


## If an item is already there, returns true, otherwise checks for one, 
## if it finds one it assigns it to the slot and returns true, 
## if it doesn't find one it returns false
func check_for_item(slot_index: int) -> bool:
	assert(slot_index >= 0 && slot_index < item_slots.size())
	
	var slot := item_slots[slot_index]
	if slot.is_full(): # We have an item, no work needs to be done
		print("already have an item")
		return true
	
	# We don't have an item, so check if one was fed to the slot, and assign if so
	slot.item = world.get_item(slot.grid_loc)
	if !slot.is_empty():
		print("No item yet")
	else:
		print("Got fed an item just now")
	
	return slot.is_full()


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


	func is_full() -> bool:
		return item != null


	func is_empty() -> bool:
		return item == null


	func clear() -> Item:
		var my_it := item
		item = null
		return my_it


	func update(world: WorldPanel) -> void:
		item = world.get_item(grid_loc, true)
