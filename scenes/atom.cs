using Godot;
using System;
using Classes;
using static Utils;

namespace AtomClass
{
	public partial class atom : Node3D
	{

		// Base atom
		public AtomBase atomBase = new AtomBase();

		// Atom scene attributes
		public Color atomColor { get; set; }
		public StaticBody3D atomStaticBody;
		public CollisionShape3D atomCollision;
		public MeshInstance3D atomMesh;
		public StandardMaterial3D OriginalMaterial;
		public bool highlighted = false;
		public const float sizeNormalization = 5.0f;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			// Load atts.
			atomStaticBody = GetNode<StaticBody3D>("AtomStaticBody");
			if (atomStaticBody == null)
			{
				GD.PrintErr("Atom Static node not found!");
				return;
			}

			atomCollision = GetNode<CollisionShape3D>("AtomStaticBody/AtomCollision");
			if (atomCollision == null)
			{
				GD.PrintErr("Atom Collision node not found!");
				return;
			}

			atomMesh = GetNode<MeshInstance3D>("AtomStaticBody/AtomMesh");
			if (atomMesh == null)
			{
				GD.PrintErr("Atom Mesh node not found!");
				return;
			}

			// Set properties
			var sphereMesh = new SphereMesh();
			sphereMesh.Radius = atomBase.Radius;
			sphereMesh.Height = atomBase.Radius * 2;
			atomMesh.Mesh = sphereMesh;

			var sphereCol = atomCollision.Shape as SphereShape3D;
			sphereCol.Radius = atomBase.Radius;

			var material = new StandardMaterial3D
			{
				AlbedoColor = new Color(atomColor.R, atomColor.G, atomColor.B)
			};

			atomMesh.MaterialOverride = material;
			OriginalMaterial = material;
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void SetPosition(Vector3 position)
		{
			GlobalTransform = new Transform3D(GlobalTransform.Basis, position);
			Position = position;

			atomBase.SetPosition(ConvertToNumericsVector3(position));
		}

		public void SetRadius()
		{
			atomBase.Radius /= sizeNormalization;
		}

		public void Highlight()
		{

			if (!highlighted)
			{

				var material = new StandardMaterial3D
				{
					AlbedoColor = atomColor,
					EmissionEnabled = true,
					Emission = atomColor,
					EmissionEnergyMultiplier = 2.0f,
				};

				atomMesh.MaterialOverride = material;
				highlighted = true;
			}
			else
			{
				atomMesh.MaterialOverride = OriginalMaterial;
				highlighted = false;
			}

		}

		public void SetPreviewMode(bool isPreview)
		{
			if (atomMesh == null || OriginalMaterial == null)
			{
				// If the mesh or material is not ready, we can't set the preview mode yet
				return;
			}

			if (isPreview)
			{
				var material = new StandardMaterial3D
				{
					AlbedoColor = new Color(atomColor.R, atomColor.G, atomColor.B, 0.25f), // Set alpha to 0.5 for transparency
					Transparency = BaseMaterial3D.TransparencyEnum.Alpha
				};

				atomMesh.MaterialOverride = material;
			}
			else
			{
				atomMesh.MaterialOverride = OriginalMaterial;
			}
		}
	}
}
