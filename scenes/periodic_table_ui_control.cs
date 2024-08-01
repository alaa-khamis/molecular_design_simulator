using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using Classes;
using Atom = AtomClass.atom;


public partial class periodic_table_ui_control : Control
{   

	private GridContainer grid;

	public override void _Ready()
	{
		// Get grid
		grid = GetNode<GridContainer>("Grid");
		grid.Columns = 5;
	}
	public void Setup(List<AtomBase> elements) {
		
		// Setup buttons for each element
		foreach(AtomBase element in elements){
			Button button = new Button
			{
				Text = element.ElementSymbol,
				SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
				SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
				CustomMinimumSize = new Vector2(80, 80),
			};


			// Create a new theme override for the label
			Theme theme = new Theme();
			theme.DefaultFontSize = (int) (GetThemeDefaultFontSize() * 0.75);	

			Label label = new Label 
			{
				Text = element.AtomicNumber.ToString(),
				Theme = theme
			};

			button.AddChild(label);
			grid.AddChild(button);
		}
	}
}
