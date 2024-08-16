using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Classes;
using static Utils;

public static class ZMatrixUtils
{
	// Parse Z-Matrix
	public static Tuple<List<AtomBase>, List<BondBase>> ParseZMatrix(string zMatrix)
	{

		List<AtomBase> elements = LoadElementsFromJSON();

		List<AtomBase> atoms = new List<AtomBase>();
		List<BondBase> bonds = new List<BondBase>();

		// Split the Z-matrix into lines
		var lines = zMatrix.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

		// List to keep track of positions
		List<Vector3> positions = new List<Vector3>();

		// Create atom list
		for (int i = 0; i < lines.Length; i++)
		{
			var tokens = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string elementSymbol = tokens[0];

			AtomBase element = elements.Find(element => element.ElementSymbol == elementSymbol);

			// Create the atom with default properties
			AtomBase atom = new AtomBase();
			atom.CopyData(element);

			// Calculate the position based on the Z-matrix information
			Vector3 position = CalculateAtomPosition(i, tokens, positions);
			atom.SetPosition(position);
			positions.Add(position);

			atoms.Add(atom);

		}

		// Create bond list
		for (int i = 1; i < lines.Length; i++)
		{
			var tokens = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int bondAtomIndex = int.Parse(tokens[1]) - 1;

			BondBase bond = new BondBase(atoms[i], atoms[bondAtomIndex]);
			bonds.Add(bond);
		}

		return new Tuple<List<AtomBase>, List<BondBase>>(atoms, bonds);
	}

	private static Vector3 CalculateAtomPosition(int atomIndex, string[] tokens, List<Vector3> positions)
	{
		if (atomIndex == 0)
		{
			// First atom is at the origin
			return Vector3.Zero;
		}
		else if (atomIndex == 1)
		{
			// Second atom: Bond length from the first atom
			int bondAtomIndex = int.Parse(tokens[1]) - 1;
			float bondLength = float.Parse(tokens[2]);
			return positions[bondAtomIndex] + new Vector3(bondLength, 0, 0);
		}
		else if (atomIndex == 2)
		{
			// Third atom: Bond length and angle
			int bondAtomIndex = int.Parse(tokens[1]) - 1;
			float bondLength = float.Parse(tokens[2]);
			int angleAtomIndex = int.Parse(tokens[3]) - 1;
			float angle = float.Parse(tokens[4]);

			Vector3 bondDir = Vector3.Normalize(positions[bondAtomIndex] - positions[angleAtomIndex]);
			Vector3 perpendicularDir = Vector3.Cross(bondDir, Vector3.UnitY);
			if (perpendicularDir == Vector3.Zero)
			{
				perpendicularDir = Vector3.Cross(bondDir, Vector3.UnitX);
			}
			return positions[bondAtomIndex] + bondLength * (float)Math.Cos(DegToRad(angle)) * bondDir + bondLength * (float)Math.Sin(DegToRad(angle)) * perpendicularDir;
		}
		else
		{
			// For other atoms: Bond length, angle, and dihedral
			int bondAtomIndex = int.Parse(tokens[1]) - 1;
			float bondLength = float.Parse(tokens[2]);
			int angleAtomIndex = int.Parse(tokens[3]) - 1;
			float angle = float.Parse(tokens[4]);
			int dihedralAtomIndex = int.Parse(tokens[5]) - 1;
			float dihedral = float.Parse(tokens[6]);

			Vector3 bondDir = Vector3.Normalize(positions[bondAtomIndex] - positions[angleAtomIndex]);
			Vector3 normalDir = Vector3.Normalize(Vector3.Cross(positions[angleAtomIndex] - positions[dihedralAtomIndex], bondDir));
			Vector3 perpendicularDir = Vector3.Cross(normalDir, bondDir);
			return positions[bondAtomIndex]
				+ bondLength * (float)Math.Cos(DegToRad(angle)) * bondDir
				+ bondLength * (float)Math.Sin(DegToRad(angle)) * (float)Math.Cos(DegToRad(dihedral)) * perpendicularDir
				+ bondLength * (float)Math.Sin(DegToRad(angle)) * (float)Math.Sin(DegToRad(dihedral)) * normalDir;
		}
	}

	private static float DegToRad(float deg)
	{
		return (float)(deg * Math.PI / 180.0f);
	}

	// Calculate Z-Matrix

	// public static string ConvertToZMatrix(List<AtomBase> atoms, List<BondBase> bonds)
	// {
	// 	//TODO: Implementation
	// }

