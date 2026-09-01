class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const UNSliderScene := preload("res://scenes/un_slider.tscn")
const UNSupplierScene := preload("res://scenes/un_supplier.tscn")
const UNUpdaterScene := preload("res://scenes/un_updater.tscn")
const UNBinScene := preload("res://scenes/un_bin.tscn")
const UNDemanderScene := preload("res://scenes/un_demander.tscn")

var debug_font: Font = load("res://resources/fonts/AnonymousPro-Regular.ttf")

var dims := Vector2i(20, 15)
var cell_width: float = 100.0

## Simulation
var default_tick_millis: float = 300.0
var tick_timer: Timer
var tick_count: int = 0 # Number of ticks since the start.

var _tick_millis: float
var _units: Array[Unit] = []
var _tiles: Array[Tile] = []
var blocked_slide_cmds: Array[CmdSlide] = []


func get_tick_rate() -> float:
	return 1000.0 / _tick_millis


func has_tile(gloc: Vector2i) -> bool:
	return _tiles[gloc.y * dims.x + gloc.x] != null


func get_tile(gloc: Vector2i) -> Tile:
	return _tiles[gloc.y * dims.x + gloc.x]


func get_unit(gloc: Vector2i) -> Unit:
	for u in _units:
		if u.has_tile(gloc):
			return u
	return null


func get_tick_elapsed_millis() -> float:
	return _tick_millis - tick_timer.time_left*1000


func is_within(gloc: Vector2i) -> bool:
	return Rect2i(Vector2i(), dims).has_point(gloc)


func grid_to_pos(gloc: Vector2i) -> Vector2:
	return cell_width * gloc
	# return cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5)


func pos_to_grid(pos: Vector2) -> Vector2i:
	return Vector2i(pos / cell_width)

## Does not account for whether the tile is reserved or not.
func item_can_be_in(gloc: Vector2i) -> bool:
	if is_within(gloc): 
		return true
	if get_tile(gloc).is_free(): 
		return true
	return false


func item_can_slide(item_g_loc: Vector2i, item_dir: Unit.Direction) -> bool:
	var dest_tile := get_tile(item_g_loc + Unit.Direction_to_grid(item_dir))
	return !dest_tile.is_reserved() && !dest_tile.has_wall(Unit.Direction_inv(item_dir))


func install_tile(tile: Tile) -> Tile:
	var gloc := tile.get_grid_loc()
	assert(is_within(gloc))
	assert(!has_tile(gloc))
	
	_tiles[gloc.y * dims.x + gloc.x] = tile
	return tile


## Removes the tile from the world, and destroys items within it
func remove_tile(gloc: Vector2i, is_maybe_null := false) -> void:
	assert(is_maybe_null || has_tile(gloc))
	assert(is_within(gloc))

	if !has_tile(gloc):
		return

	var i := gloc.y * dims.x + gloc.x
	if _tiles[i] is TlHolder:
		_tiles[i].destroy_item(true)
	_tiles[i] = null


## Moves the tile from one location to another, along with the items it contains.
func move_tile(gfrom: Vector2i, gto: Vector2i, is_maybe_null := false) -> Tile:
	assert(is_maybe_null || has_tile(gfrom))
	assert(gfrom == gto || !has_tile(gto))
	assert(is_within(gfrom))
	assert(is_within(gto))
	
	if gfrom == gto: # Moving something nowhere?
		return
	
	var isrc  := gfrom.y * dims.x + gfrom.x
	var idest := gto.y   * dims.x + gto.x
	var my_tile := _tiles[isrc]
	_tiles[isrc] = null
	my_tile.set_grid_loc_unchecked(gto)
	_tiles[idest] = my_tile
	return my_tile


## Removes the tile from the world, and returns it.
func extract_tile(gloc: Vector2i, is_maybe_null := false) -> Tile:
	assert(is_maybe_null || has_tile(gloc))
	assert(is_within(gloc))

	var i := gloc.y * dims.x + gloc.x
	var my_tile := _tiles[i]
	_tiles[i] = null
	return my_tile


## Replaces the tile in the location of the tile passed in with it. Same as `remove_tile` followed
## by `install_tile`.
## `is_maybe_null` means the old tile might be null, the one passed in can't be null!
func override_tile(tile: Tile, is_maybe_null := false) -> void:
	var gloc := tile.get_grid_loc()
	assert(is_maybe_null || has_tile(gloc))
	remove_tile(gloc, is_maybe_null)
	install_tile(tile)


