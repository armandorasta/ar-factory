class_name CmdDemand extends Command
## Same as `CmdAwait`, but checks the value and removes the item if the value matches, otherwise
## supposed to fail the design.

var grid_loc: Vector2i

## Item must have this value, else the design fails.
var value: int

func _init(world_: WorldPanel, gloc: Vector2i, required_val: int) -> void:
	super(world_, 1)
	self.grid_loc = gloc
	self.value = required_val


func on_tick() -> void:
	var my_tile := world.get_tile(grid_loc) as WorldPanel.TlHolder
	if !my_tile.has_item() || !my_tile.get_item().is_stationary():
		pause_this_tick()
		return

	var it_val := my_tile.get_item().get_value()
	if it_val == value:
		print("Expected %d and got it!" % value)
		my_tile.destroy_item()
	else:
		print("Expected %d but got %d instead" % [value, it_val])
		my_tile.destroy_item()
		my_tile.set_reserved(true)
