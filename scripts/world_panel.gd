class_name WorldPanel extends Panel

const ItemScene := preload("res://scenes/item.tscn")
const UNSliderScene := preload("res://scenes/un_slider.tscn")
const UNSupplierScene := preload("res://scenes/un_supplier.tscn")
const UNUpdaterScene := preload("res://scenes/un_updater.tscn")
const UNBinScene := preload("res://scenes/un_bin.tscn")
const UNDemanderScene := preload("res://scenes/un_demander.tscn")
const UNCombinerScene := preload("res://scenes/un_combiner.tscn")
const UNClonerScene := preload("res://scenes/un_cloner.tscn")

var debug_font: Font = load("res://resources/fonts/AnonymousPro-Regular.ttf")

var dims := Vector2i(15, 10)
var cell_width: float = 100.0

var _units: Array[Unit] = []
var _tiles: Array[Tile] = []
var blocked_slide_cmds: Array[CmdSlide] = []


func has_tile(gloc: Vector2i) -> bool:
	return is_within(gloc) && _tiles[grid_to_index(gloc)] != null


func get_tile(gloc: Vector2i) -> Tile:
	return null if !is_within(gloc) else _tiles[grid_to_index(gloc)]


func get_unit(gloc: Vector2i) -> Unit:
	for u in _units:
		if u.has_tile(gloc):
			return u
	return null


func is_within(gloc: Vector2i) -> bool:
	return Rect2i(Vector2i(), dims).has_point(gloc)


func grid_to_pos(gloc: Vector2i) -> Vector2:
	return cell_width * gloc
	# return cell_width * Vector2(gloc.x + 0.5, gloc.y + 0.5)


func pos_to_grid(pos: Vector2) -> Vector2i:
	return Vector2i(pos / cell_width)


func grid_to_index(gloc: Vector2i) -> int:
	return gloc.y * dims.x + gloc.x


func index_to_grid(i: int) -> Vector2i:
	return Vector2i(i % dims.x, i / dims.x)


func item_can_slide(item_g_loc: Vector2i, item_dir: Unit.Direction) -> bool:
	var dest_tile := get_tile(item_g_loc + Unit.Direction_to_grid(item_dir))
	return !dest_tile.is_reserved() && !dest_tile.has_wall(Unit.Direction_inv(item_dir))


## Adds the passed tile to the world, the tile has to be within boundaries!
## `is_override` can be used to override an existing tile in the same location, the old tile will be
## sent to oblivion and not returned.
func install_tile(tile: Tile, is_override := false) -> Tile:
	var tile_gloc := tile.get_grid_loc()
	assert(is_within(tile_gloc))
	assert(is_override || !has_tile(tile_gloc))

	remove_tile(tile_gloc, !is_override)
	_tiles[grid_to_index(tile_gloc)] = tile
	tile.needs_redraw.connect(_on_tile_needs_redraw)
	return tile


## Removes the tile from the world, and destroys items within it
func remove_tile(gloc: Vector2i, is_maybe_null := false) -> void:
	assert(is_within(gloc))
	assert(is_maybe_null || has_tile(gloc))

	if !has_tile(gloc):
		return

	var i := grid_to_index(gloc)
	if _tiles[i] is TlHolder:
		_tiles[i].destroy_item(true)
	_tiles[i] = null


## Returns the moved tile.
## Moves the tile from one location to another, along with the items it contains.
## Same as `extract_tile` followed by `install_tile`.
func move_tile(gfrom: Vector2i, gto: Vector2i, is_maybe_null := false, is_override := false) -> Tile:
	assert(is_maybe_null || has_tile(gfrom))
	assert(is_override || gfrom == gto || !has_tile(gto))
	assert(is_within(gfrom))
	assert(is_within(gto))
	
	if gfrom == gto: # Moving something nowhere?
		return
	
	var my_tile := extract_tile(gfrom, is_maybe_null)
	my_tile.set_grid_loc_unchecked(gto)
	install_tile(my_tile, is_override)
	return my_tile