## Swaps two tiles, or one tile with itself.
func swap_tiles(gloc1: Vector2i, gloc2: Vector2i, is_maybe_null := false) -> void:
	assert(is_maybe_null || has_tile(gloc1) && has_tile(gloc2))
	assert(is_within(gloc1))
	assert(is_within(gloc2))
	
	if gloc1 == gloc2:
		return
	
	var i1 := gloc1.y * dims.x + gloc1.x
	var i2 := gloc2.y * dims.x + gloc2.x

	# Swapping 2 nulls? we aint swapping nothin'
	if _tiles[i1] == null && _tiles[i2] == null:
		return
	
	if _tiles[i1] == null:
		move_tile(gloc2, gloc1)
		return
	
	if _tiles[i2] == null:
		move_tile(gloc1, gloc2)
		return
	
	# Swaps the grid_locs
	var loc1 := _tiles[i1].get_grid_loc()
	_tiles[i1].set_grid_loc_unchecked(_tiles[i2].get_grid_loc())
	_tiles[i2].set_grid_loc_unchecked(loc1)
	
	# Swap the locations on the array
	var temp_tile := _tiles[i1]
	_tiles[i1] = _tiles[i2]
	_tiles[i2] = temp_tile


## Returns the item just added.
func add_item(gloc: Vector2i, value: int) -> Item:
	var my_tile := get_tile(gloc)
	assert(!my_tile.is_reserved()) # Might have to change this to `has_item`, but who knows.
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	new_item.setup(self, gloc, value)
	my_tile.set_item(new_item)
	return new_item


## Returns the clone, not the original
func clone_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	assert(is_maybe_null || get_tile(g_from).has_item())
	assert(is_override || !get_tile(g_to).has_item())
	return add_item(g_to, get_tile(g_from).get_item().value)


## Returns the teleported item.
func teleport_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	var src_tile := get_tile(g_from)
	var dest_tile := get_tile(g_to)
	assert(is_maybe_null || src_tile.has_item())
	assert(is_override || !dest_tile.has_item())
	dest_tile.set_item(src_tile.extract_item())
	return dest_tile.get_item()


func do_per_frame(dt: float) -> void:
	for u in _units:
		u.do_per_frame(dt)


func on_tick() -> void:
	for tile in _tiles:
		if tile is TlHolder:
			var holder := tile as TlHolder
			if holder.has_item():
				tile.get_item().cant_move_this_tick = false

	for u in _units: # Preprocess all units first
		u.preprocess_tick()

	# The above loop should not be moved below this one.
	for u in _units: # Add commands
		u.pend_new_commands()

	for u in _units: # Process commands
		u.handle_cmd_tick()

	_handle_slide_cmd_overlapping() # Overlap slide commands
	
	tick_count += 1
	queue_redraw()


func set_tick_rate(rate_per_second: float) -> void:
	_tick_millis = 1000.0 / rate_per_second


func reset_tick_rate() -> void:
	_tick_millis = default_tick_millis


func clean_up() -> void:
	for u in _units:
		u.reset()
	
	for tile in _tiles:
		if tile is TlHolder:
			tile.destroy_item(true)
	
	tick_count = 0
	blocked_slide_cmds.clear()


func _ready() -> void:
	size = cell_width * dims

	tick_timer = Timer.new()
	tick_timer.one_shot = true
	add_child(tick_timer)

	for i in dims.x * dims.y:
		_tiles.push_back(null)

	_place_some_units()


func _draw() -> void:
	draw_rect(Rect2(Vector2(), size), Color(0, 0.1, 0) if tick_count % 2 == 1 else Color(0, 0.11, 0))

	for i in range(1, dims.x):
		draw_line(Vector2(i * cell_width, 0), Vector2(i * cell_width, size.y), Color(0, 0.18, 0))

	for i in range(1, dims.y):
		draw_line(Vector2(0, i * cell_width), Vector2(size.x, i * cell_width), Color(0, 0.15, 0))

	for y in dims.y:
		for x in dims.x:
			var tile_gloc := Vector2i(x, y)
			if has_tile(tile_gloc):
				get_tile(tile_gloc).debug_draw(self)
	
	for unit in _units:
		draw_rect(Rect2(grid_to_pos(unit.grid_loc), cell_width*unit.dims), Color.BLACK, false, -2.0)
		draw_string(debug_font, unit.position + Vector2(0.0, 15.0), unit.get_script().get_global_name(), HORIZONTAL_ALIGNMENT_LEFT, cell_width, 16, Color.BLACK)


