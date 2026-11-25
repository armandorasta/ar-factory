using Godot;
using System;
using System.Reflection.Metadata;

namespace ArFactory;

public static class SceneManager
{
	#region .Constants
		
	public const string EditorScenePath = "res://scenes/Editor.tscn";
	public const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
	public const string EditorSelectMenuScenePath = "res://scenes/EditorSelectMenu.tscn";

	#endregion // .Constants
	#region .Transitions
		
	public static void MoveToMainMenu(Godot.SceneTree tree)
	{
		tree.ChangeSceneToFile(MainMenuScenePath);
	}

	public static void MoveToEditorSelectMenu(Godot.SceneTree tree)
	{
		tree.ChangeSceneToFile(EditorSelectMenuScenePath);
	}

	public static void MoveToEditor(Godot.SceneTree tree)
	{
		var editor = GD.Load<Godot.PackedScene>(EditorScenePath).Instantiate<Editor>();
		ChangeSceneToNodeImmediately(tree, editor);
		editor.Deserialize(Editor.AutoSaveFilePath);
	}

	#endregion // .Transitions
	#region .Helpers
		
	private static void ChangeSceneToNodeImmediately(Godot.SceneTree tree, Godot.Node sceneNode)
	{
		var currScene = tree.CurrentScene;
		tree.Root.RemoveChild(currScene);
		currScene.QueueFree();
		tree.Root.AddChild(sceneNode);
	}

	private static void ChangeSceneToPackedImmediately(Godot.SceneTree tree, Godot.PackedScene newScene)
	{
		ChangeSceneToNodeImmediately(tree, newScene.Instantiate());
	}

	#endregion // .Helpers
}
