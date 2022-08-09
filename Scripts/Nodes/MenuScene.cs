using Godot;
using Godot.Collections;
using System;
using Fifteen.Models;
using Fifteen.Storage;

namespace Fifteen.Nodes
{
    public class MenuScene : Node2D
    {

        public override void _EnterTree()
        {
        }

        public override void _Ready()
        {
        }

        private void OnPolygonButtonPressed(string name)
        {
            var sceneManager = GetNode<SceneManager>("/root/SceneManager");
            GetNode<AudioStreamPlayer>("FxPlayer").Play();

            if (sceneManager.IsTweenActive) return;
            switch (name)
            {
                case "FifteenButton":
                    sceneManager.GoToScene(this, "res://Scenes/MainScene.tscn", Vector2.Left, "res://Scenes/FifteenPuzzle.tscn", sceneManager.Textures, new MenuItem[] {
                        new MenuItem(MenuItemType.Reset, "Reset"),
                        new MenuItem(MenuItemType.SwitchMode, "Switch to pictures", "Switch to numbers"),
                        new MenuItem(MenuItemType.ShowHideReference, "Show reference", "Hide reference")});
                    break;
                case "RotacubeButton":
                        sceneManager.GoToScene(this, "res://Scenes/MainScene.tscn", Vector2.Down, "res://Scenes/Rotacube.tscn", null, new MenuItem[] {
                        new MenuItem(MenuItemType.Reset, "Reset")});
                    break;
                case "SettingsButton":
                    sceneManager.GoToScene(this, "res://Scenes/MainScene.tscn", Vector2.Up, "res://Scenes/Settings.tscn", null, null, true, "Options");
                    break;
            }
        }
    }
}
