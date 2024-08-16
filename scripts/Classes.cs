using System;
using System.Collections.Generic;
using System.Numerics;

namespace Classes
{
	public class AtomBase
	{
		public float Radius { get; set; }
		public string ElementSymbol { get; set; }
		public string ElementName { get; set; }
		public int AtomicNumber { get; set; }
		public float Mass { get; set; }
		public float CovalentRadius { get; set; }
		public float VanDerWaalsRadius { get; set; }
		public Vector3 AtomColor { get; set; }
		public Vector3 Position { get; set; }

		// Copy Data
		public void CopyData(AtomBase newAtom)
		{
			ElementSymbol = newAtom.ElementSymbol;
			ElementName = newAtom.ElementName;
			AtomicNumber = newAtom.AtomicNumber;
			Mass = newAtom.Mass;
			CovalentRadius = newAtom.CovalentRadius;
			VanDerWaalsRadius = newAtom.VanDerWaalsRadius;
			Radius = newAtom.VanDerWaalsRadius;
			AtomColor = newAtom.AtomColor;
			Position = newAtom.Position;
		}

		public void SetPosition(System.Numerics.Vector3 pos){
			Position = pos;
		}

		// Constructor
		public AtomBase(
			string elementSymbol,
			string elementName,
			int atomicNumber,
			float mass,
			float covalentRadius,
			float vanDerWaalsRadius,
			Vector3 atomColor,
			Vector3 position)
		{
			Radius = vanDerWaalsRadius;
			ElementSymbol = elementSymbol;
			ElementName = elementName;
			AtomicNumber = atomicNumber;
			Mass = mass;
			CovalentRadius = covalentRadius;
			VanDerWaalsRadius = vanDerWaalsRadius;
			AtomColor = atomColor;
			Position = position;
		}

		public AtomBase(){}

	}

	public class BondBase
	{
		public AtomBase Atom1 { get; set; }
		public AtomBase Atom2 { get; set; }
		public float BondLength { get; set; }
		public static float RestLength = 1.0f;

		public BondBase(AtomBase atom1, AtomBase atom2)
		{
			Atom1 = atom1;
			Atom2 = atom2;

			Vector3 startPos = Atom1.Position;
			Vector3 endPos = Atom2.Position;
			Vector3 bondVec = endPos - startPos;
			float length = bondVec.Length();

			BondLength = length;
		}

		public BondBase(){}
	}
}
