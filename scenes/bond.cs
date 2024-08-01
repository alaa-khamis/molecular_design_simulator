using Godot;
using System;
using Atom = AtomClass.atom;
using Classes;
using static Utils;

namespace BondClass
{
	public partial class bond : Node3D
	{
		public StaticBody3D bondStaticBody;
		public CollisionShape3D bondCollision;
		public MeshInstance3D bondMesh;

		public BondBase bondBase = new BondBase();

		public void CreateBond(AtomBase atom1, AtomBase atom2){
			bondBase = new BondBase(atom1, atom2);
		}

		public void UpdateBond()
		{
			Vector3 startPos = ConvertToGodotVector3(bondBase.Atom1.Position);
			Vector3 endPos = ConvertToGodotVector3(bondBase.Atom2.Position);
			Vector3 bondVec = endPos - startPos;
			float length = bondVec.Length();

			// Create a unique mesh for this bond
			var cylinderMesh = new CylinderMesh();
			cylinderMesh.TopRadius = 0.05f;
			cylinderMesh.BottomRadius = 0.05f;
			bondMesh.Mesh = cylinderMesh;

			Vector3 axis = bondVec.Normalized();
			Vector3 up = new Vector3(0, 1, 0);
			Basis rotationBasis = Basis.LookingAt(axis, up);

			GlobalTransform = new Transform3D(rotationBasis, startPos + bondVec / 2);

			AdjustCylinder(startPos, endPos, length);
		}

		public void AdjustCylinder(Vector3 start, Vector3 end, float length)
		{
			Vector3 bondVec = end - start;
			Vector3 midpoint = (start + end) / 2;

			var cylMesh = bondMesh.Mesh as CylinderMesh;
			if (cylMesh != null)
			{
				cylMesh.Height = length;
			}

			// Calculate the rotation to align the cylinder with the bond vector
			Vector3 axis = bondVec.Normalized();
			Vector3 up = new Vector3(0, 1, 0);
			Basis rotationBasis;

			if (axis != up && axis != -up)
			{
				Vector3 right = up.Cross(axis).Normalized();
				Vector3 newUp = axis.Cross(right);
				rotationBasis = new Basis(right, axis, newUp);
			}
			else
			{
				rotationBasis = axis.Y > 0 ? Basis.Identity : Basis.FromEuler(new Vector3(Mathf.Pi, 0, 0));
			}

			// Set the global transform to position and align the cylinder
			GlobalTransform = new Transform3D(rotationBasis, midpoint);
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{

			bondMesh = GetNode<MeshInstance3D>("bondMesh");
			if (bondMesh == null)
			{
				GD.PrintErr("Bond Mesh node not found!");
				return;
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			UpdateBond();
		}
	}
}
