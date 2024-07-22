using Godot;
using System;
using Classes;

namespace AtomClass
{
	public partial class GodotAtom : Node3D, Atom
	{

		// Atom attributes
		public float atomRadius { get; set; }
		public string ElementSymbol { get; set; }
		public string ElementName { get; set; }
		public int AtomicNumber { get; set; }
		public float Mass { get; set; }
		public float CovalentRadius { get; set; }
		public float VanDerWaalsRadius { get; set; }

		// Atom scene attributes
		public Color atomColor {get; set;}
		public StaticBody3D atomStaticBody;
		public CollisionShape3D atomCollision;
		public MeshInstance3D atomMesh;
		public StandardMaterial3D OriginalMaterial;
		public bool highlighted = false;
		public const float sizeNormalization  = 5.0f;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{

			atomRadius = VanDerWaalsRadius / sizeNormalization;

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
			sphereMesh.Radius = atomRadius;
			sphereMesh.Height = atomRadius * 2;
			atomMesh.Mesh = sphereMesh;

			var sphereCol = atomCollision.Shape as SphereShape3D;
			sphereCol.Radius = atomRadius;

			var material = new StandardMaterial3D
			{
				AlbedoColor = atomColor,
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

		public void CopyData(GodotAtom currAtom){
			ElementSymbol = currAtom.ElementSymbol;
			ElementName = currAtom.ElementName;
			AtomicNumber = currAtom.AtomicNumber;
			Mass = currAtom.Mass;
			CovalentRadius = currAtom.CovalentRadius;
			VanDerWaalsRadius = currAtom.VanDerWaalsRadius;
			atomColor = currAtom.atomColor;
		}
    }
}
