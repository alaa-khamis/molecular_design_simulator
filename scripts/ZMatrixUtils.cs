using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Classes;

public static class MoleculeUtils
{
    public static string ConvertToZMatrix(List<AtomBase> atomList, List<BondBase> bondsList)
    {
        if (atomList.Count == 0)
            return string.Empty;

        StringBuilder zMatrix = new StringBuilder();

        // Add the first atom
        var firstAtom = atomList[0];
        zMatrix.AppendLine($"{firstAtom.ElementSymbol}");

        if (atomList.Count > 1)
        {
            // Add the second atom
            var secondAtom = atomList[1];
            float distance = Vector3.Distance(secondAtom.Position, firstAtom.Position);
            zMatrix.AppendLine($"{secondAtom.ElementSymbol} 1 {distance:F4}");

            if (atomList.Count > 2)
            {
                // Add the third atom
                var thirdAtom = atomList[2];
                float angle = CalculateAngle(firstAtom.Position, secondAtom.Position, thirdAtom.Position);
                zMatrix.AppendLine($"{thirdAtom.ElementSymbol} 1 {distance:F4} 2 {angle:F4}");

                // Add the rest of the atoms
                for (int i = 3; i < atomList.Count; i++)
                {
                    var atom = atomList[i];
                    var ref1 = atomList[i - 1];
                    var ref2 = atomList[i - 2];
                    var ref3 = atomList[i - 3];

                    distance = Vector3.Distance(atom.Position, ref1.Position);
                    angle = CalculateAngle(ref1.Position, ref2.Position, atom.Position);
                    float dihedral = CalculateDihedralAngle(ref3.Position, ref2.Position, ref1.Position, atom.Position);

                    zMatrix.AppendLine($"{atom.ElementSymbol} {i} {distance:F4} {i - 1} {angle:F4} {i - 2} {dihedral:F4}");
                }
            }
        }

        return zMatrix.ToString();
    }

    private static float CalculateAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        Vector3 vec1 = Vector3.Normalize(pos1 - pos2);
        Vector3 vec2 = Vector3.Normalize(pos3 - pos2);
        float dotProduct = Vector3.Dot(vec1, vec2);
        return MathF.Acos(dotProduct) * (180.0f / MathF.PI);
    }

    private static float CalculateDihedralAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4)
    {
        Vector3 b1 = pos2 - pos1;
        Vector3 b2 = pos3 - pos2;
        Vector3 b3 = pos4 - pos3;

        Vector3 n1 = Vector3.Cross(b1, b2);
        Vector3 n2 = Vector3.Cross(b2, b3);

        n1 = Vector3.Normalize(n1);
        n2 = Vector3.Normalize(n2);

        float m1 = Vector3.Dot(n1, n2);
        float m2 = Vector3.Dot(Vector3.Cross(n1, b2), n2);

        return MathF.Atan2(m2, m1) * (180.0f / MathF.PI);
    }

    public static void SaveZMatrixToFile(string zMatrix, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.Write(zMatrix);
        }
    }
}
