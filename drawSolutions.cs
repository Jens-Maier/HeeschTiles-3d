using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.IO;
using System;


namespace drawSolutionsNamespace
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
    public class drawSolutions : MonoBehaviour
    {
        public int shapeNr;
        private int lastShapeNr = -1;

        public int shapeID;
        public List<int> shapeIDs;

        public int displayNeighborTile;

        public List<pyramidCoord> pyramids;
        public List<pyramidCoord> surroundPyramids;

        public List<pyramidCoord> meshPyramids;
        public List<pyramidCoord> surroundPyramidsS1;
        public List<pyramidCoord> surroundPyramidsS2;
        public List<List<pyramidCoord>> neighborTilePyramids;

        public List<List<pyramidCoord>> allBases;
        public List<List<pyramidCoord>> pyramidSurround;
        public List<List<posRot>> allS1;
        public List<List<posRot>> allS2;

        List<Vector3> meshVertices;
        List<int> meshTriangles;

        List<Vector3> surroundVerticesS1;
        List<int> surroundTrianglesS1;

        List<Vector3> surroundVerticesS2;
        List<int> surroundTrianglesS2;

        List<Vector3> neighborTilePyramidsVertices;
        List<int> neighborTilePyramidsTriangles;


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

        public Mesh surroundMesh2;
        public MeshFilter surroundMeshFilter2;
        public MeshRenderer surroundMeshRenderer2;
        public GameObject surroundGO2;

        public Mesh neighborTilePyramidsMesh;
        public MeshFilter neighborTilePyramidsMeshFilter;
        public MeshRenderer neighborTilePyramidsMeshRenderer;
        public GameObject neighborTilePyramidsGO;

        public Material material;
        public Material surroundMaterial1;
        public Material surroundMaterial2;


        //string path = Application.dataPath + "/Scripts/all_solutions_heesch1_tile.txt"; // -> tiles
        //string path = Application.dataPath + "/Scripts/corona_cells.txt"; // -> pyramids
        string path = Application.dataPath + "/Scripts/heesch_solver_summary.txt";

        int nrFrames = 20;
        int frameCounter = 1;

        void Start()
        {
            material = new Material(Shader.Find("Autodesk Interactive"));
            material.color = Color.red;
            Shader surroundShader = Shader.Find("Autodesk Interactive");
            if (surroundShader == null)
            {
                Debug.LogError("Shader 'Autodesk Interactive' not found. Please ensure it is included in the project's 'Always Included Shaders' list.");
                return;
            }
            surroundMaterial1 = new Material(surroundShader);
            surroundMaterial1.color = new Color(0.3f, 0.3f, 1f);
            surroundMaterial2 = new Material(surroundShader);
            surroundMaterial2.color = new Color(1f, 0.3f, 0.3f);

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = material;

            surroundGO = new GameObject();
            surroundGO.transform.position = new Vector3(0f, 0f, 5f);
            surroundGO.AddComponent<MeshFilter>();
            surroundGO.AddComponent<MeshRenderer>();
            surroundMeshRenderer = surroundGO.GetComponent<MeshRenderer>();
            surroundMeshRenderer.material = surroundMaterial1;

            surroundGO2 = new GameObject();
            surroundGO2.transform.position = new Vector3(0f, 0f, 15f);
            surroundGO2.AddComponent<MeshFilter>();
            surroundGO2.AddComponent<MeshRenderer>();
            surroundMeshRenderer2 = surroundGO2.GetComponent<MeshRenderer>();
            surroundMeshRenderer2.material = surroundMaterial2;

            neighborTilePyramidsGO = new GameObject();
            neighborTilePyramidsGO.transform.position = new Vector3(0f, 0f, 10f);
            neighborTilePyramidsGO.AddComponent<MeshFilter>();
            neighborTilePyramidsGO.AddComponent<MeshRenderer>();
            neighborTilePyramidsMeshRenderer = neighborTilePyramidsGO.GetComponent<MeshRenderer>();
            neighborTilePyramidsMeshRenderer.material = material;
            

            pyramids = new List<pyramidCoord>();
            surroundPyramids = new List<pyramidCoord>();

            allBases = new List<List<pyramidCoord>>();
            pyramidSurround = new List<List<pyramidCoord>>();
            allS1 = new List<List<posRot>>();
            allS2 = new List<List<posRot>>();
            shapeIDs = new List<int>();

            vertices = new List<Vector3>();
            triangles = new List<int>();

            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();

            surroundVertices = new List<Vector3>();
            surroundTriangles = new List<int>();

            surroundMeshFilter = surroundGO.GetComponent<MeshFilter>();
            surroundMesh = new Mesh();

            surroundMeshFilter2 = surroundGO2.GetComponent<MeshFilter>();
            surroundMesh2 = new Mesh();

            neighborTilePyramidsMeshFilter = neighborTilePyramidsGO.GetComponent<MeshFilter>();
            neighborTilePyramidsMesh = new Mesh();
            

            //pyramids = generateSingleTilePyramidCoords(new int3(0, 0, 0), 0);
            

            //ImportTilesFromFile(path);
            //ImportPyramidsFromFile(path);
            //ImportFromSolverLog(path);
            //parsePyramidSurround(path);
            parseNeighborTilePositions(path);



            //Debug.Log("allBases.Count: " + allBases.Count + ", shapeNr: " + shapeNr);
            //pyramids = new List<pyramidCoord>(allBases[shapeNr]);
            //
            //generateMesh(vertices, triangles, pyramids);
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

            shapeNr = 0;

            nrFrames = 100;
            frameCounter = 1;
        }

        void Update()
        {
            //if (frameCounter % nrFrames == 0)
            //{
            //    shapeNr++;
            //    shapeNr %= shapeIDs.Count;
            //}
            //frameCounter++;

            if (shapeNr != lastShapeNr)
            {
                shapeNr = shapeNr % allBases.Count;
                lastShapeNr = shapeNr;
                pyramids = allBases[shapeNr];
                generateMesh(vertices, triangles, pyramids);
                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                meshFilter.mesh = mesh;

                surroundMesh.Clear();
            }
            TryBuildShape();
            surroundMesh.SetVertices(surroundVerticesS1);
            surroundMesh.SetTriangles(surroundTrianglesS1, 0);
            surroundMesh.RecalculateNormals();
            surroundMeshFilter.mesh = surroundMesh;
            Debug.Log("generated S1 mesh!");
// 
            // // S2
            // List<Vector3> surroundVerticesS2 = new List<Vector3>();
            // List<int> surroundTrianglesS2 = new List<int>();
            // List<pyramidCoord> surroundPyramidsS2 = new List<pyramidCoord>();
            // foreach (posRot pr in allS2[shapeNr])
            // {
            //     surroundPyramidsS2.AddRange(generateSingleTilePyramidCoords(pr.pos, pr.rot));
            // }
            // generateMesh(surroundVerticesS2, surroundTrianglesS2, surroundPyramidsS2);
// 
            
            surroundMesh2.Clear();
            surroundMesh2.SetVertices(surroundVerticesS2);
            surroundMesh2.SetTriangles(surroundTrianglesS2, 0);
            surroundMesh2.RecalculateNormals();
            surroundMeshFilter2.mesh = surroundMesh2;
            

            // if (shapeNr != lastShapeNr)
            // {
            //     TryBuildShape();
            //     lastShapeNr = shapeNr;
            //     if (allBases != null)
            //     {
            //         Debug.Log("allBases.Count: " + allBases.Count + ", shapeNr: " + shapeNr);
            //     }
            //     else
            //     {
            //         Debug.Log("allBases is null!");
            //     }
            //     if (shapeIDs != null && shapeNr >= 0 && shapeNr < shapeIDs.Count)
            //     {
            //         shapeID = shapeIDs[shapeNr];
            //     }
            //     pyramids = new List<pyramidCoord>(allBases[shapeNr]);
            //     Debug.Log("base tile made of " + pyramids.Count + " pyramids");
            //     foreach (pyramidCoord p in pyramids)
            //     {
            //         Debug.Log("pyramid: (" + p.pos.x + ", " + p.pos.y + ", " + p.pos.z + "), " + p.pyramid);
            //     }
            //     generateMesh(vertices, triangles, pyramids);
            //     mesh.Clear();
            //     mesh.SetVertices(vertices);
            //     mesh.SetTriangles(triangles, 0);
            //     mesh.RecalculateNormals();
            //     meshFilter.mesh = mesh;
            // 
        }

        public static int3 add(int3 a, int3 b)
        {
            return new int3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public void parseNeighborTilePositions(string filePath)
        {
            Debug.Log("in parseNeighborTilePositions()");
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at: {filePath}");
                return;
            }
            else
            {
                Debug.Log($"File found at: {filePath}");
            }

            allBases.Clear();
            pyramidSurround.Clear();
            allS1.Clear();
            allS2.Clear();
            shapeIDs.Clear();
            neighborTilePyramids = new List<List<pyramidCoord>>();

            string[] lines = File.ReadAllLines(filePath);
            Debug.Log("nr lines: " + lines.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Shape:"))
                {
                    Debug.Log("parsing line " + lines[i] + ": 'Shape': ");

                    string[] parts = lines[i].Split(": ");
                    foreach (string part in parts)
                    {
                        Debug.Log("part: " + part);
                    }

                    List<pyramidCoord> shape = ParsePyramidCoordList(parts[1]);
                    allBases.Add(shape);
                }

                if (lines[i].StartsWith("Surround:"))
                {
                    Debug.Log("parsing line " + lines[i] + ": 'Surround': ");

                    string[] parts = lines[i].Split(": ");


                    List<pyramidCoord> surroundPyramids = ParsePyramidCoordList(parts[1]);
                    pyramidSurround.Add(surroundPyramids);
                }

                if (lines[i].StartsWith("Neighbor Tile Positions:"))
                {
                    List<posRot> neighborTilePositions = ParsePosRotList(lines[i]);
                    allS2.Add(neighborTilePositions);
                }

                if (lines[i].StartsWith("Neighbor Tile Pyramids:"))
                {
                    Debug.Log("parsing line " + lines[i] + ": 'Neighbor Tile Pyramids': ");
                    neighborTilePyramids.Add(ParsePyramidCoordList(lines[i]));
                    Debug.Log("parsed neighborTilePyramids: " + neighborTilePyramids.Count + " pyramids");
                }
            }
        }

        public void parsePyramidSurround(string filePath)
        {
            Debug.Log("in parsePyramidSurround()");
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at: {filePath}");
                return;
            }
            else
            {
                Debug.Log($"File found at: {filePath}");
            }

            allBases.Clear();
            pyramidSurround.Clear();
            allS1.Clear();
            allS2.Clear();
            shapeIDs.Clear();

            string[] lines = File.ReadAllLines(filePath);
            Debug.Log("nr lines: " + lines.Length);

            int currentShapeID = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Shape:"))
                {
                    Debug.Log("parsing line " + lines[i] + ": 'Shape': ");
                    string[] parts = lines[i].Split(": ");
                    foreach (string part in parts)
                    {
                        Debug.Log("part: " + part);
                    }

                    List<pyramidCoord> shape = ParsePyramidCoordList(parts[1]);
                    allBases.Add(shape);
                    foreach (pyramidCoord p in shape)
                    {
                        Debug.Log("(" + p.pos.x + ", " + p.pos.y + ", " + p.pos.z + "), " + p.pyramid);
                    }
                }

                if (lines[i].StartsWith("Surround:"))
                {
                    Debug.Log("parsing line " + lines[i] + ": 'Surround': ");
                    string[] parts = lines[i].Split(": ");

                    List<pyramidCoord> newPyramidSurround = ParsePyramidCoordList(parts[1]);
                    pyramidSurround.Add(newPyramidSurround);
                }
            }
        }

        public void ImportFromSolverLog(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found at: {filePath}");
                return;
            }
            else
            {
                Debug.Log($"File found at: {filePath}");
            }

            allBases.Clear();
            allS1.Clear();
            allS2.Clear();
            shapeIDs.Clear();

            string[] lines = File.ReadAllLines(filePath);

            int currentShapeID = -1;
            
            // Candidate solution for the current shape
            List<pyramidCoord> candidateBase = new List<pyramidCoord>();
            List<posRot> candidateS1 = new List<posRot>();
            List<posRot> candidateS2 = new List<posRot>();
            
            bool foundInfeasibleS2 = false;
            bool foundInfeasibleS3 = false;

            // Temporary variables for the current block being parsed
            List<pyramidCoord> tempBase = new List<pyramidCoord>();
            int tempCorona = 0;
            bool tempOptimal = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("Testing Shape"))
                {
                    Debug.Log("parsing line " + line + ": 'Testing Shape': ");
                    string[] parts = line.Split(' ');
                    int shapeID = -1;
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedId))
                    {
                        shapeID = parsedId;
                        Debug.Log("parsed shapeID: " + shapeID);
                    }

                    if (shapeID != currentShapeID)
                    {
                        // Commit previous shape if valid
                        if (currentShapeID != -1)
                        {
                            if ((foundInfeasibleS2 || foundInfeasibleS3) && candidateBase.Count > 0)
                            {
                                allBases.Add(new List<pyramidCoord>(candidateBase));
                                allS1.Add(new List<posRot>(candidateS1));
                                allS2.Add(new List<posRot>(candidateS2));
                                shapeIDs.Add(currentShapeID);
                                Debug.Log("committing previous shape");
                            }
                        }

                        // Reset for new shape
                        currentShapeID = shapeID;
                        candidateBase.Clear();
                        candidateS1.Clear();
                        candidateS2.Clear();
                        foundInfeasibleS2 = false;
                        foundInfeasibleS3 = false;
                        Debug.Log("parsed new shapeID, saving previous shape");
                    }
                    
                    // Reset temp block variables
                    tempBase.Clear();
                    tempCorona = 0;
                    tempOptimal = false;
                }
                else if (line.StartsWith("Pyramids:"))
                {
                    tempBase = ParsePyramidCoordList(line);
                    Debug.Log("parsed pyramids, line: " + line);
                }
                else if (line.StartsWith("Coronas:"))
                {
                    string[] parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        int.TryParse(parts[1].Trim().Split(' ')[0], out tempCorona);
                    }
                    Debug.Log("parsed line 'Coronas:', tempCorona: " + tempCorona);
                }
                else if (line.Contains("Solver Status:"))
                {
                    Debug.Log("parsed line 'Solver Status:': " + line);
                    if (line.Contains("INFEASIBLE"))
                    {
                        if (tempCorona == 2) 
                        {
                            Debug.Log("parsed 'INFEASIBLE', setting foundInfeasibleS2 to true");
                            foundInfeasibleS2 = true;
                        }
                        if (tempCorona == 3) 
                        {
                            foundInfeasibleS3 = true;
                            Debug.Log("parsed 'INFEASIBLE', setting foundInfeasibleS3 to true");
                        }
                    }
                    else if (line.Contains("OPTIMAL"))
                    {
                        tempOptimal = true;
                        Debug.Log("parsed 'OPTIMAL', setting tempOptimal to true");

                        if (tempCorona == 2) 
                        {
                            Debug.Log("parsed 'OPTIMAL', setting foundInfeasibleS2 to false");
                            foundInfeasibleS2 = false;
                        }
                        if (tempCorona == 3) 
                        {
                            Debug.Log("parsed 'OPTIMAL', setting foundInfeasibleS3 to false");
                            foundInfeasibleS3 = false;
                        }
                    }
                }
                else if (line.StartsWith("S1:") && tempOptimal)
                {
                    Debug.Log("parsed line 'S1:' and tempOptimal is true");
                    candidateBase = new List<pyramidCoord>(tempBase);
                    candidateS1 = ParsePosRotList(line);
                    Debug.Log("parsed S1 setting candidateS1 to line: " + line);
                    
                    if (tempCorona == 1)
                    {
                        candidateS2.Clear();
                        Debug.Log("tempCorona == 1, clearing candidateS2");
                    }
                }
                else if (line.StartsWith("S2:") && tempOptimal)
                {
                    Debug.Log("parsed line 'S2:' and tempOptimal is true");
                    candidateS2 = ParsePosRotList(line);
                    Debug.Log("parsed S2 setting candidateS2 to line: " + line);
                }
            }

            // Handle the last shape in the file
            if (currentShapeID != -1)
            {
                if ((foundInfeasibleS2 || foundInfeasibleS3) && candidateBase.Count > 0)
                {
                    Debug.Log("foundInfeasibleS2 or foundInfeasibleS3 -> adding to allBases, allS1, allS2");
                    allBases.Add(new List<pyramidCoord>(candidateBase));
                    allS1.Add(new List<posRot>(candidateS1));
                    allS2.Add(new List<posRot>(candidateS2));
                    shapeIDs.Add(currentShapeID);
                }
                else
                {
                    Debug.Log("not foundInfeasibleS2 or foundInfeasibleS3 -> not adding to allBases, allS1, allS2");
                }
            }

            Debug.Log($"Imported {allBases.Count} shapes.");
            //if (allBases.Count > 0)
            //{
            //    TryBuildShape();
            //}
        }

        private List<pyramidCoord> ParsePyramidCoordList(string line)
        {
            List<pyramidCoord> result = new List<pyramidCoord>();

            int start = line.IndexOf('[');
            int end = line.LastIndexOf(']');

            if (start < 0 || end < 0) return result;

            string listContent = line.Substring(start + 1, end - start - 1);

            string[] entries = listContent.Split(new string[] { "), (" }, StringSplitOptions.None);

            foreach (string entryRaw in entries)
            {
                string entry = entryRaw.Replace("(", "").Replace(")", "").Replace(" ", "");
                string[] parts = entry.Split(',');

                if (parts.Length == 4)
                {
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    int z = int.Parse(parts[2]);
                    int pyr = int.Parse(parts[3]);

                    result.Add(new pyramidCoord(new int3(x, y, z), pyr));
                }
            }
            return result;
        }

        private List<posRot> ParsePosRotList(string line)
        {
            List<posRot> result = new List<posRot>();

            int start = line.IndexOf('[');
            int end = line.LastIndexOf(']');

            if (start < 0 || end < 0) return result;

            string listContent = line.Substring(start + 1, end - start - 1);            

            string[] entries = listContent.Split(new string[] { "), (" }, StringSplitOptions.None);
            foreach (string entryRaw in entries)            
            {
                string entry = entryRaw.Replace("(", "").Replace(")", "").Replace(" ",      "");
                string[] parts = entry.Split(',');

                if (parts.Length == 4)
                {
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    int z = int.Parse(parts[2]);
                    int r = int.Parse(parts[3]);

                    result.Add(new posRot(new int3(x, y, z), r));
                }
            }
            return result;
        }

        private void TryBuildShape()
        {
            if (allBases == null) return;
            if (allBases.Count == 0) return;
            
            // Ensure shapeNr is within bounds
            if (shapeNr < 0) shapeNr = 0;
            if (shapeNr >= allBases.Count) shapeNr = allBases.Count - 1;

            Debug.Log("shapeNr: " + shapeNr + ", pyramidSurround count: " + pyramidSurround.Count);
            surroundVerticesS1 = new List<Vector3>();
            surroundTrianglesS1 = new List<int>();

            generateMesh(surroundVerticesS1, surroundTrianglesS1, pyramidSurround[shapeNr]);

            surroundPyramidsS2 = new List<pyramidCoord>();
            surroundVerticesS2 = new List<Vector3>();
            surroundTrianglesS2 = new List<int>();
            Debug.Log("displayNeighborTile: " + displayNeighborTile + ", pos: " + allS2[shapeNr][displayNeighborTile].pos + ", rot: " + allS2[shapeNr][displayNeighborTile].rot);
            generateMesh(surroundVerticesS2, surroundTrianglesS2, generateSingleTilePyramidCoords(allS2[shapeNr][displayNeighborTile].pos, allS2[shapeNr][displayNeighborTile].rot));


            // S1
            // List<Vector3> surroundVerticesS1 = new List<Vector3>();
            // List<int> surroundTrianglesS1 = new List<int>();
            // List<pyramidCoord> surroundPyramidsS1 = new List<pyramidCoord>();
            // foreach (posRot pr in allS1[shapeNr])
            // {
            //     surroundPyramidsS1.AddRange(generateSingleTilePyramidCoords(pr.pos, pr.rot));
            // }
            // generateMesh(surroundVerticesS1, surroundTrianglesS1, surroundPyramidsS1);
// 
            surroundMesh.Clear();
            surroundMesh.SetVertices(surroundVerticesS1);
            surroundMesh.SetTriangles(surroundTrianglesS1, 0);
            surroundMesh.RecalculateNormals();
            surroundMeshFilter.mesh = surroundMesh;
            Debug.Log("generated S1 mesh!");

            
// 
            // // S2
            // List<Vector3> surroundVerticesS2 = new List<Vector3>();
            // List<int> surroundTrianglesS2 = new List<int>();
            // List<pyramidCoord> surroundPyramidsS2 = new List<pyramidCoord>();
            // foreach (posRot pr in allS2[shapeNr])
            // {
            //     surroundPyramidsS2.AddRange(generateSingleTilePyramidCoords(pr.pos, pr.rot));
            // }
            // generateMesh(surroundVerticesS2, surroundTrianglesS2, surroundPyramidsS2);
// 
            
            surroundMesh2.Clear();
            surroundMesh2.SetVertices(surroundVerticesS2);
            surroundMesh2.SetTriangles(surroundTrianglesS2, 0);
            surroundMesh2.RecalculateNormals();
            surroundMeshFilter2.mesh = surroundMesh2;
            // Debug.Log("generated S2 mesh!");

            neighborTilePyramidsVertices = new List<Vector3>();
            neighborTilePyramidsTriangles = new List<int>();
            generateMesh(neighborTilePyramidsVertices, neighborTilePyramidsTriangles, neighborTilePyramids[shapeNr]);

            neighborTilePyramidsMesh.Clear();
            neighborTilePyramidsMesh.SetVertices(neighborTilePyramidsVertices);
            neighborTilePyramidsMesh.SetTriangles(neighborTilePyramidsTriangles, 0);
            neighborTilePyramidsMesh.RecalculateNormals();
            neighborTilePyramidsMeshFilter.mesh = neighborTilePyramidsMesh;
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
                Debug.Log($"parsed {lines.Length} lines rom file: {filePath}");

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
            List<pyramidCoord> tempPyramidCoords = new List<pyramidCoord>();

            if (allBases != null && shapeNr >= 0 && shapeNr < allBases.Count)
            {
                foreach (pyramidCoord p in allBases[shapeNr])
                {
                    tempPyramidCoords.Add(new pyramidCoord(add(pos, p.pos), p.pyramid));
                }
            }

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
                        Debug.LogError("ERROR invalid pyramid!: " + p.pyramid);
                        break;
                }
            }
            //Debug.Log("vertices count: " + vertices.Count);
            //Debug.Log("triangles count: " + triangles.Count);
        }

    }
}