using System;
using System.Collections.Generic;
using System.Numerics;

namespace Classes
{
	public interface Atom
	{
		public float atomRadius { get; set; }
		public string ElementSymbol { get; set; }
		public string ElementName { get; set; }
		public int AtomicNumber { get; set; }
		public float Mass { get; set; }
		public float CovalentRadius { get; set; }
		public float VanDerWaalsRadius { get; set; }

	}

	public interface Bond
	{
		public Atom Atom1 { get; set; }
		public Atom Atom2 { get; set; }
		public float BondLength { get; set; }
	}
}
