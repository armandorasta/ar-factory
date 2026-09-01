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


func get_tile(gloc: Vector2i, is_add_if_null := false) -> Tile:
	var my_tile := _tiles[gloc.y * dims.x + gloc.x]
	if is_add_if_null && my_tile == null:
		my_tile = add_tile(gloc)
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


## Returns the tile just added.
func add_tile(gloc: Vector2i) -> Tile:
	assert(!has_tile(gloc))
	var new_tile := Tile.new(self, gloc)
	_tiles[gloc.y * dims.x + gloc.x] = new_tile
	return new_tile


## Adds a tile that was extracted via `extract_tile` back.
func install_tile(tile: Tile, gloc: Vector2i) -> void:
	assert(!has_tile(gloc))
	_tiles[gloc.y * dims.x + gloc.x] = tile
	tile.set_grid_loc_unchecked(gloc)


## Removes the tile from the world, and destroys items within it
func remove_tile(gloc: Vector2i, is_maybe_null := false) -> void:
	assert(is_maybe_null || has_tile(gloc))
	assert(is_within(gloc))

	if !has_tile(gloc):
		return

	var i := gloc.y * dims.x + gloc.x
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
## Use `install_tile` to put it back into the world.
func extract_tile(gloc: Vector2i, is_maybe_null := false) -> Tile:
	assert(is_maybe_null || has_tile(gloc))
	assert(is_within(gloc))

	var i := gloc.y * dims.x + gloc.x
	var my_tile := _tiles[i]
	_tiles[i] = null
	return my_tile


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
		if tile != null && tile.has_item():
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
		if tile != null:
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
				get_tile(tile_gloc).draw_self_on_world(self)
	
	for unit in _units:
		draw_rect(Rect2(grid_to_pos(unit.grid_loc), cell_width*unit.dims), Color.BLACK, false, -2.0)
		draw_string(debug_font, unit.position + Vector2(0.0, 15.0), unit.get_script().get_global_name(), HORIZONTAL_ALIGNMENT_LEFT, cell_width, 16, Color.BLACK)


func _place_some_units() -> void:
	_place_supplier(Vector2i(0, 5), Unit.Direction.EAST, 1, [1, 2, 3])
	_place_slider(Vector2i(2, 6), Unit.Direction.EAST)
	# _place_demander(Vector2i(2, 6), Unit.Direction.WEST, [1, 2, 3])
	
	_place_supplier(Vector2i(4, 5), Unit.Direction.SOUTH, 1, [1, 2, 3])
	_place_slider(Vector2i(5, 7), Unit.Direction.NORTH)
	
	_place_supplier(Vector2i(9, 5), Unit.Direction.WEST, 1, [1, 2, 3])
	_place_slider(Vector2i(8, 6), Unit.Direction.EAST)
	
	_place_supplier(Vector2i(13, 5), Unit.Direction.NORTH, 1, [1, 2, 3])
	_place_slider(Vector2i(14, 4), Unit.Direction.EAST)

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


