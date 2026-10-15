using Godot;
using System;

namespace ArFactory;

public partial class EditorSelectMenu : Godot.Control
{
	const string LevelsFolderPath = "res://Levels/";

	public Godot.Button BackButt => m_BackButt!;
	public Godot.Button AddButt => m_AddButt!;

	private Godot.Button? m_BackButt;
	private Godot.Button? m_AddButt;

	public override void _Ready()
	{
		m_BackButt = GetNode<Godot.Button>("Panel/VBoxContainer/MarginContainer/HBoxContainer/BackButt");
		m_AddButt = GetNode<Godot.Button>("Panel/VBoxContainer/MarginContainer/HBoxContainer/AddButt");

		m_BackButt.Pressed += OnBackButt_Pressed;
		m_AddButt.Pressed += OnAddButt_Pressed;
	}

	private void OnBackButt_Pressed()
	{
		SceneManager.MoveToMainMenu(GetTree());
	}

	private void OnAddButt_Pressed()
	{
		SceneManager.MoveToLevel(GetTree());
	}
}