extends GutTest

const LevelScene := preload("res://scenes/level.tscn")

var lv: CS.Level = LevelScene.instantiate()


func should_skip_script():
	return false


func before_all():
	add_child(lv)


func before_each():
	pass


func after_each():
	pass


func after_all() -> void:
	remove_child(lv)
	lv.queue_free()


func test_some_in() -> void:
	lv.World._Place_supplier(Vector2i(0, 6), Unit.Direction.EAST, 1, [1, 2, 3])
	assert_is(lv.World.get_unit(Vector2i(0, 6)), UNSupplier)