## Removes the tile from the world, and returns it.
func extract_tile(gloc: Vector2i, is_maybe_null := false) -> Tile:
	assert(is_maybe_null || has_tile(gloc))
	assert(is_within(gloc))

	var i := grid_to_index(gloc)
	var my_tile := _tiles[i]
	_tiles[i] = null
	my_tile.needs_redraw.disconnect(_on_tile_needs_redraw)
	return my_tile


## Swaps two tiles, or one tile with itself.
func swap_tiles(gloc1: Vector2i, gloc2: Vector2i, is_maybe_null := false) -> void:
	assert(is_maybe_null || has_tile(gloc1) && has_tile(gloc2))
	assert(is_within(gloc1))
	assert(is_within(gloc2))
	
	if gloc1 == gloc2:
		return

	var tl1 := extract_tile(gloc1, is_maybe_null)
	var tl2 := extract_tile(gloc2, is_maybe_null)
	if tl1 != null: 
		tl1.set_grid_loc_unchecked(gloc2)
		install_tile(tl1)
	if tl2 != null: 
		tl2.set_grid_loc_unchecked(gloc1)
		install_tile(tl2)


## Returns the item just added.
func spawn_item(gloc: Vector2i, value: int) -> Item:
	var my_tile := get_tile(gloc)
	assert(!my_tile.is_reserved()) # Might have to change this to `has_item`, but who knows.
	var new_item := ItemScene.instantiate()
	add_child(new_item)
	new_item.setup(self, gloc, value)
	my_tile.set_item_unchecked(new_item)
	return new_item


## Returns the clone, not the original
func clone_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	assert(is_maybe_null || get_tile(g_from).has_item())
	assert(is_override || !get_tile(g_to).has_item())
	return spawn_item(g_to, get_tile(g_from).get_item().get_value())


## Returns the teleported item.
func teleport_item(g_from: Vector2i, g_to: Vector2i, is_maybe_null: bool = false, 
	is_override: bool = false
) -> Item:
	var src_tile := get_tile(g_from)
	var dest_tile := get_tile(g_to)
	assert(is_maybe_null || src_tile.has_item())
	assert(is_override || !dest_tile.has_item())
	dest_tile.set_item_unchecked(src_tile.extract_item())
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


func clean_up() -> void:
	for u in _units:
		u.reset()
	
	for tile in _tiles:
		if tile is TlHolder:
			tile.destroy_item(true)
	
	blocked_slide_cmds.clear()


func _ready() -> void:
	size = cell_width * dims

	for i in dims.x * dims.y:
		_tiles.push_back(null)

	_place_some_units()


func _draw() -> void:
	draw_rect(Rect2(Vector2(), size), Color(0, 0.1, 0))

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
		draw_rect(Rect2(grid_to_pos(unit.grid_loc), cell_width*unit._dims), Color.BLACK, false, -2.0)
		draw_string(debug_font, unit.position + Vector2(0.0, 15.0), unit.get_script().get_global_name(), HORIZONTAL_ALIGNMENT_LEFT, cell_width, 16, Color.BLACK)


func _place_some_units() -> void:
	_place_supplier(Vector2i(0, 5), Unit.Direction.SOUTH, 1, [1, 2, 3])
	_place_slider(Vector2i(1, 7), Unit.Direction.EAST)
	_place_slider(Vector2i(2, 7), Unit.Direction.EAST)
	_place_slider(Vector2i(3, 7), Unit.Direction.EAST)
	_place_cloner(Vector2i(4, 6), Unit.Direction.NORTH, 2)
	_place_slider(Vector2i(4, 7), Unit.Direction.NORTH)
	_place_slider(Vector2i(4, 5), Unit.Direction.NORTH)
	_place_slider(Vector2i(4, 4), Unit.Direction.NORTH)
	_place_slider(Vector2i(5, 5), Unit.Direction.NORTH)
	_place_slider(Vector2i(5, 4), Unit.Direction.NORTH)
	_place_combiner(Vector2i(4, 2), Unit.Direction.NORTH, 2, UNCombiner.Operation.ADD)
	_place_slider(Vector2i(5, 1), Unit.Direction.EAST)
	_place_slider(Vector2i(6, 1), Unit.Direction.EAST)
	_place_demander(Vector2i(7, 1), Unit.Direction.WEST, [2, 4, 6])
	# _place_supplier(Vector2i(0, 9), Unit.Direction.NORTH, 1, [1, 2, 3])
	# _place_slider(Vector2i(1, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(2, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(3, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(4, 8), Unit.Direction.EAST)
	# _place_slider(Vector2i(5, 8), Unit.Direction.NORTH)
	# _place_slider(Vector2i(5, 7), Unit.Direction.NORTH)
	# _place_combiner(Vector2i(4, 5), Unit.Direction.NORTH, 5, UNCombiner.Operation.DIV)
	# _place_slider(Vector2i(5, 4), Unit.Direction.NORTH)
	# _place_slider(Vector2i(5, 3), Unit.Direction.NORTH)
	# _place_bin(Vector2i(9, 8))

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
	