func _place_some_units() -> void:
	_place_supplier(Vector2i(0, 5), Unit.Direction.SOUTH, 1, [1, 2, 3])
	_place_slider(Vector2i(1, 7), Unit.Direction.SOUTH)
	_place_slider(Vector2i(1, 8), Unit.Direction.EAST)
	_place_slider(Vector2i(2, 8), Unit.Direction.NORTH)
	_place_slider(Vector2i(2, 7), Unit.Direction.WEST)
	# _place_supplier(Vector2i(0, 10), Unit.Direction.NORTH, 1, [1, 2, 3])
	# _place_slider(Vector2i(2, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(3, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(4, 8), Unit.Direction.EAST)
	_place_demander(Vector2i(5, 8), Unit.Direction.WEST, [1, 2, 3])

	_units.shuffle()
	
	# _place_slider(Vector2i(2, 3), Unit.Direction.EAST)
	# _place_updater(Vector2i(3, 3), Unit.Direction.EAST, 1, UNUpdater.UpdateType.DOUBLE)
	# _place_demander(Vector2i(4, 3), Unit.Direction.WEST, [2, 4, 6])
	# _place_slider(Vector2i(3, 3), Unit.Direction.NORTH)
	# _place_slider(Vector2i(3, 2), Unit.Direction.NORTH)
	# _place_slider(Vector2i(3, 1), Unit.Direction.WEST)
	# # _place_slider(Vector2i(2, 1), Unit.Direction.WEST)
	# _place_updater(Vector2i(2, 1), Unit.Direction.WEST, 1, UNUpdater.UpdateType.DOUBLE)
	# _place_slider(Vector2i(1, 1), Unit.Direction.WEST)
	

func _place_supplier(loc: Vector2i, dir: Unit.Direction, rate: int, seq: Array[int]) -> void:
	var my_supp := UNSupplierScene.instantiate()
	_units.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, loc, rate, seq)
	my_supp.set_dir(dir)


func _place_slider(loc: Vector2i, dir: Unit.Direction) -> void:
	var my_bus := UNSliderScene.instantiate()
	_units.push_back(my_bus)
	add_child(my_bus)
	my_bus.setup(self, loc, 1)
	my_bus.set_dir(dir)


func _place_updater(loc: Vector2i, dir: Unit.Direction, rate: int, up_t: 
	UNUpdater.UpdateType
) -> void:
	var doub := UNUpdaterScene.instantiate()
	_units.push_back(doub)
	add_child(doub)
	doub.setup(self, loc, rate, up_t)
	doub.set_dir(dir)


func _place_bin(loc: Vector2i) -> void:
	var bin := UNBinScene.instantiate()
	_units.push_back(bin)
	add_child(bin)
	bin.setup(self, loc)

func _place_demander(loc: Vector2i, dir: Unit.Direction, seq: Array[int]) -> void:
	var dem := UNDemanderScene.instantiate()
	_units.push_back(dem)
	add_child(dem)
	dem.setup(self, loc, seq)
	dem.set_dir(dir)


## Called after handling all ticks of pending commands
func _handle_slide_cmd_overlapping() -> void:
	# Slide command overlapping.
	# If any update happens in a pass, end that pass and start over,
	# repeat until a full pass happens with no updates.
	while 1 + 1 == 2:
		var dirty_index := -1
		for i in blocked_slide_cmds.size():
			if blocked_slide_cmds[i].is_updated_this_pass():
				dirty_index = i
				break
		if dirty_index == -1: # None has updated? it's over.
			break
		# Something moved? can only move once per tick so bye-bye.
		blocked_slide_cmds.pop_at(dirty_index)
	blocked_slide_cmds.clear()


#region Tile CLASSES

