using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.IO;


namespace drawClusterNamespace
{

    public struct int3
    {
        public int x;
        public int y;
        public int z;

        public int3(int nx, int ny, int nz)
        {
            x = nx;
            y = ny;
            z = nz;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is int3)) return false;
            int3 other = (int3)obj;
            return x == other.x && y == other.y && z == other.z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                return hash;
            }
        }
    }

    public struct pyramidCoord
    {
        public int3 pos;
        public int pyramid;

        public pyramidCoord(int3 p, int py)
        {
            pos = p;
            pyramid = py;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is pyramidCoord)) return false;
            pyramidCoord other = (pyramidCoord)obj;
            return pos.Equals(other.pos) && pyramid == other.pyramid;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash = 31 + pos.GetHashCode();
                hash = hash = 31 + pyramid;
                return hash;
            }
        }
    }

    public struct posRot
    {
        public int3 pos;
        public int rot;

        public posRot(int3 p, int r)
        {
            pos = p;
            rot = r;
        }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class drawCluster : MonoBehaviour
    {
        public List<pyramidCoord> pyramids;
        public List<pyramidCoord> surroundPyramids;

        public List<Vector3> vertices;
        public List<int> triangles;
        public List<Vector3> surroundVertices;
        public List<int> surroundTriangles;

        public Mesh mesh;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        public Mesh surroundMesh;
        public MeshFilter surroundMeshFilter;
        public MeshRenderer surroundMeshRenderer;
        public GameObject surroundGO;

        public Material material;
        public Material surroundMaterial;

        void Start()
        {
            material = new Material(Shader.Find("Autodesk Interactive"));
            material.color = Color.red;
            //surroundMaterial = new Material(Shader.Find("Aurodesk Interactive"));
            //surroundMaterial.color = Color.blue;

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = material;

            surroundGO = new GameObject();
            surroundGO.AddComponent<MeshFilter>();
            surroundGO.AddComponent<MeshRenderer>();
            surroundMeshRenderer = surroundGO.GetComponent<MeshRenderer>();
            surroundMeshRenderer.material = surroundMaterial;

            pyramids = new List<pyramidCoord>();
            surroundPyramids = new List<pyramidCoord>();

            vertices = new List<Vector3>();
            triangles = new List<int>();

            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();

            surroundVertices = new List<Vector3>();
            surroundTriangles = new List<int>();

            surroundMeshFilter = surroundGO.GetComponent<MeshFilter>();
            surroundMesh = new Mesh();

            pyramids = generateSingleTilePyramidCoords(new int3(0, 0, 0), 0);
            string path = Application.dataPath + "/Scripts/solution.txt"; // -> tiles
            //string path = Application.dataPath + "/Scripts/corona_cells.txt"; // -> pyramids
            //string path = Application.dataPath + "/Scripts/surround.txt"; // -> pyramids
            
            //ImportPyramidsFromFile(path);
            ImportTilesFromFile(path);

            generateMesh(vertices, triangles, pyramids);
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

        }

        public static int3 add(int3 a, int3 b)
        {
            return new int3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public void ImportPyramidsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at: {filePath}");
                return;
            }

            // Clear existing data before import
            pyramids.Clear();
            string[] lines = File.ReadAllLines(filePath);            

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Clean the string: remove ( ) and spaces
                string cleanLine = line.Replace("(", "").Replace(")", "").Replace(" ", "");
                string[] parts = cleanLine.Split(',');

                if (parts.Length == 4)
                {
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    int z = int.Parse(parts[2]);
                    int pyr = int.Parse(parts[3]);

                    pyramids.Add(new pyramidCoord(new int3(x, y, z), pyr));
                }
            }
        }

        public void ImportTilesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at: {filePath}");
                return;
            }

            // Clear existing data before import
            surroundPyramids.Clear();

            //try
            //{
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Clean the string: remove ( ) and spaces
                    string cleanLine = line.Replace("(", "").Replace(")", "").Replace(" ", "");
                    string[] parts = cleanLine.Split(',');

                    if (parts.Length == 4)
                    {
                        int x = int.Parse(parts[0]);
                        int y = int.Parse(parts[1]);
                        int z = int.Parse(parts[2]);
                        int rot = int.Parse(parts[3]);

                        // Use your existing logic to generate coordinates for this tile
                        List<pyramidCoord> tilePyramids = generateSingleTilePyramidCoords(new int3(x, y, z), rot);
                        
                        // Add the resulting pyramid components to the main list
                        surroundPyramids.AddRange(tilePyramids);



                        // TODO: 2 different colors for debug...
                        // !!!
                        //
                        //
                        //
                        //
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping malformed line: {line}");
                    }
                }

                // Refresh the mesh after importing all tiles
                generateMesh(surroundVertices, surroundTriangles, surroundPyramids);
                surroundMesh.Clear();
                surroundMesh.SetVertices(surroundVertices);
                surroundMesh.SetTriangles(surroundTriangles, 0);
                surroundMesh.RecalculateNormals();
                surroundMeshFilter.mesh = surroundMesh;
                
                Debug.Log($"Import complete. Total pyramid components: {surroundPyramids.Count}");
            //}
            //catch (Exception e)
            //{
            //    Debug.LogError($"Error parsing file: {e.Message}");
            //}
        }

        public List<pyramidCoord> generateSingleTilePyramidCoords(int3 pos, int r)
        {
            int center = 1;

            List<pyramidCoord> tempPyramidCoords = new List<pyramidCoord>();

            // --- tile description ---
            //
            //              Y
            //              ^
            //              |
            //              +------> Z
            //             /
            //            X  

            //                  2 1
            //                  |/
            //              5 --+-- 4
            //                 /|
            //                0 3  


            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, -1 - center)), 4));
            
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0 - center)), 1));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0 - center)), 3));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0 - center)), 4));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0 - center)), 5));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(-1, 0, 0 - center)), 0));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, -1, 0 - center)), 2));
            
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 1 - center)), 1));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 1 - center)), 3));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 1 - center)), 4));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 1 - center)), 5));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(-1, 0, 1 - center)), 0));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, -1, 1 - center)), 2));
            
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 2 - center)), 1));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 2 - center)), 3));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 2 - center)), 4));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 2 - center)), 5));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(-1, 0, 2 - center)), 0));
            tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, -1, 2 - center)), 2));
            // --- ---

            // --- test tile ---
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 0));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 1));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 2));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 3));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 4));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 5));

            // --- simple tile ---
            // tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 0));
            // tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 2));
            // tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 4));

            // --- simple tile ---
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 0));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 1));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 4));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 5));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 2));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 1, 0)), 3));


            return createRotatedPyramidCoords(r % 24, tempPyramidCoords, pos);
        }

        List<pyramidCoord> createRotatedPyramidCoords(int r, List<pyramidCoord> tempPyramidCoords, int3 center)
        {
            // sorted rotations!
            List<pyramidCoord> returnPyramidCoords;
            switch (r)
            {
                case 0:
                    returnPyramidCoords = tempPyramidCoords; //+y up 0
                    break;
                case 1:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 1, true, center); //+y up 1
                    break;
                case 2:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 1, true, center), 1, true, center); //+y up 2
                    break;
                case 3:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 1, false, center); //+y up 3
                    break;

                case 4:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 2, false, center); //+x up 0
                    break;
                case 5:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center); //+x up 1
                    break;
                case 6:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center), 1, true, center); //+x up 2
                    break;
                case 7:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, false, center); //+x up 3
                    break;

                case 8:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center); //-y up 0
                    break;
                case 9:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center); //-y up 1
                    break;
                case 10:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center), 1, true, center); //-y up 2
                    break;
                case 11:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, false, center); //-y up 3
                    break;

                case 12:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 2, true, center); //-x up 0
                    break;
                case 13:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center); //-x up 1
                    break;
                case 14:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center), 1, true, center); //-x up 2
                    break;
                case 15:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, false, center); //-x up 3
                    break;

                case 16:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 0, true, center); // +z up 0
                    break;
                case 17:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center); // +z up 1
                    break;
                case 18:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center), 1, true, center); // +z up 2
                    break;
                case 19:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, false, center); // +z up 3
                    break;

                case 20:
                    returnPyramidCoords = rotatePyramids(tempPyramidCoords, 0, false, center); //-z up 0
                    break;
                case 21:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center); //-z up 1
                    break;
                case 22:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center), 1, true, center); //-z up 2
                    break;
                case 23:
                    returnPyramidCoords = rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, false, center); //-z up 3
                    break;

                default:
                    returnPyramidCoords = tempPyramidCoords;
                    break;
            }
            return returnPyramidCoords;
        }

         List<pyramidCoord> rotatePyramids(List<pyramidCoord> pyrCoord, int axisxyz, bool dir, int3 center)
        {
            List<pyramidCoord> newCoords = new List<pyramidCoord>();

            switch (axisxyz)
            {
                case 0:
                    if (dir)
                    {
                        // x -> x
                        // y -> -z
                        // z -> y
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 0;
                                    break;
                                case 1:
                                    newPyr = 1;
                                    break;
                                case 2:
                                    newPyr = 5;
                                    break;
                                case 3:
                                    newPyr = 4;
                                    break;
                                case 4:
                                    newPyr = 2;
                                    break;
                                case 5:
                                    newPyr = 3;
                                    break;
                            }
                            newCoords.Add(new pyramidCoord(new int3(p.pos.x, center.y + p.pos.z - center.z, center.z - (p.pos.y - center.y)), newPyr));
                        }
                    }
                    else
                    {
                        // x -> x
                        // y -> z
                        // z -> -y
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 0;
                                    break;
                                case 1:
                                    newPyr = 1;
                                    break;
                                case 2:
                                    newPyr = 4;
                                    break;
                                case 3:
                                    newPyr = 5;
                                    break;
                                case 4:
                                    newPyr = 3;
                                    break;
                                case 5:
                                    newPyr = 2;
                                    break;
                            }
                            newCoords.Add(new pyramidCoord(new int3(p.pos.x, center.y - (p.pos.z - center.z), center.z + (p.pos.y - center.y)), newPyr));
                        }
                    }
                    break;
                case 1: // switch (axisxyz)
                    if (dir)
                    {
                        // x -> z
                        // y -> y
                        // z -> -x
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 4;
                                    break;
                                case 1:
                                    newPyr = 5;
                                    break;
                                case 2:
                                    newPyr = 2;
                                    break;
                                case 3:
                                    newPyr = 3;
                                    break;
                                case 4:
                                    newPyr = 1;
                                    break;
                                case 5:
                                    newPyr = 0;
                                    break;
                            }
                            newCoords.Add(new pyramidCoord(new int3(center.x - (p.pos.z - center.z), p.pos.y, center.z + (p.pos.x - center.x)), newPyr));
                        }
                    }
                    else
                    {
                        // x -> -z
                        // y -> y
                        // z -> x
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 5;
                                    break;
                                case 1:
                                    newPyr = 4;
                                    break;
                                case 2:
                                    newPyr = 2;
                                    break;
                                case 3:
                                    newPyr = 3;
                                    break;
                                case 4:
                                    newPyr = 0;
                                    break;
                                case 5:
                                    newPyr = 1;
                                    break;
                            }
                            newCoords.Add(new pyramidCoord(new int3(center.x + (p.pos.z - center.z), p.pos.y, center.z - (p.pos.x - center.x)), newPyr));
                        }
                    }
                    break;
                case 2: // switch (axisxyz)
                    if (dir)
                    {
                        // x -> -y
                        // y -> x
                        // z -> z
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 3;
                                    break;
                                case 1:
                                    newPyr = 2;
                                    break;
                                case 2:
                                    newPyr = 0;
                                    break;
                                case 3:
                                    newPyr = 1;
                                    break;
                                case 4:
                                    newPyr = 4;
                                    break;
                                case 5:
                                    newPyr = 5;
                                    break;
                            }

                            newCoords.Add(new pyramidCoord(new int3(center.x + (p.pos.y - center.y), center.y - (p.pos.x - center.x), p.pos.z), newPyr));
                        }
                    }
                    else
                    {
                        // x -> y
                        // y -> -x
                        // z -> z
                        foreach (pyramidCoord p in pyrCoord)
                        {
                            int newPyr = -1;
                            switch (p.pyramid)
                            {
                                case 0:
                                    newPyr = 2;
                                    break;
                                case 1:
                                    newPyr = 3;
                                    break;
                                case 2:
                                    newPyr = 1;
                                    break;
                                case 3:
                                    newPyr = 0;
                                    break;
                                case 4:
                                    newPyr = 4;
                                    break;
                                case 5:
                                    newPyr = 5;
                                    break;
                            }
                            newCoords.Add(new pyramidCoord(new int3(center.x - (p.pos.y - center.y), center.y + (p.pos.x - center.x), p.pos.z), newPyr));
                        }
                    }
                    break;
            }
            return newCoords;
        }


        void generateMesh(List<Vector3> vertices, List<int> triangles, List<pyramidCoord> pyramids)
        {
            triangles.Clear();
            vertices.Clear();
            int pyramidCount = 0;
            foreach (pyramidCoord p in pyramids)
            {
                triangles.Add(16 * pyramidCount + 0);
                triangles.Add(16 * pyramidCount + 1);
                triangles.Add(16 * pyramidCount + 2);

                triangles.Add(16 * pyramidCount + 3);
                triangles.Add(16 * pyramidCount + 4);
                triangles.Add(16 * pyramidCount + 5);

                triangles.Add(16 * pyramidCount + 6);
                triangles.Add(16 * pyramidCount + 7);
                triangles.Add(16 * pyramidCount + 8);

                triangles.Add(16 * pyramidCount + 9);
                triangles.Add(16 * pyramidCount + 10);
                triangles.Add(16 * pyramidCount + 11);

                triangles.Add(16 * pyramidCount + 12);
                triangles.Add(16 * pyramidCount + 14);
                triangles.Add(16 * pyramidCount + 13);

                triangles.Add(16 * pyramidCount + 12);
                triangles.Add(16 * pyramidCount + 15);
                triangles.Add(16 * pyramidCount + 14);

                pyramidCount += 1;

                switch (p.pyramid)
                {
                    case 0:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 1:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    case 2:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 3:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 4:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 5:
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));

                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        vertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    default:
                        Debug.LogError("ERROR invalid pyramid!");
                        break;
                }
            }
            //Debug.Log("vertices count: " + vertices.Count);
            //Debug.Log("triangles count: " + triangles.Count);
        }

    }
}