func _place_supplier(gloc: Vector2i, dir: Unit.Direction, rate: int, seq: Array[int]) -> void:
	var my_supp := UNSupplierScene.instantiate()
	_units.push_back(my_supp)
	add_child(my_supp)
	my_supp.setup(self, gloc, rate, seq)
	my_supp.set_dir(dir)


func _place_slider(gloc: Vector2i, dir: Unit.Direction) -> void:
	var my_bus := UNSliderScene.instantiate()
	_units.push_back(my_bus)
	add_child(my_bus)
	my_bus.setup(self, gloc, 1)
	my_bus.set_dir(dir)


func _place_updater(gloc: Vector2i, dir: Unit.Direction, rate: int, up_t: 
	UNUpdater.UpdateType
) -> void:
	var doub := UNUpdaterScene.instantiate()
	_units.push_back(doub)
	add_child(doub)
	doub.setup(self, gloc, rate, up_t)
	doub.set_dir(dir)


func _place_bin(gloc: Vector2i) -> void:
	var bin := UNBinScene.instantiate()
	_units.push_back(bin)
	add_child(bin)
	bin.setup(self, gloc)


func _place_demander(gloc: Vector2i, dir: Unit.Direction, seq: Array[int]) -> void:
	var dem := UNDemanderScene.instantiate()
	_units.push_back(dem)
	add_child(dem)
	dem.setup(self, gloc, seq)
	dem.set_dir(dir)


func _place_combiner(gloc: Vector2i, dir: Unit.Direction, rate: int, op: UNCombiner.Operation) -> void:
	var comb := UNCombinerScene.instantiate()
	_units.push_back(comb)
	add_child(comb)
	comb.setup(self, gloc, rate, op)
	comb.set_dir(dir)


func _place_cloner(gloc: Vector2i, dir: Unit.Direction, rate: int) -> void:
	var cloner := UNClonerScene.instantiate()
	_units.push_back(cloner)
	add_child(cloner)
	cloner.setup(self, gloc, rate)
	cloner.set_dir(dir)



## Called after handling all ticks of pending commands
func _handle_slide_cmd_overlapping() -> void:
	# Slide command overlapping.
	# If any update happens in a pass, end that pass and start over,
	# repeat until a full pass happens with no updates.
	while 1 + 1 == 2:
		var dirty_index := -1
		for i in blocked_slide_cmds.size():
			if blocked_slide_cmds[i].is_updated_this_pass(self):
				dirty_index = i
				break
		if dirty_index == -1: # None has updated? it's over.
			break
		# Something moved? can only move once per tick so bye-bye.
		blocked_slide_cmds.pop_at(dirty_index)
	blocked_slide_cmds.clear()


## Silly little function... Needed because disconnection requires a reference to the callable, 
## otherwise I would have to disconnect everything, which I feel might bite me in the butt later.
func _on_tile_needs_redraw() -> void:
	queue_redraw()


#region Tile CLASSES

