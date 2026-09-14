using Godot;
using System;

namespace ArFactory;

public partial class EditorSelectMenu : Control
{
	const string LevelsFolderPath = "res://Levels/";

	public Button BackButt;
	public Button AddButt;

	public override void _Ready()
	{
		BackButt = GetNode<Button>("Panel/VBoxContainer/MarginContainer/HBoxContainer/BackButt");
		AddButt = GetNode<Button>("Panel/VBoxContainer/MarginContainer/HBoxContainer/AddButt");

		BackButt.Connect(Button.SignalName.Pressed, Callable.From(OnBackButt_Pressed));
		AddButt.Connect(Button.SignalName.Pressed, Callable.From(OnAddButt_Pressed));
	}

	private void OnBackButt_Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}

	private void OnAddButt_Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/level.tscn");
	}
}