@abstract class Tile:
	const DEFAULT_DIR: Unit.Direction = Unit.Direction.EAST

	var _world: WorldPanel
	var _grid_loc: Vector2i

	func _init(world_: WorldPanel, gloc: Vector2i) -> void:
		self._world = world_
		self._grid_loc = gloc


	## `movement_dir` is the direction of the movement, not the direction of the edge of the tile.
	## If an item is sliding in from the west moving east, `Direction.EAST` should passed, not 
	## `Direction.WEST`.
	@abstract func can_enter_in_dir(_movement_dir: Unit.Direction) -> bool

	## Items are not allowed to slide into reserved tiles.
	@abstract func is_reserved() -> bool
	@abstract func set_reserved(to_what: bool) -> void
	
	## Used for debug drawing.
	@abstract func type_to_col() -> Color

	## For `TlIO` this is an alias for `get_output_dir`.
	func get_dir() -> Unit.Direction: 
		return DEFAULT_DIR
	
	## For `TlIO` this is an alias for `set_output_dir`.
	func set_dir(_new_dir: Unit.Direction) -> void: 
		pass
	
	## Rotates the unit 90 degrees clockwise. `rotate270` just calls this 3 times.
	func rotate90() -> void:
		set_dir(Unit.Direction_rotate90(get_dir()))
	
	## Implemented in terms of `rotate90` by default, this way I only need to implement 1 instead of
	## 2 functions.
	func rotate270() -> void:
		rotate90()
		rotate90()
		rotate90()
	

	func get_grid_loc() -> Vector2i:
		return _grid_loc


	## Use `WorldPanel.move_tile()` instead, this doesn't actually move the tile!
	func set_grid_loc(gloc: Vector2i) -> void:
		assert(!_world.has_tile(gloc))
		set_grid_loc_unchecked(gloc)


	## Use `WorldPanel.move_tile()` instead
	func set_grid_loc_unchecked(gloc: Vector2i) -> void:
		_grid_loc = gloc

	
	func debug_draw(world: WorldPanel) -> void:
		var my_pos := world.grid_to_pos(_grid_loc)
		world.draw_rect(Rect2(my_pos, Vector2.ONE * world.cell_width), type_to_col())


@abstract class TlHolder extends Tile:
	var _item: Item = null
	var _is_reserved: bool = false
	
	func _init(world_: WorldPanel, gloc: Vector2i) -> void:
		super(world_, gloc)


	func is_reserved() -> bool:
		return _is_reserved
	

	func set_reserved(to_what: bool) -> void:
		_is_reserved = to_what


	func has_item() -> bool:
		return _item != null


	func get_item(is_maybe_null: bool = false) -> Item:
		assert(is_maybe_null || has_item())
		return _item
	

	func set_item(new_item: Item, is_override := false) -> void:
		assert(is_override || !has_item())
		destroy_item(true)

		_item = new_item
		_is_reserved = true


	func destroy_item(is_maybe_null := false) -> void:
		assert(is_maybe_null || has_item())
		if _item != null:
			_item.queue_free()
			_item = null
		_is_reserved = false

	
	## Removes the item from the block and returns it, the caller has full ownership of the item.
	func extract_item(is_maybe_null := false) -> Item:
		assert(is_maybe_null || has_item())
		var my_item := _item
		_item = null
		_is_reserved = false
		return my_item


class TlSolid extends Tile:
	func _init(world_: WorldPanel, gloc: Vector2i) -> void:
		super(world_, gloc)


	func is_reserved() -> bool:return true
	func set_reserved(_to_what: bool) -> void: pass
	func type_to_col() -> Color: return Color.DARK_GRAY
	func can_enter_in_dir(_movement_dir: Unit.Direction) -> bool: return false
	

class TlInput extends TlHolder:
	var _dir: Unit.Direction = Unit.Direction.EAST


	func _init(world_: WorldPanel, gloc: Vector2i, dir_ := DEFAULT_DIR) -> void:
		super(world_, gloc)
		self._dir = dir_


	func type_to_col() -> Color: 
		return Color.INDIAN_RED
	
	
	func get_dir() -> Unit.Direction:
		return _dir	

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		_world.queue_redraw()


	func can_enter_in_dir(movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_dir)

	
	func debug_draw(world: WorldPanel) -> void:
		super(world)
		var my_pos := world.grid_to_pos(_grid_loc)
		var edge_pos := (my_pos
			+ (0.5*world.cell_width) * Vector2.ONE
			+ (0.4*world.cell_width) * Unit.Direction_to_grid(_dir))
		# world.draw_circle(edge_pos, world.cell_width * 0.1, Color.BLACK)
		world.draw_circle(edge_pos, world.cell_width * 0.1, Color.DARK_RED, false)
		

