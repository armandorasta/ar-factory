using Godot;
using System;
using System.Reflection.Metadata;

namespace ArFactory;

public static class SceneManager
{
	public const string LevelScenePath = "res://scenes/Level.tscn";
	public const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
	public const string EditorSelectMenuScenePath = "res://scenes/EditorSelectMenu.tscn";

	public static void MoveToMainMenu(SceneTree tree)
	{
		tree.ChangeSceneToFile(MainMenuScenePath);
	}

	public static void MoveToEditorSelectMenu(SceneTree tree)
	{
		tree.ChangeSceneToFile(EditorSelectMenuScenePath);
	}

	public static void MoveToLevel(SceneTree tree)
	{
		tree.ChangeSceneToFile(LevelScenePath);
	}
}
