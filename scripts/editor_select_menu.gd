extends Control

const LEVELS_FOLDER_PATH := "res://levels/"


@onready var back_butt: Button = $Panel/VBoxContainer/MarginContainer/HBoxContainer/BackButt
@onready var add_butt: Button = $Panel/VBoxContainer/MarginContainer/HBoxContainer/AddButt


func _ready() -> void:
	back_butt.pressed.connect(_on_back_butt_pressed)
	add_butt.pressed.connect(_on_add_butt_pressed)


func _on_back_butt_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")


func _on_add_butt_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/editor.tscn")
