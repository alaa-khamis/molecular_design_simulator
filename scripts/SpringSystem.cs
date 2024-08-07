using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using Classes;

class SpringSystem
{
	public List<AtomBase> atoms;
	public List<BondBase> bonds;

	public SpringSystem(List<AtomBase> atoms, List<BondBase> bonds)
	{
		this.atoms = atoms;
		this.bonds = bonds;

	}

	public Vector3[] ComputeGradient()
	{
		Vector3[] gradients = new Vector3[atoms.Count];
		foreach (var bond in bonds)
		{
			Vector3 pos1 = bond.Atom1.Position;
			Vector3 pos2 = bond.Atom2.Position;
			Vector3 direction = pos1 - pos2;
			float currentLength = direction.Length();
			Vector3 normalizedDirection = Vector3.Normalize(direction);
			float restLength = bond.RestLength;
			Vector3 grad = (currentLength - restLength) * normalizedDirection;

			gradients[atoms.IndexOf(bond.Atom1)] += grad;
			gradients[atoms.IndexOf(bond.Atom2)] -= grad;
		}
		return gradients;
	}


	// Energy = 0.5 || x1 - x2 || ^ 2
	public float ComputeEnergy()
	{
		float energy = 0.0f;
		foreach (var bond in bonds)
		{
			Vector3 pos1 = bond.Atom1.Position;
			Vector3 pos2 = bond.Atom2.Position;
			float currentLength = (pos1 - pos2).Length();
			float restLength = bond.RestLength;
			energy += 0.5f * (currentLength - restLength) * (currentLength - restLength);
		}
		return energy;
	}

	public float BacktrackingLineSearch(Vector3[] directions, Vector3[] gradients, float alpha = 1.0f, float beta = 0.5f)
	{
		float energy = ComputeEnergy();
		float stepSize = alpha;

		while (true)
		{
			// Move atoms along the search direction
			for (int i = 0; i < atoms.Count; i++)
			{
				atoms[i].Position += stepSize * directions[i];
			}

			// Check if the energy has decreased
			float newEnergy = ComputeEnergy();
			if (newEnergy < energy)
			{
				break;
			}
			else
			{
				// Revert positions
				for (int i = 0; i < atoms.Count; i++)
				{
					atoms[i].Position -= stepSize * directions[i];
				}

				// Reduce step size
				stepSize *= beta;
			}
		}
		return stepSize;
	}

	public void Optimize(int maxIter = 5, float tol = 1e-6f)
	{
		for (int iter = 0; iter < maxIter; iter++)
		{
			Vector3[] gradients = ComputeGradient();
			float norm = 0.0f;
			foreach (var grad in gradients)
			{
				norm += grad.LengthSquared();
			}
			norm = (float)Math.Sqrt(norm);

			if (norm < tol)
			{
				break;
			}

			Vector3[] searchDirection = new Vector3[gradients.Length];
			for (int i = 0; i < gradients.Length; i++)
			{
				searchDirection[i] = -gradients[i];
			}

			BacktrackingLineSearch(searchDirection, gradients);
		}
	}
}
