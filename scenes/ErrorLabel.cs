using Godot;
using System;
using System.Threading.Tasks;

public partial class ErrorLabel : Label
{

	private ColorRect background;

	public async void ShowText()
	{

		background.Size = new Vector2(Size.X + 20, Size.Y + 10);
		background.Position = new Vector2(Position.X - 10, Position.Y - 5);

		// Ensure the label and background are visible
		Visible = true;
		background.Visible = true;
		
		// Set the initial opacity of the label
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1);

		// Wait for 5 seconds
		await Task.Delay(5000);

		// Start fading out
		float fadeDuration = 2.0f; // duration of the fade effect in seconds
		float fadeStep = 0.05f;    // step for each fade iteration
		int iterations = (int)(fadeDuration / fadeStep);

		for (int i = 0; i < iterations; i++)
		{
			// Decrease the opacity gradually
			Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, Modulate.A - (fadeStep / fadeDuration));
			background.Modulate = new Color(background.Modulate.R, background.Modulate.G, background.Modulate.B, 1);
			
			// Wait for the next step
			await Task.Delay((int)(fadeStep * 1000));
		}

		// Ensure the label and background are fully invisible at the end
		Visible = false;
		background.Visible = false;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		Visible = false;
		
		// Create and configure the background ColorRect
		background = new ColorRect();
		background.Color = new Color(0, 0, 0, 0.5f); // Transparent gray
		AddChild(background);
		background.ZIndex = -1; // Ensure the background is behind the label

		// Adjust the background size to match the label size
		background.Size = new Vector2(Size.X + 20, Size.Y + 10);
		background.Position = new Vector2(Position.X - 10, Position.Y - 5);

		background.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