@abstract class Tile:
	const DEFAULT_DIR: Unit.Direction = Unit.Direction.EAST

	signal needs_redraw()

	var _grid_loc: Vector2i

	func _init(gloc: Vector2i) -> void:
		self._grid_loc = gloc


	#region TILE ABSTRACT

	## `movement_dir` is the direction of the movement, not the direction of the edge of the tile.
	## If an item is sliding in from the west moving east, `movement_dir` will be `Direction.EAST`, 
	## not `Direction.WEST`.
	@abstract func can_item_enter_in_dir(item: Item, movement_dir: Unit.Direction) -> bool

	@abstract func has_input_in_dir(dir: Unit.Direction) -> bool
	@abstract func has_output_in_dir(dir: Unit.Direction) -> bool

	## Used for debug drawing.
	@abstract func type_to_col() -> Color

	#endregion


	#region SHORTHANDS
	# Normally these would need you to down-cast, so instead they all return false or do nothing
	# saving you a line or two.
	
	## Items are not allowed to slide into reserved tiles.
	func is_reserved() -> bool: return true
	func set_reserved(_to_what: bool) -> void: pass

	## For `TlIO` this is an alias for `get_output_dir`.
	func get_dir() -> Unit.Direction: 
		return DEFAULT_DIR
	
	## For `TlIO` this is an alias for `set_output_dir`.
	## This forces a redraw no matter what.
	func set_dir(_new_dir: Unit.Direction) -> void: 
		needs_redraw.emit()
	
	## Rotates the unit 90 degrees clockwise. `rotate270` just calls this 3 times.
	func rotate90() -> void:
		set_dir(Unit.Direction_rotate90(get_dir()))
	
	## Implemented in terms of `rotate90` by default, this way I only need to implement 1 instead of
	## 2 functions.
	func rotate270() -> void:
		rotate90()
		rotate90()
		rotate90()
	
	## Shorthand for `tile is WorldPanel.TlHolder && tile.has_item()`.
	func has_item() -> bool: return false
	
	## Always returns null for tiles that are not `TlHolder`
	func get_item() -> Item: return null

	# No set_item of course, adding that would have caused so many disasters, god damn...

	#endregion


	#region TILE NORMAL
	# These are never or rarely overriden by sub classes

	func get_grid_loc() -> Vector2i:
		return _grid_loc


	## Use this if the tile is floating, otherwise use `WorldPanel.move_tile()` instead
	func set_grid_loc_unchecked(gloc: Vector2i) -> void:
		_grid_loc = gloc

	
	func debug_draw(world_: WorldPanel) -> void:
		var my_pos := world_.grid_to_pos(_grid_loc)
		world_.draw_rect(Rect2(my_pos, Vector2.ONE * world_.cell_width), type_to_col())
		for d in Unit.Direction.LAST + 1:
			var gdir := Unit.Direction_to_grid(d)
			if !(has_output_in_dir(d) || has_input_in_dir(d)):
				continue

			var edge_pos := my_pos + world_.cell_width * (0.5 * Vector2.ONE + 0.4 * gdir)
			var my_col := Color.DARK_RED if has_input_in_dir(d) else Color.DARK_GREEN
			
			var my_scale := 0.15
			if has_input_in_dir(d) && !(
				world_.has_tile(_grid_loc + gdir) &&
				world_.get_tile(_grid_loc + gdir).has_output_in_dir(Unit.Direction_inv(d))
			): # Unconnected input?
				my_scale *= 0.5
				my_col.a = 0.3

			var w := world_.cell_width * my_scale
			world_.draw_rect(Rect2(edge_pos - w*0.5 * Vector2.ONE, w*Vector2.ONE), my_col, false)
			# world.draw_circle(edge_pos, world.cell_width * my_scale, my_col, false)

	#endregion


@abstract class TlHolder extends Tile:
	var _item: Item = null
	var _is_reserved: bool = false


	func is_reserved() -> bool:
		return _is_reserved
	

	func set_reserved(to_what: bool) -> void:
		_is_reserved = to_what


	func has_item() -> bool:
		return _item != null


	func get_item(is_maybe_null: bool = false) -> Item:
		assert(is_maybe_null || has_item())
		return _item
	

	## Never use this directly! use `WorldPanel.spawn_item`
	func set_item_unchecked(new_item: Item, is_override := false) -> void:
		assert(new_item.get_parent() is WorldPanel)
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
	func is_reserved() -> bool:return true
	func set_reserved(_to_what: bool) -> void: pass
	func type_to_col() -> Color: return Color.DARK_GRAY
	func can_item_enter_in_dir(_item: Item, _movement_dir: Unit.Direction) -> bool: return false
	func has_input_in_dir(_dir: Unit.Direction) -> bool: return false
	func has_output_in_dir(_dir: Unit.Direction) -> bool: return false

	

