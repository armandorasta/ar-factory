class_name CmdCondSlide extends Command

var Check_callable: Callable
var Src_grid_loc: Vector2i

var True_grid_loc: Vector2i
var True_slide_dir: Unit.Direction

var False_grid_loc: Vector2i
var False_slide_dir: Unit.Direction

var _Slide_cmd: CmdSlide
var _State_machine := StateMachine.new(_Handle_default)


static func from_tiles(src_tl: WorldPanel.TlHolder, true_tl: WorldPanel.TlHolder, 
		true_dir: Unit.Direction, false_tl: WorldPanel.TlHolder, false_dir: Unit.Direction, 
		what_to_do: Callable) -> CmdCondSlide:
	return CmdCondSlide.new(src_tl.get_grid_loc(), true_tl.get_grid_loc(), true_dir, 
		false_tl.get_grid_loc(), false_dir, what_to_do)


## [param what_to_do] takes the tile at [param src_loc] and returns a bool.
func _init(src_loc: Vector2i, gtrue: Vector2i, true_dir: Unit.Direction, gfalse: Vector2i, 
		false_dir: Unit.Direction, what_to_do: Callable) -> void:
	super(1)
	self.Src_grid_loc = src_loc
	self.True_grid_loc = gtrue
	self.True_slide_dir = true_dir
	self.False_grid_loc = gfalse
	self.False_slide_dir = false_dir
	self.Check_callable = what_to_do


func on_tick(lv: Level) -> void:
	_State_machine.call_next_state(lv)


func do_per_frame(dt: float, lv: Level) -> void:
	if _Slide_cmd != null:
		_Slide_cmd.do_per_frame(dt, lv)


func _Handle_default(lv: Level) -> void:
	assert(lv.world.get_tile(Src_grid_loc) is WorldPanel.TlHolder)
	assert(lv.world.get_tile(True_grid_loc) is WorldPanel.TlHolder)
	assert(lv.world.get_tile(False_grid_loc) is WorldPanel.TlHolder)
	assert(_Slide_cmd == null)

	var src_tile := lv.world.get_tile(Src_grid_loc) as WorldPanel.TlHolder
	if !src_tile.has_item() || src_tile.get_item().is_mid_animation(): 
		pause_this_tick()
		return

	if Check_callable.call(src_tile):
		lv.world.teleport_item(src_tile.get_grid_loc(), True_grid_loc)
		_Slide_cmd = CmdSlide.new(True_grid_loc, True_slide_dir)
	else:
		lv.world.teleport_item(src_tile.get_grid_loc(), False_grid_loc)
		_Slide_cmd = CmdSlide.new(False_grid_loc, False_slide_dir)
	
	_State_machine.set_state_and_call(_Handle_slide, lv)


func _Handle_slide(lv: Level) -> void:
	assert(_Slide_cmd != null)
	_Slide_cmd.on_tick(lv)
	_Slide_cmd.count_this_tick()
	if !_Slide_cmd.is_done():
		pause_this_tick()
		return

	_Slide_cmd = null
	_State_machine.set_state(_Handle_default)


	


	
	