class TlOutput extends TlHolder:
	var _dir: Unit.Direction = Unit.Direction.EAST


	func _init(world_: WorldPanel, gloc: Vector2i, dir_ := DEFAULT_DIR) -> void:
		super(world_, gloc)
		self._dir = dir_


	func type_to_col() -> Color: 
		return Color.LIGHT_GREEN

	
	func get_dir() -> Unit.Direction:
		return _dir	

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		_world.queue_redraw()


	func can_enter_in_dir(movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_dir)

	
	func debug_draw(world: WorldPanel) -> void:
		super(world)
		var my_pos := world.grid_to_pos(_grid_loc)
		var edge_pos := (my_pos
			+ (0.5*world.cell_width) * Vector2.ONE
			+ (0.4*world.cell_width) * Unit.Direction_to_grid(_dir))
		# world.draw_circle(edge_pos, world.cell_width * 0.1, Color.BLACK)
		world.draw_circle(edge_pos, world.cell_width * 0.1, Color.DARK_GREEN, false)


class TlIO extends TlHolder:
	var _output_dir: Unit.Direction
	var _input_dir: Unit.Direction


	func _init(world_: WorldPanel, gloc: Vector2i, out_dir: Unit.Direction, in_dir: Unit.Direction) -> void:
		super(world_, gloc)
		self._output_dir = out_dir
		self._input_dir = in_dir


	func type_to_col() -> Color: 
		return Color.LIGHT_YELLOW

	
	## Same as `get_output_dir`
	func get_dir() -> Unit.Direction:
		return get_output_dir()

	
	## Same as `set_output_dir`
	func set_dir(new_dir: Unit.Direction) -> void:
		return set_output_dir(new_dir)


	func rotate90() -> void:
		_input_dir =  Unit.Direction_rotate90(_input_dir)
		_output_dir = Unit.Direction_rotate90(_output_dir)


	func can_enter_in_dir(movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_input_dir)

	
	func get_output_dir() -> Unit.Direction:
		return _output_dir
	
	
	func get_input_dir() -> Unit.Direction:
		return _input_dir

	
	func set_output_dir(new_dir: Unit.Direction, is_set_in_to_inv := false) -> void:
		assert(is_set_in_to_inv || new_dir != _input_dir)
		_output_dir = new_dir
		if is_set_in_to_inv:
			_input_dir = Unit.Direction_inv(new_dir)
		_world.queue_redraw()


	func set_input_dir(new_dir: Unit.Direction) -> void:
		assert(new_dir != _output_dir)
		_input_dir = new_dir
		_world.queue_redraw()

	
	func debug_draw(world: WorldPanel) -> void:
		super(world)
		var my_pos := world.grid_to_pos(_grid_loc)
		var out_edge_pos := (my_pos
			+ (0.5*world.cell_width) * Vector2.ONE
			+ (0.4*world.cell_width) * Unit.Direction_to_grid(_output_dir))
		var in_edge_pos := (my_pos
			+ (0.5*world.cell_width) * Vector2.ONE
			+ (0.4*world.cell_width) * Unit.Direction_to_grid(_input_dir))

		# world.draw_circle(out_edge_pos, world.cell_width * 0.1, Color.BLACK)
		world.draw_circle(out_edge_pos, world.cell_width * 0.1, Color.DARK_GREEN, false)
		# world.draw_circle(in_edge_pos, world.cell_width * 0.1, Color.BLACK)
		world.draw_circle(in_edge_pos, world.cell_width * 0.1, Color.DARK_RED, false)


class TlSlider extends TlHolder:
	var _dir: Unit.Direction

	func _init(world_: WorldPanel, gloc: Vector2i, dir_: Unit.Direction) -> void:
		super(world_, gloc)
		self._dir = dir_
	

	func get_dir() -> Unit.Direction:
		return _dir

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		_world.queue_redraw()


	func can_enter_in_dir(movement_dir: Unit.Direction) -> bool:
		return movement_dir != Unit.Direction_inv(_dir)

	
	func type_to_col() -> Color:
		return Color.LIGHT_PINK


	func debug_draw(world: WorldPanel) -> void:
		super(world)
		var my_pos := world.grid_to_pos(_grid_loc)
		var edge_pos := (my_pos
			+ (0.5*world.cell_width) * Vector2.ONE
			+ (0.4*world.cell_width) * Unit.Direction_to_grid(_dir))
		# world.draw_circle(edge_pos, world.cell_width * 0.1, Color.BLACK)
		world.draw_circle(edge_pos, world.cell_width * 0.1, Color.HOT_PINK, false)