	public static string ConvertToZMatrix(List<AtomBase> atoms, List<BondBase> bonds)
	{
		StringBuilder zMatrix = new StringBuilder();

		// First atom (just the element symbol)
		zMatrix.AppendLine(atoms[0].ElementSymbol);

		// Second atom (element symbol, index of first atom, bond length)
		Vector3 position1 = atoms[0].Position;
		Vector3 position2 = atoms[1].Position;
		float bondLength = Vector3.Distance(position1, position2);
		zMatrix.AppendLine($"{atoms[1].ElementSymbol} 1 {bondLength:F5}");

		if (atoms.Count > 2)
		{
			// Third atom (element symbol, index of bonded atom, bond length, index of atom forming angle, angle)
			BondBase bondToThird = bonds.Find(b => b.Atom2 == atoms[2] || b.Atom1 == atoms[2]);
			int bondAtomIndex = (bondToThird.Atom1 == atoms[2]) ? atoms.IndexOf(bondToThird.Atom2) : atoms.IndexOf(bondToThird.Atom1);
			bondLength = Vector3.Distance(atoms[bondAtomIndex].Position, atoms[2].Position);
			int angleAtomIndex = (bondAtomIndex == 0) ? 1 : 0; // Select the other atom for angle
			float angle = CalculateAngle(atoms[angleAtomIndex].Position, atoms[bondAtomIndex].Position, atoms[2].Position);
			zMatrix.AppendLine($"{atoms[2].ElementSymbol} {bondAtomIndex + 1} {bondLength:F5} {angleAtomIndex + 1} {angle:F5}");

			// Subsequent atoms
			for (int i = 3; i < atoms.Count; i++)
			{
				Vector3 positionI = atoms[i].Position;

				// Find the bond for the current atom
				BondBase bond = bonds.Find(b => b.Atom2 == atoms[i] || b.Atom1 == atoms[i]);
				if (bond == null)
					continue;

				// Get the index of the atom this atom is bonded to
				bondAtomIndex = (bond.Atom1 == atoms[i]) ? atoms.IndexOf(bond.Atom2) : atoms.IndexOf(bond.Atom1);
				bondLength = Vector3.Distance(atoms[bondAtomIndex].Position, positionI);

				// Determine the best angle atom
				angleAtomIndex = -1;
				for (int j = 0; j < i; j++)
				{
					if (j != bondAtomIndex && bonds.Exists(b => (b.Atom1 == atoms[bondAtomIndex] && b.Atom2 == atoms[j]) || (b.Atom2 == atoms[bondAtomIndex] && b.Atom1 == atoms[j])))
					{
						angleAtomIndex = j;
						break;
					}
				}

				if (angleAtomIndex == -1)
				{
					angleAtomIndex = bondAtomIndex > 0 ? 0 : 1;
				}

				angle = CalculateAngle(atoms[angleAtomIndex].Position, atoms[bondAtomIndex].Position, positionI);

				// Determine the dihedral angle
				int dihedralAtomIndex = -1;
				for (int j = 0; j < i; j++)
				{
					if (j != bondAtomIndex && j != angleAtomIndex && bonds.Exists(b => (b.Atom1 == atoms[angleAtomIndex] && b.Atom2 == atoms[j]) || (b.Atom2 == atoms[angleAtomIndex] && b.Atom1 == atoms[j])))
					{
						dihedralAtomIndex = j;
						break;
					}
				}

				if (dihedralAtomIndex == -1)
				{
					dihedralAtomIndex = angleAtomIndex > 1 ? 0 : 2;
				}

				float dihedralAngle = CalculateDihedralAngle(atoms[dihedralAtomIndex].Position, atoms[angleAtomIndex].Position, atoms[bondAtomIndex].Position, positionI);

				// Ensure valid dihedral calculation
				if (float.IsNaN(dihedralAngle))
				{
					dihedralAngle = 0.0f; // Default or handle the case where it's undefined
				}

				// Add to Z-Matrix
				zMatrix.AppendLine($"{atoms[i].ElementSymbol} {bondAtomIndex + 1} {bondLength:F5} {angleAtomIndex + 1} {angle:F5} {dihedralAtomIndex + 1} {dihedralAngle:F5}");
			}
		}

		return zMatrix.ToString();
	}


	private static float CalculateAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
	{
		Vector3 AB = pos1 - pos2;
		Vector3 BC = pos2 - pos3;

		AB = Vector3.Normalize(AB);
		BC = Vector3.Normalize(BC);

		float dotProd = Vector3.Dot(AB, BC);

		float angleInRad = (float)Math.Acos(dotProd);

		float angle = angleInRad * (180.0f / (float)Math.PI);

		return angle;
	}

	private static float CalculateDihedralAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4)
	{
		Vector3 AB = pos1 - pos2;
		Vector3 BC = pos2 - pos3;
		Vector3 CD = pos3 - pos4;

		Vector3 vec1 = Vector3.Normalize(Vector3.Cross(AB, BC));
		Vector3 vec2 = Vector3.Normalize(Vector3.Cross(BC, CD));

		float dotProd = Vector3.Dot(vec1, vec2);

		float angleInRad = (float)Math.Acos(dotProd);

		float angle = angleInRad * (180.0f / (float)Math.PI);

		// Determine the sign of the angle using the direction of the vector BC
		Vector3 crossProduct = Vector3.Cross(vec1, vec2);
		float sign = Vector3.Dot(crossProduct, BC) < 0 ? -1.0f : 1.0f;

		return angle * sign;
	}

	public static void SaveZMatrixToFile(string zMatrix, string filePath)
	{
		using (StreamWriter writer = new StreamWriter(filePath))
		{
			writer.Write(zMatrix);
		}
	}
}
