using Godot;
using System;
using System.Collections.Generic;

public partial class MolecularSurface : Node3D
{
    private MeshInstance3D surfaceMeshInstance;
    private SurfaceTool surfaceTool;

    private const int GridResolution = 250;  // Increase resolution for finer details

    private float[,,] voxelGrid;
    private Vector3 gridOrigin;
    private float voxelSize;

    public override void _Ready()
    {
        surfaceMeshInstance = new MeshInstance3D();
        surfaceTool = new SurfaceTool();
        AddChild(surfaceMeshInstance);
    }

    public void GenerateSurface(List<AtomClass.atom> atoms)
    {
        InitializeVoxelGrid(atoms);
        RunMarchingCubes();
        surfaceMeshInstance.Mesh = surfaceTool.Commit();
    }

    private void InitializeVoxelGrid(List<AtomClass.atom> atoms)
    {
        // Determine the bounding box of all atoms
        Vector3 min = atoms[0].GlobalTransform.Origin;
        Vector3 max = atoms[0].GlobalTransform.Origin;

        foreach (var atom in atoms)
        {
            var pos = atom.GlobalTransform.Origin;
            min = new Vector3(Math.Min(min.X, pos.X), Math.Min(min.Y, pos.Y), Math.Min(min.Z, pos.Z));
            max = new Vector3(Math.Max(max.X, pos.X), Math.Max(max.Y, pos.Y), Math.Max(max.Z, pos.Z));
        }

        // Expand the bounding box a bit
        Vector3 padding = new Vector3(2, 2, 2);
        min -= padding;
        max += padding;

        // Calculate grid parameters
        gridOrigin = min;
        Vector3 gridSize = max - min;
        voxelSize = gridSize.X / GridResolution;
        voxelGrid = new float[GridResolution, GridResolution, GridResolution];

        // Populate the voxel grid with density values based on the atoms
        for (int x = 0; x < GridResolution; x++)
        {
            for (int y = 0; y < GridResolution; y++)
            {
                for (int z = 0; z < GridResolution; z++)
                {
                    Vector3 voxelPos = gridOrigin + new Vector3(x, y, z) * voxelSize;
                    float density = 0.0f;

                    foreach (var atom in atoms)
                    {
                        Vector3 atomPos = atom.GlobalTransform.Origin;
                        float distance = voxelPos.DistanceTo(atomPos);
                        float influence = Mathf.Exp(-Mathf.Pow(distance / atom.VanDerWaalsRadius, 2));
                        density += influence;
                    }

                    voxelGrid[x, y, z] = density;
                }
            }
        }
    }


