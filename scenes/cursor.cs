using Godot;
using System;
using System.Drawing;

public partial class cursor : CanvasLayer
{

	private Label cursorLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		cursorLabel = GetNode<Label>("Label");

		var window_size = GetWindow().Size;
		var center = window_size / 2;

		cursorLabel.Size = new Vector2(1, 1);

		cursorLabel.Position = new Vector2(center.X - cursorLabel.Size.X / 2, center.Y - cursorLabel.Size.Y / 2);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
