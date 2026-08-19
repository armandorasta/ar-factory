class_name CommandGroup

var _cmds: Array[Command] = []


func _init(seq: Array[Command] = []) -> void:
	self._cmds = seq


func size() -> void:
	return _cmds.size()


## Returns a direct reference to the underlying array, these are supposed to execute first to last
## in-order!
func get_cmds() -> Array[Command]:
	return _cmds


func pop_front() -> Command:
	assert(!_cmds.is_empty())
	return _cmds.pop_front()


func push_back(new_cmd: Command) -> void:
	_cmds.push_back(new_cmd)


func on_tick() -> void:
	for cmd in _cmds:
		cmd.on_tick()


func count_this_tick() -> void:
	for cmd in _cmds:
		cmd.count_this_tick()