extends GutTest

const LevelScene := preload("res://scenes/level.tscn")

var lv: Level = LevelScene.instantiate()


func before_all():
	add_child(lv)


func after_all() -> void:
	remove_child(lv)
	lv.queue_free()


func test_some_in() -> void:
	lv.world._Place_supplier(Vector2i(0, 6), Unit.Direction.EAST, 1, [1, 2, 3])
	assert_is(lv.world.get_unit(Vector2i(0, 6)), UNSupplier)