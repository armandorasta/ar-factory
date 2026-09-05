extends Control

@onready var edit_butt: Button = $HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/EditButt
@onready var play_butt: Button = $HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/PlayButt


func _ready() -> void:
	edit_butt.pressed.connect(_on_edit_butt_pressed)
	play_butt.pressed.connect(_on_play_butt_pressed)


func _on_edit_butt_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/editor_select_menu.tscn")


func _on_play_butt_pressed() -> void:
	assert(false, "No play yet")