    private void RunMarchingCubes()
    {
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        for (int x = 0; x < GridResolution - 1; x++)
        {
            for (int y = 0; y < GridResolution - 1; y++)
            {
                for (int z = 0; z < GridResolution - 1; z++)
                {
                    // Get the 8 voxel values around the current cell
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3I offset = CubeCorners[i];
                        cube[i] = voxelGrid[x + offset.X, y + offset.Y, z + offset.Z];
                    }

                    // Compute the marching cubes case index
                    int caseIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cube[i] > 0.5f)
                        {
                            caseIndex |= 1 << i;
                        }
                    }

                    // Create triangles from the case index
                    for (int i = 0; i < TriTable[caseIndex].Length; i += 3)
                    {
                        if (TriTable[caseIndex][i] == -1) break;

                        Vector3[] vertices = new Vector3[3];
                        for (int j = 0; j < 3; j++)
                        {
                            int edgeIndex = TriTable[caseIndex][i + j];
                            vertices[j] = InterpolateEdge(edgeIndex, x, y, z, cube);
                        }

                        // Add the triangle to the surface tool
                        surfaceTool.AddVertex(vertices[0]);
                        surfaceTool.AddVertex(vertices[1]);
                        surfaceTool.AddVertex(vertices[2]);
                    }
                }
            }
        }
    }


    private Vector3 InterpolateEdge(int edgeIndex, int x, int y, int z, float[] cube)
    {
        int p1 = CubeEdgeConnection[edgeIndex, 0];
        int p2 = CubeEdgeConnection[edgeIndex, 1];

        Vector3 v1 = gridOrigin + new Vector3(x + CubeCorners[p1].X, y + CubeCorners[p1].Y, z + CubeCorners[p1].Z) * voxelSize;
        Vector3 v2 = gridOrigin + new Vector3(x + CubeCorners[p2].X, y + CubeCorners[p2].Y, z + CubeCorners[p2].Z) * voxelSize;

        float density1 = cube[p1];
        float density2 = cube[p2];

        float t = (0.5f - density1) / (density2 - density1);
        return v1 + t * (v2 - v1);
    }

    private static readonly Vector3I[] CubeCorners = new Vector3I[]
    {
            new Vector3I(0, 0, 0), new Vector3I(1, 0, 0),
            new Vector3I(1, 1, 0), new Vector3I(0, 1, 0),
            new Vector3I(0, 0, 1), new Vector3I(1, 0, 1),
            new Vector3I(1, 1, 1), new Vector3I(0, 1, 1)
    };

    private static readonly Vector3I[,] EdgeCorners = new Vector3I[,]
    {
            { new Vector3I(0, 0, 0), new Vector3I(1, 0, 0) },
            { new Vector3I(1, 0, 0), new Vector3I(1, 1, 0) },
            { new Vector3I(1, 1, 0), new Vector3I(0, 1, 0) },
            { new Vector3I(0, 1, 0), new Vector3I(0, 0, 0) },
            { new Vector3I(0, 0, 1), new Vector3I(1, 0, 1) },
            { new Vector3I(1, 0, 1), new Vector3I(1, 1, 1) },
            { new Vector3I(1, 1, 1), new Vector3I(0, 1, 1) },
            { new Vector3I(0, 1, 1), new Vector3I(0, 0, 1) },
            { new Vector3I(0, 0, 0), new Vector3I(0, 0, 1) },
            { new Vector3I(1, 0, 0), new Vector3I(1, 0, 1) },
            { new Vector3I(1, 1, 0), new Vector3I(1, 1, 1) },
            { new Vector3I(0, 1, 0), new Vector3I(0, 1, 1) }
    };

    private static readonly int[,] CubeEdgeConnection = new int[,]
    {
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
    };

    private static readonly int[][] TriTable = new int[][]
    {
            new int[] {-1}, new int[] {0, 8, 3, -1}, new int[] {0, 1, 9, -1}, new int[] {1, 8, 3, 9, 8, 1, -1}, new int[] {1, 2, 10, -1}, new int[] {0, 8, 3, 1, 2, 10, -1},
            new int[] {9, 2, 10, 0, 2, 9, -1}, new int[] {2, 8, 3, 2, 10, 8, 10, 9, 8, -1}, new int[] {3, 11, 2, -1}, new int[] {0, 11, 2, 8, 11, 0, -1}, new int[] {1, 9, 0, 2, 3, 11, -1},
            new int[] {1, 11, 2, 1, 9, 11, 9, 8, 11, -1}, new int[] {3, 10, 1, 11, 10, 3, -1}, new int[] {0, 10, 1, 0, 8, 10, 8, 11, 10, -1}, new int[] {3, 9, 0, 3, 11, 9, 11, 10, 9, -1},
            new int[] {9, 8, 10, 10, 8, 11, -1}, new int[] {4, 7, 8, -1}, new int[] {4, 3, 0, 7, 3, 4, -1}, new int[] {0, 1, 9, 8, 4, 7, -1}, new int[] {4, 1, 9, 4, 7, 1, 7, 3, 1, -1},
            new int[] {1, 2, 10, 8, 4, 7, -1}, new int[] {3, 4, 7, 3, 0, 4, 1, 2, 10, -1}, new int[] {9, 2, 10, 9, 0, 2, 8, 4, 7, -1}, new int[] {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1},
            new int[] {8, 4, 7, 3, 11, 2, -1}, new int[] {11, 4, 7, 11, 2, 4, 2, 0, 4, -1}, new int[] {9, 0, 1, 8, 4, 7, 2, 3, 11, -1}, new int[] {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1},
            new int[] {3, 10, 1, 3, 11, 10, 7, 8, 4, -1}, new int[] {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1}, new int[] {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1}, new int[] {4, 7, 11, 4, 11, 9, 9, 11, 10, -1},
            new int[] {9, 5, 4, -1}, new int[] {9, 5, 4, 0, 8, 3, -1}, new int[] {0, 5, 4, 1, 5, 0, -1}, new int[] {8, 5, 4, 8, 3, 5, 3, 1, 5, -1}, new int[] {1, 2, 10, 9, 5, 4, -1}, new int[] {3, 0, 8, 1, 2, 10, 4, 9, 5, -1},
            new int[] {5, 2, 10, 5, 4, 2, 4, 0, 2, -1}, new int[] {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1}, new int[] {9, 5, 4, 2, 3, 11, -1}, new int[] {0, 11, 2, 0, 8, 11, 4, 9, 5, -1}, new int[] {0, 5, 4, 0, 1, 5, 2, 3, 11, -1},
            new int[] {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1}, new int[] {10, 3, 11, 10, 1, 3, 9, 5, 4, -1}, new int[] {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1}, new int[] {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1},
            new int[] {5, 4, 8, 5, 8, 10, 10, 8, 11, -1}, new int[] {9, 7, 8, 5, 7, 9, -1}, new int[] {9, 3, 0, 9, 5, 3, 5, 7, 3, -1}, new int[] {0, 7, 8, 0, 1, 7, 1, 5, 7, -1}, new int[] {1, 5, 3, 3, 5, 7, -1}, new int[] {9, 7, 8, 9, 5, 7, 10, 1, 2, -1},
            new int[] {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1}, new int[] {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1}, new int[] {2, 10, 5, 2, 5, 3, 3, 5, 7, -1}, new int[] {7, 9, 5, 7, 8, 9, 3, 11, 2, -1}, new int[] {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1},
            new int[] {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1}, new int[] {11, 2, 1, 11, 1, 7, 7, 1, 5, -1}, new int[] {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1}, new int[] {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
            new int[] {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1}, new int[] {11, 10, 5, 7, 11, 5, -1}, new int[] {10, 6, 5, -1}, new int[] {0, 8, 3, 5, 10, 6, -1}, new int[] {9, 0, 1, 5, 10, 6, -1}, new int[] {1, 8, 3, 1, 9, 8, 5, 10, 6, -1},
            new int[] {1, 6, 5, 2, 6, 1, -1}, new int[] {1, 6, 5, 1, 2, 6, 3, 0, 8, -1}, new int[] {9, 6, 5, 9, 0, 6, 0, 2, 6, -1}, new int[] {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1}, new int[] {2, 3, 11, 10, 6, 5, -1},
            new int[] {11, 0, 8, 11, 2, 0, 10, 6, 5, -1}, new int[] {0, 1, 9, 2, 3, 11, 5, 10, 6, -1}, new int[] {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1}, new int[] {6, 3, 11, 6, 5, 3, 5, 1, 3, -1}, new int[] {0, 8, 11, 0, 11, 5, 0, 5, 9, 5, 11, 6, -1},
            new int[] {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 1, -1}, new int[] {6, 5, 9, 6, 9, 11, 11, 9, 8, -1}, new int[] {5, 10, 6, 4, 7, 8, -1}, new int[] {4, 3, 0, 4, 7, 3, 6, 5, 10, -1}, new int[] {1, 9, 0, 5, 10, 6, 8, 4, 7, -1}, new int[] {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1},
            new int[] {6, 1, 2, 6, 5, 1, 4, 7, 8, -1}, new int[] {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1}, new int[] {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1}, new int[] {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1}, new int[] {3, 11, 2, 7, 8, 4, 10, 6, 5, -1},
            new int[] {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1}, new int[] {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1}, new int[] {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1}, new int[] {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1},
            new int[] {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1}, new int[] {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1}, new int[] {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1}, new int[] {10, 4, 9, 6, 4, 10, -1}, new int[] {4, 10, 6, 4, 9, 10, 0, 8, 3, -1},
            new int[] {10, 0, 1, 10, 6, 0, 6, 4, 0, -1}, new int[] {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1}, new int[] {1, 4, 9, 1, 2, 4, 2, 6, 4, -1}, new int[] {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1}, new int[] {0, 2, 4, 4, 2, 6, -1}, new int[] {8, 3, 2, 8, 2, 4, 4, 2, 6, -1},
            new int[] {10, 4, 9, 10, 6, 4, 11, 2, 3, -1}, new int[] {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1}, new int[] {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1}, new int[] {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1}, new int[] {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1},
            new int[] {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1}, new int[] {3, 11, 6, 3, 6, 0, 0, 6, 4, -1}, new int[] {6, 4, 8, 11, 6, 8, -1}, new int[] {7, 10, 6, 7, 8, 10, 8, 9, 10, -1}, new int[] {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1},
            new int[] {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1}, new int[] {10, 6, 7, 10, 7, 1, 1, 7, 3, -1}, new int[] {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1}, new int[] {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1}, new int[] {7, 8, 0, 7, 0, 6, 6, 0, 2, -1}, new int[] {7, 3, 2, 6, 7, 2, -1},
            new int[] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1}, new int[] {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1}, new int[] {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1}, new int[] {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1}, new int[] {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
            new int[] {0, 9, 1, 11, 6, 7, -1}, new int[] {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1}, new int[] {7, 11, 6, -1}, new int[] {7, 6, 11, -1}, new int[] {3, 0, 8, 11, 7, 6, -1}, new int[] {0, 1, 9, 11, 7, 6, -1}, new int[] {8, 1, 9, 8, 3, 1, 11, 7, 6, -1},
            new int[] {10, 1, 2, 6, 11, 7, -1}, new int[] {1, 2, 10, 3, 0, 8, 6, 11, 7, -1}, new int[] {2, 9, 0, 2, 10, 9, 6, 11, 7, -1}, new int[] {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1}, new int[] {7, 2, 3, 6, 2, 7, -1}, new int[] {7, 0, 8, 7, 6, 0, 6, 2, 0, -1},
            new int[] {2, 7, 6, 2, 3, 7, 0, 1, 9, -1}, new int[] {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1}, new int[] {10, 7, 6, 10, 1, 7, 1, 3, 7, -1}, new int[] {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1}, new int[] {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1}, new int[] {7, 6, 10, 7, 10, 8, 8, 10, 9, -1},
            new int[] {6, 8, 4, 11, 8, 6, -1}, new int[] {3, 6, 11, 3, 0, 6, 0, 4, 6, -1}, new int[] {8, 6, 11, 8, 4, 6, 9, 0, 1, -1}, new int[] {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1}, new int[] {6, 8, 4, 6, 11, 8, 2, 10, 1, -1}, new int[] {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1},
            new int[] {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1}, new int[] {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1}, new int[] {8, 2, 3, 8, 4, 2, 4, 6, 2, -1}, new int[] {0, 4, 2, 4, 6, 2, -1}, new int[] {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1},
            new int[] {1, 9, 4, 1, 4, 2, 2, 4, 6, -1}, new int[] {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1}, new int[] {10, 1, 0, 10, 0, 6, 6, 0, 4, -1}, new int[] {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1}, new int[] {10, 9, 4, 6, 10, 4, -1},
            new int[] {4, 9, 5, 7, 6, 11, -1}, new int[] {0, 8, 3, 4, 9, 5, 11, 7, 6, -1}, new int[] {5, 0, 1, 5, 4, 0, 7, 6, 11, -1}, new int[] {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1}, new int[] {9, 5, 4, 10, 1, 2, 7, 6, 11, -1}, new int[] {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1},
            new int[] {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1}, new int[] {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1}, new int[] {7, 2, 3, 7, 6, 2, 5, 4, 9, -1}, new int[] {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1}, new int[] {3, 6, 2, 3, 7, 6, 1, 9, 0, 5, 4, 8, -1},
            new int[] {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1}, new int[] {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1}, new int[] {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1}, new int[] {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1}, new int[] {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1},
            new int[] {6, 9, 5, 6, 11, 9, 11, 8, 9, -1}, new int[] {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1}, new int[] {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1}, new int[] {6, 11, 3, 6, 3, 5, 5, 3, 1, -1}, new int[] {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1}, new int[] {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
            new int[] {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1}, new int[] {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1}, new int[] {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1}, new int[] {9, 5, 6, 9, 6, 0, 0, 6, 2, -1}, new int[] {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
            new int[] {1, 5, 6, 2, 1, 6, -1}, new int[] {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1}, new int[] {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1}, new int[] {0, 3, 8, 5, 6, 10, -1}, new int[] {10, 5, 6, -1},
            new int[] {11, 5, 10, 7, 5, 11, -1}, new int[] {11, 5, 10, 11, 7, 5, 8, 3, 0, -1}, new int[] {5, 11, 7, 5, 10, 11, 1, 9, 0, -1}, new int[] {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1}, new int[] {11, 1, 2, 11, 7, 1, 7, 5, 1, -1}, new int[] {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1},
            new int[] {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1}, new int[] {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1}, new int[] {2, 5, 10, 2, 3, 5, 3, 7, 5, -1}, new int[] {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1}, new int[] {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1},
            new int[] {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1}, new int[] {1, 3, 5, 3, 7, 5, -1}, new int[] {0, 8, 7, 0, 7, 1, 1, 7, 5, -1}, new int[] {9, 0, 3, 9, 3, 5, 5, 3, 7, -1}, new int[] {9, 8, 7, 5, 9, 7, -1},
            new int[] {5, 8, 4, 5, 10, 8, 10, 11, 8, -1}, new int[] {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1}, new int[] {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1}, new int[] {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1}, new int[] {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1},
            new int[] {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1}, new int[] {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1}, new int[] {9, 4, 5, 2, 11, 3, -1}, new int[] {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1}, new int[] {5, 10, 2, 5, 2, 4, 4, 2, 0, -1},
            new int[] {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1}, new int[] {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1}, new int[] {8, 4, 5, 8, 5, 3, 3, 5, 1, -1}, new int[] {0, 4, 5, 1, 0, 5, -1}, new int[] {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1},
            new int[] {9, 4, 5, -1}, new int[] {4, 11, 7, 4, 9, 11, 9, 10, 11, -1}, new int[] {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1}, new int[] {1, 11, 7, 1, 10, 11, 1, 4, 10, 1, 0, 4, 10, 4, 11, -1}, new int[] {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
            new int[] {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1}, new int[] {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1}, new int[] {11, 7, 4, 11, 4, 2, 2, 4, 0, -1}, new int[] {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1}, new int[] {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1},
            new int[] {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1}, new int[] {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1}, new int[] {1, 10, 2, 8, 7, 4, -1}, new int[] {4, 9, 1, 4, 1, 7, 7, 1, 3, -1}, new int[] {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1},
            new int[] {4, 0, 3, 7, 4, 3, -1}, new int[] {4, 8, 7, -1}, new int[] {9, 10, 8, 10, 11, 8, -1}, new int[] {3, 0, 9, 3, 9, 11, 11, 9, 10, -1}, new int[] {0, 1, 10, 0, 10, 8, 8, 10, 11, -1}, new int[] {3, 1, 10, 11, 3, 10, -1},
            new int[] {1, 2, 11, 1, 11, 9, 9, 11, 8, -1}, new int[] {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1}, new int[] {0, 2, 11, 8, 0, 11, -1}, new int[] {3, 2, 11, -1}, new int[] {2, 3, 8, 2, 8, 10, 10, 8, 9, -1}, new int[] {9, 10, 2, 0, 9, 2, -1},
            new int[] {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1}, new int[] {1, 10, 2, -1}, new int[] {1, 3, 8, 9, 1, 8, -1}, new int[] {0, 9, 1, -1}, new int[] {0, 3, 8, -1}, new int[] {-1}
    };

}