class TlInput extends TlHolder:
	var _dir: Unit.Direction = Unit.Direction.EAST


	func _init(gloc: Vector2i, dir_ := DEFAULT_DIR) -> void:
		super(gloc)
		self._dir = dir_


	func type_to_col() -> Color: 
		return Color.INDIAN_RED
	
	
	func get_dir() -> Unit.Direction:
		return _dir	

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		needs_redraw.emit()


	func can_item_enter_in_dir(_it: Item, movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_dir)

	
	func has_output_in_dir(_d: Unit.Direction) -> bool: return false
	func has_input_in_dir(dir_: Unit.Direction) -> bool:
		return dir_ == self._dir
		

class TlOutput extends TlHolder:
	var _dir: Unit.Direction = Unit.Direction.EAST


	func _init(gloc: Vector2i, dir_ := DEFAULT_DIR) -> void:
		super(gloc)
		self._dir = dir_


	func type_to_col() -> Color: 
		return Color.LIGHT_GREEN

	
	func get_dir() -> Unit.Direction:
		return _dir	

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		needs_redraw.emit()


	func can_item_enter_in_dir(_it: Item, movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_dir)


	func has_input_in_dir(_d: Unit.Direction) -> bool: return false
	func has_output_in_dir(dir_: Unit.Direction) -> bool:
		return dir_ == self._dir


class TlIO extends TlHolder:
	var _output_dir: Unit.Direction
	var _input_dir: Unit.Direction


	func _init(gloc: Vector2i, out_dir: Unit.Direction, in_dir: Unit.Direction) -> void:
		super(gloc)
		assert(out_dir != in_dir)
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


	func can_item_enter_in_dir(_it: Item, movement_dir: Unit.Direction) -> bool: 
		return movement_dir == Unit.Direction_inv(_input_dir)

	
	func has_input_in_dir(dir_: Unit.Direction) -> bool:
		return dir_ == _input_dir


	func has_output_in_dir(dir_: Unit.Direction) -> bool:
		return dir_ == _output_dir
	

	func get_output_dir() -> Unit.Direction:
		return _output_dir
	
	
	func get_input_dir() -> Unit.Direction:
		return _input_dir

	
	func set_output_dir(new_dir: Unit.Direction, is_set_in_to_inv := false) -> void:
		assert(is_set_in_to_inv || new_dir != _input_dir)
		_output_dir = new_dir
		if is_set_in_to_inv:
			_input_dir = Unit.Direction_inv(new_dir)
		needs_redraw.emit()


	func set_input_dir(new_dir: Unit.Direction) -> void:
		assert(new_dir != _output_dir)
		_input_dir = new_dir
		needs_redraw.emit()


class TlSlider extends TlHolder:
	var _dir: Unit.Direction

	func _init(gloc: Vector2i, dir_: Unit.Direction) -> void:
		super(gloc)
		self._dir = dir_
	

	func get_dir() -> Unit.Direction:
		return _dir

	
	func set_dir(new_dir: Unit.Direction) -> void:
		_dir = new_dir
		needs_redraw.emit()


	func can_item_enter_in_dir(_it: Item, movement_dir: Unit.Direction) -> bool: 
		return movement_dir != Unit.Direction_inv(_dir)


	func has_input_in_dir(dir: Unit.Direction) -> bool:
		return dir != self._dir

	
	func has_output_in_dir(dir: Unit.Direction) -> bool:
		return dir == self._dir

	
	func type_to_col() -> Color:
		return Color.LIGHT_PINK


class TlBlackhole extends TlHolder:
	func _init(gloc: Vector2i) -> void:
		super(gloc)
	

	func can_item_enter_in_dir(_it: Item, _movement_dir: Unit.Direction) -> bool:
		## Can enter from any direction
		return true

	
	## All directions.
	func has_input_in_dir(_dir: Unit.Direction) -> bool: return true
	## No outputs.
	func has_output_in_dir(_dir: Unit.Direction) -> bool: return false

	
	func type_to_col() -> Color:
		return Color.BROWN

#endregion