class Tile:
	enum TileType {
		FREE     = 0x0, # Anything allowed in from any direction.
		INPUT    = 0x1, # Items are allowed to slide in, but not out.
		OUTPUT   = 0x2, # Items are allowed to slide out, but not in.
		SOLID    = 0x4, # Just in the way.
		
		IO       = 0x1 | 0x2, # Both input and output
		SLIDER   = 0x1 | 0x2 | 0x8, # Items are allowed in from 3 directions, out from the 4th.
	}

	var _world: WorldPanel
	var _grid_loc: Vector2i

	var _type: TileType = TileType.FREE
	var _item: Item = null
	
	# Only matters for inputs and outputs.
	# Inputs face where they take in items, outputs face where they push out items.
	# For io tiles, this is the direction of the output.
	var _dir: Unit.Direction = Unit.Direction.EAST

	# Only matters for io tiles
	# Used for the direction of the input.
	var _alt_dir: Unit.Direction = Unit.Direction.EAST
	
	## The makes the tile inaccessible to items even if it's empty.
	var _is_reserved: bool = false


	func _init(world_: WorldPanel, gloc: Vector2i) -> void:
		assert(!world_.has_tile(gloc))
		self._world = world_
		self._grid_loc = gloc


	func get_grid_loc() -> Vector2i:
		return _grid_loc


	## The direction of output in io tiles.
	func get_dir() -> Unit.Direction:
		return _dir


	## The direction of input in io tiles.
	func get_alt_dir() -> Unit.Direction:
		return _alt_dir


	## Is the tile solid? if not are items not allowed there?
	func is_reserved() -> bool:
		return is_solid() || _is_reserved

	# `is_input` and `is_output` are different because one tile can be an input and output at the 
	# same time.

	func is_free()        -> bool: return _type == TileType.FREE
	func is_solid()       -> bool: return _type == TileType.SOLID
	func is_slider()      -> bool: return _type == TileType.SLIDER
	
	func is_io()          -> bool: return _type == TileType.IO
	func is_io_only()     -> bool: return _type == TileType.IO
	

	## IO tiles will return true
	func is_input()       -> bool: return _type &  TileType.INPUT > 0
	
	## IO tiles will return false
	func is_input_only()  -> bool: return _type == TileType.INPUT
	
	## IO tiles will return true
	func is_output()      -> bool: return _type &  TileType.OUTPUT > 0
	
	## IO tiles will return false
	func is_output_only() -> bool: return _type == TileType.OUTPUT


	## `movement_dir` is the direction of the movement, not the direction of the edge of the tile.
	## If an item is sliding in from the west moving east, `Direction.EAST` should passed, not 
	## `Direction.WEST`.
	func can_enter_in_dir(movement_dir: Unit.Direction) -> bool:
		match _type:
			TileType.FREE : return true
			TileType.INPUT: return movement_dir == Unit.Direction_inv(_dir)
			TileType.IO   : return movement_dir == Unit.Direction_inv(_alt_dir)
			_             : return false


	func has_item() -> bool:
		return _item != null


	func get_item(is_maybe_null: bool = false) -> Item:
		assert(is_maybe_null || has_item())
		return _item


	func make_free() -> void:
		_type = TileType.FREE
		_world.queue_redraw()


	func make_solid() -> void:
		_type = TileType.SOLID
		_world.queue_redraw()


	func make_input() -> void:
		_type = TileType.INPUT
		_world.queue_redraw()


	func make_output() -> void: 
		_type = TileType.OUTPUT
		_world.queue_redraw()

	
	## Also sets `alt_dir` to the oppsite of the current direction.
	func make_io() -> void:
		_type = TileType.IO
		set_dir(_dir, true)
		_world.queue_redraw()


	## Use `WorldPanel.move_tile()` instead, this doesn't actually move the tile!
	func set_grid_loc(gloc: Vector2i) -> void:
		assert(!_world.has_tile(gloc))
		set_grid_loc_unchecked(gloc)


	## Use `WorldPanel.move_tile()` instead
	func set_grid_loc_unchecked(gloc: Vector2i) -> void:
		_grid_loc = gloc


	## Cannot set to `alt_dir` if it's an io tile.
	func set_dir(new_dir: Unit.Direction, is_set_alt_to_inv := false) -> void:
		assert(_type & (TileType.INPUT | TileType.OUTPUT) > 0)
		assert(_type != TileType.IO || is_set_alt_to_inv || new_dir != _alt_dir)
		_dir = new_dir
		if is_set_alt_to_inv:
			_alt_dir = Unit.Direction_inv(new_dir)
		_world.queue_redraw()


	func set_alt_dir(new_dir: Unit.Direction) -> void:
		assert(_type == TileType.IO)
		assert(new_dir != _dir)
		_alt_dir = new_dir
		_world.queue_redraw()
	

	func rotate90() -> void:
		assert(_type & (TileType.INPUT | TileType.OUTPUT) > 0)
		set_dir(Unit.Direction_rotate90(_dir))
		if is_io():
			set_alt_dir(Unit.Direction_rotate90(_alt_dir))


	func rotate270() -> void:
		assert(_type & (TileType.INPUT | TileType.OUTPUT) > 0)
		set_dir(Unit.Direction_rotate270(_dir))
		if is_io():
			set_alt_dir(Unit.Direction_rotate270(_alt_dir))


	func set_item(new_item: Item, is_override := false) -> void:
		assert(!is_solid())
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

	
	func set_reserved(to_what: bool) -> void:
		_is_reserved = to_what

	
	func draw_self_on_world(world: WorldPanel) -> void:
		var my_pos := world.grid_to_pos(_grid_loc)
		world.draw_rect(Rect2(my_pos, Vector2.ONE * world.cell_width), _type_to_col())
		if is_output():
			var edge_pos := (my_pos
				+ (0.5*world.cell_width) * Vector2.ONE
				+ (0.4*world.cell_width) * Unit.Direction_to_grid(_dir))
			# world.draw_circle(edge_pos, world.cell_width * 0.1, Color.BLACK)
			world.draw_circle(edge_pos, world.cell_width * 0.1, Color.DARK_GREEN, false)
		
		if is_input():
			var my_dir := _dir if !is_output() else _alt_dir
			var edge_pos := (my_pos
				+ (0.5*world.cell_width) * Vector2.ONE
				+ (0.4*world.cell_width) * Unit.Direction_to_grid(my_dir))
			# world.draw_circle(edge_pos, world.cell_width * 0.1, Color.BLACK)
			world.draw_circle(edge_pos, world.cell_width * 0.1, Color.DARK_RED, false)
			

	func _type_to_col() -> Color:
		match _type:
			TileType.FREE  : return Color.TRANSPARENT
			TileType.INPUT : return Color.RED
			TileType.OUTPUT: return Color.GREEN
			TileType.SOLID : return Color.DARK_GRAY
			TileType.IO    : return Color.YELLOW
		return Color.BLACK
