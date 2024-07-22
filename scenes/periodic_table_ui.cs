using Godot;
using System;

public partial class periodic_table_ui : StaticBody3D
{

	private MeshInstance3D displayMesh;
	private SubViewport viewport;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		displayMesh = GetNode<MeshInstance3D>("DisplayMesh");
		viewport = GetNode<SubViewport>("SubViewport");

		var material = new StandardMaterial3D
		{
			AlbedoTexture = viewport.GetTexture(),
			ResourceLocalToScene = true
		};

		displayMesh.MaterialOverride = material;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
