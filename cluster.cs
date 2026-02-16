using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

namespace surroundNamespace
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

    public enum draw
    {
        best,
        all
    };

    public struct matchingRule
    {
        public int3 pos;
        public int dir; // 0, 1, 2, 3, 4, 5
        public int rule; // +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

        public matchingRule(int3 position, int direction, int rul)
        {
            pos = position;
            dir = direction;
            rule = rul;
        }
    }

    public struct possibleTilePosition
    {
        public posRot posrot;
        public int overlap;
        public int nrFacesTouch;
        public bool squareFaceNeighbor;
        public int maxCubeDistance;

        // sort: 
        // 1. squareFaceNeighbor == true
        //      2. nrFacesTouchRootTile bigger
        //          3. rotation
        //              4. overlap
        //                  5. maxCubeDistance

        public possibleTilePosition(posRot position, int newOverlap, int newNrFacesTouch, bool squareNeighbor, int maxCubeDist)
        {
            posrot = position;
            overlap = newOverlap;
            nrFacesTouch = newNrFacesTouch;
            squareFaceNeighbor = squareNeighbor;
            maxCubeDistance = maxCubeDist;
        }
    }

    public struct node
    {
        public posRot placement;

        public HashSet<pyramidCoord> currentPyramids;

        public List<List<possibleTilePosition>> possibleTilePositions; // int overlap

        public List<matchingRule> nodeMatchingRules;

        public int currentNextIndex;

        public int pyramidsFilled;


        public node(posRot pr)
        {
            placement = pr;
            currentPyramids = new HashSet<pyramidCoord>();
            possibleTilePositions = new List<List<possibleTilePosition>>();
            nodeMatchingRules = new List<matchingRule>();
            currentNextIndex = 0;
            pyramidsFilled = 0;
        }
    }


    public class tile
    {
        public int3 pos;
        public int rot; // [0, ..., 23]
        public List<tile> next;
        public cluster clust;

        public List<pyramidCoord> pyramidCoords;
        public List<matchingRule> matchingRules;// +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

        public tile(int3 p, int r, cluster c)
        {
            pos = p;
            rot = r;
            clust = c;
            next = new List<tile>();
            pyramidCoords = new List<pyramidCoord>();
            pyramidCoords.AddRange(c.singleTilePyramidCoords[r]);
            for (int i = 0; i < pyramidCoords.Count; i++)
            {
                pyramidCoord tempPyrCoord = pyramidCoords[i];
                tempPyrCoord.pos.x = tempPyrCoord.pos.x + p.x;
                tempPyrCoord.pos.y = tempPyrCoord.pos.y + p.y;
                tempPyrCoord.pos.z = tempPyrCoord.pos.z + p.z;

                pyramidCoords[i] = tempPyrCoord;
            }
            matchingRules = new List<matchingRule>();
        }

        public static int3 add(int3 a, int3 b)
        {
            return new int3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
    }



    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class cluster : MonoBehaviour
    {
        public tile rootTile;
        public HashSet<pyramidCoord> pyramids;
        public List<pyramidCoord>[] singleTilePyramidCoords; //pyramids of all orientations of a single tile in pos (0,0,0)

        public HashSet<pyramidCoord> pyramidSurround;

        public List<pyramidCoord> pyramidSurroundForMesh;

        public List<possibleTilePosition> neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap; // reuse for each layer

        public List<Vector3> vertices;
        public List<int> triangles;

        public Mesh mesh;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        public Mesh surroundMesh;
        public MeshFilter surroundMeshFilter;

        public GameObject surround;

        public List<Vector3> surroundVertices;
        public List<int> surroundTriangles;

        public List<Vector3> bestSurroundVertices;
        public List<int> bestSurroundTriangles;

        public Material material;
        public Material surroundMaterial;

        public node[] surroundNodes;
        public int maxDepth;

        List<matchingRule> clusterMatchingRules;

        public int activeNodeIndex;

        public int maxPyramidsFilled;

        public bool onlyBestOptions;

        public List<Vector3> debgLstRed;
        public List<Vector3> debgLstGreen;
        public List<Vector3> debgLstBlue;

        public List<pyramidCoord> debgPyramidsGreen1;
        public List<pyramidCoord> debgPyramidsRed1;
        public List<pyramidCoord> debgPyramidsBlue1;

        public List<pyramidCoord> debgPyramidsGreen2;
        public List<pyramidCoord> debgPyramidsRed2;
        public List<pyramidCoord> debgPyramidsBlue2;


        public draw drawMode;
        [HideInInspector]
        public bool onlyOnce;

        [Range(-3, 2)]
        public int testPosX;
        [Range(-3, 2)]
        public int testPosY;
        [Range(-4, 3)]
        public int testPosZ;
        [Range(0, 23)]
        public int testRot;

        public int testOverlap;

        public int testNrFacesTouchRootTile;
        [HideInInspector]
        public bool tileFits;

        // Start is called before the first frame update
        void Start()
        {
            material = new Material(Shader.Find("Autodesk Interactive"));
            material.color = Color.red;
            surroundMaterial = new Material(Shader.Find("Autodesk Interactive"));
            surroundMaterial.color = new Color(0.3f, 0.3f, 1f);

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = material;

            surround = new GameObject();
            surround.AddComponent<MeshFilter>();
            surround.AddComponent<MeshRenderer>();

            surround.GetComponent<MeshFilter>().mesh = new Mesh();
            surround.GetComponent<MeshRenderer>().material = surroundMaterial;

            pyramids = new HashSet<pyramidCoord>();
            neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = new List<possibleTilePosition>();

            maxPyramidsFilled = 0;

            onlyOnce = false;

            pyramidSurround = new HashSet<pyramidCoord>();
            pyramidSurroundForMesh = new List<pyramidCoord>();

            debgLstRed = new List<Vector3>();
            debgLstGreen = new List<Vector3>();
            debgLstBlue = new List<Vector3>();
            debgPyramidsRed1 = new List<pyramidCoord>();
            debgPyramidsGreen1 = new List<pyramidCoord>();
            debgPyramidsBlue1 = new List<pyramidCoord>();
            debgPyramidsRed2 = new List<pyramidCoord>();
            debgPyramidsGreen2 = new List<pyramidCoord>();
            debgPyramidsBlue2 = new List<pyramidCoord>();


            vertices = new List<Vector3>();
            triangles = new List<int>();
            surroundVertices = new List<Vector3>();
            surroundTriangles = new List<int>();

            bestSurroundVertices = new List<Vector3>();
            bestSurroundTriangles = new List<int>();

            tileFits = false;


            singleTilePyramidCoords = new List<pyramidCoord>[24 * 3];
            for (int i = 0; i < 24 * 3; i++)
            {
                singleTilePyramidCoords[i] = new List<pyramidCoord>();

                singleTilePyramidCoords[i] = generateSingleTilePyramidCoords(new int3(0, 0, 0), i);
            }

            meshFilter = GetComponent<MeshFilter>();
            surroundMeshFilter = surround.GetComponent<MeshFilter>();

            mesh = new Mesh();

            surroundMesh = new Mesh();


            // TEST
            (int, int, int, int)[] testPyrs = {
            (0, -1, -1, 0),
            (0, -1, -1, 2),
            (0, -1, -1, 4),
            (0, -1, 0, 0),
            (0, -1, 0, 2),
            (0, -1, 0, 4),
            (0, -1, 0, 5),
            (0, -1, 1, 0),
            (0, -1, 1, 2),
            (0, -1, 1, 5),
            (0, 0, -1, 0),
            (0, 0, -1, 2),
            (0, 0, -1, 3),
            (0, 0, -1, 4),
            (0, 0, 0, 1),
            (0, 0, 0, 2),
            (0, 0, 0, 3),
            (0, 0, 0, 4),
            (0, 0, 0, 5),
            (0, 0, 1, 0),
            (0, 0, 1, 2),
            (0, 0, 1, 3),
            (0, 0, 1, 5),
            (0, 1, -1, 0),
            (0, 1, -1, 3),
            (0, 1, -1, 4),
            (0, 1, 0, 0),
            (0, 1, 0, 3),
            (0, 1, 0, 4),
            (0, 1, 0, 5),
            (0, 1, 1, 0),
            (0, 1, 1, 3),
            (0, 1, 1, 5),
            (1, -1, -1, 1),
            (1, -1, -1, 2),
            (1, -1, -1, 4),
            (1, -1, 0, 1),
            (1, -1, 0, 2),
            (1, -1, 0, 4),
            (1, -1, 0, 5),
            (1, -1, 1, 1),
            (1, -1, 1, 2),
            (1, -1, 1, 5),
            (1, 0, -1, 1),
            (1, 0, -1, 2),
            (1, 0, -1, 3),
            (1, 0, -1, 4),
            (1, 0, 0, 1),
            (1, 0, 0, 2),
            (1, 0, 0, 3),
            (1, 0, 0, 4),
            (1, 0, 0, 5),
            (1, 0, 1, 1),
            (1, 0, 1, 2),
            (1, 0, 1, 3),
            (1, 0, 1, 5),
            (1, 1, -1, 1),
            (1, 1, -1, 3),
            (1, 1, -1, 4),
            (1, 1, 0, 1),
            (1, 1, 0, 3),
            (1, 1, 0, 4),
            (1, 1, 0, 5),
            (1, 1, 1, 1),
            (1, 1, 1, 3),
            (1, 1, 1, 5)};

            foreach (var p in testPyrs)
            {
                pyramids.Add(new pyramidCoord(new int3(p.Item1, p.Item2, p.Item3), p.Item4));
            }


            // setup ----------

            // root tile
            // pyramids.Clear();
            // rootTile = new tile(new int3(0, 0, 0), 0, this);
// 
            // getAllPyramids();
            generateMesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
// 
            // // surround
            // calculateAllNeighborPyramids(pyramids);
            // surroundNodes = new node[pyramidSurround.Count];
            // activeNodeIndex = -1;
// 
// 
            // //Debug.Log("pyramids count : " + pyramids.Count); // 19 OK
            // clusterMatchingRules = generateClusterMatchingRules(pyramids);
// 
            // neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = calculateAllNeighborTilePositions(pyramids);


        }

        // Update is called once per frame
        //void Update()
        //{
        //    // add single tile OK
        //    //Debug.Log(" total neighbor tile positions count: " + //neighborTilePositionsWithNrFacesTouchingRootTile.Count);
//
        //    // test surround
        //    //
        //    //surroundRootNode = new node(neighborTilePositionsWithNrFacesTouchingRootTile[test].Item1);
        //    //surroundRootNode.currentPyramids = generateSingleTilePyramidCoords(surroundRootNode.placement.pos, //surroundRootNode.placement.rot);
        //    //activeNode = surroundRootNode;
//
        //    tilingStep();
//
        //    debgLstRed.Clear();
        //    debgLstGreen.Clear();
        //    debgLstBlue.Clear();
//
//
//
//
        //    debgPyramidsRed2.Clear();
        //    debgPyramidsGreen2.Clear();
        //    debgPyramidsBlue2.Clear();
//
//
        //    // for test:
        //    // neighborTilePositionsWithNrFacesTouchingRootTile = calculateAllNeighborTilePositions(pyramids);
        //    //
//
        //    // FUNKT !!!
        //    //
        //    // posRot testPos = new posRot(new int3(testPosX, testPosY, testPosZ), testRot);
        //    // bool fits = tileFitsToCluster(pyramids, testPos);
        //    // Debug.Log("tile fits: " + fits);
        //    // 
        //    // tileFits = fits;
//
        //    // draw test tile  ----------------------
        //    //
        //    // tileFits = false;
        //    // foreach (possibleTilePosition n in neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap)
        //    // {
        //    //     if (int3equal(n.posrot.pos, new int3(testPosX, testPosY, testPosZ)) == true && n.posrot.rot == //testRot)
        //    //     {
        //    //         tileFits = true;
        //    //         testNrFacesTouchRootTile = n.nrFacesTouch;
        //    //         testOverlap = n.overlap;
        //    // 
        //    //         node newNode = new node(new posRot(n.posrot.pos, n.posrot.rot));
        //    //         List<pyramidCoord> testPyramids = generateSingleTilePyramidCoords(n.posrot.pos, n.posrot.//rot);
        //    // 
        //    //         newNode.currentPyramids = testPyramids;
        //    //         node temp = activeNode;
        //    //         activeNode.next = newNode;
        //    //         activeNode = newNode;
        //    // 
        //    //         generateSurroundMesh();
        //    // 
        //    // 
        //    //         activeNode = temp;
        //    //     }
        //    // }
//
//
        //}

        public (bool, bool) tileFitsToCluster(HashSet<pyramidCoord> cluster, posRot newTilePos, List<matchingRule> clustMatchingRules)
        {
            // ---- test draw single surround test tile
            // surroundRootNode = new node(newTilePos);
            // surroundRootNode.currentPyramids = generateSingleTilePyramidCoords(surroundRootNode.placement.pos, surroundRootNode.placement.rot);
            // activeNode = surroundRootNode;
            // generateSurroundMesh();
            // ----

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

            // overlap
            List<pyramidCoord> tilePyramids = generateSingleTilePyramidCoords(newTilePos.pos, newTilePos.rot);
            bool overlap = false;
            foreach (pyramidCoord tilePyramid in tilePyramids)
            {
                if (cluster.Contains(tilePyramid) == true)
                {
                    overlap = true;
                    break;
                }
            }

            if (overlap == true)
            {
                //Debug.Log("overlap!");
                return (false, false);
            }

            List<matchingRule> tileRules = generateSingleTileMatchingRules(newTilePos, 2); // OK

            bool fits = true;
            bool squareRule = false;
            foreach (matchingRule tileRule in tileRules)
            {
                matchingRule tileOppositeRule = getOppositeRule(tileRule);

                foreach (matchingRule clustRule in clustMatchingRules)
                {
                    if (int3equal(clustRule.pos, tileOppositeRule.pos) == true && clustRule.dir == tileOppositeRule.dir && clustRule.rule == 0 && tileOppositeRule.rule == 0)
                    {
                        squareRule = true;
                    }

                    if (int3equal(clustRule.pos, tileOppositeRule.pos) == true && clustRule.dir == tileOppositeRule.dir && clustRule.rule != tileOppositeRule.rule)
                    {
                        fits = false;
                        return (false, squareRule);
                    }
                }

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

                // TODO: next neighbor rule // TODO: ERROR HERE !!! FIX!!!

                // // +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor
                // switch (tileRule.dir)
                // {
                //     case 0:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(1, 0, 0))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                //     case 1:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(-1, 0, 0))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                //     case 2:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(0, 1, 0))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                //     case 3:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(0, -1, 0))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                //     case 4:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(0, 0, 1))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                //     case 5:
                //         foreach (matchingRule clustRule in clustMatchingRules)
                //         {
                //             if (int3equal(clustRule.pos, add(tileOppositeRule.pos, new int3(0, 0, -1))) == true && clustRule.rule == tileRule.rule)
                //             {
                //                 return (false, squareRule);
                //             }
                //         }
                //         break;
                // }  
            }
            return (fits, squareRule);
        }

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

        // dir: 0, 1, 2, 3, 4, 5
        // rule: +1, 0, -1 of cluster

        matchingRule getOppositeRule(matchingRule rule)
        {
            int3 oppositePos = new int3(0, 0, 0);
            int oppositeDir = 9999;
            int oppositeRule = -rule.rule;

            switch (rule.dir)
            {
                case 0:
                    oppositePos = add(rule.pos, new int3(1, 0, 0));
                    oppositeDir = 1;
                    break;
                case 1:
                    oppositePos = add(rule.pos, new int3(-1, 0, 0));
                    oppositeDir = 0;
                    break;
                case 2:
                    oppositePos = add(rule.pos, new int3(0, 1, 0));
                    oppositeDir = 3;
                    break;
                case 3:
                    oppositePos = add(rule.pos, new int3(0, -1, 0));
                    oppositeDir = 2;
                    break;
                case 4:
                    oppositePos = add(rule.pos, new int3(0, 0, 1));
                    oppositeDir = 5;
                    break;
                case 5:
                    oppositePos = add(rule.pos, new int3(0, 0, -1));
                    oppositeDir = 4;
                    break;
                default:
                    Debug.Log("ERROR: invalid rule!");
                    break;
            }
            return new matchingRule(oppositePos, oppositeDir, oppositeRule);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            if (debgLstBlue != null)
            {
                foreach (Vector3 p in debgLstBlue)
                {
                    Gizmos.DrawSphere(p, 0.3f);
                }
            }

            Gizmos.color = Color.green;

            if (debgPyramidsGreen1 != null)
            {
                float size = 0.05f;

                foreach (pyramidCoord p in debgPyramidsGreen1)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), size);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), size);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), size);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), size);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), size);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), size);
                            break;
                    }
                }
            }

            if (debgPyramidsGreen2 != null)
            {
                float size = 0.05f;

                foreach (pyramidCoord p in debgPyramidsGreen2)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), size);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), size);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), size);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), size);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), size);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), size);
                            break;
                    }
                }
            }

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

            Gizmos.color = Color.red;

            if (debgLstRed != null)
            {
                foreach (Vector3 p in debgLstRed)
                {
                    Gizmos.DrawSphere(p, 0.03f);
                }
            }

            if (debgPyramidsRed1 != null)
            {
                foreach (pyramidCoord p in debgPyramidsRed1)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), 0.03f);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), 0.03f);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), 0.03f);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), 0.03f);
                            break;
                    }
                }
            }

            if (debgPyramidsRed2 != null)
            {
                foreach (pyramidCoord p in debgPyramidsRed2)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), 0.03f);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), 0.03f);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), 0.03f);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), 0.03f);
                            break;
                    }
                }
            }

            Gizmos.color = Color.blue;

            if (debgPyramidsBlue1 != null)
            {
                foreach (pyramidCoord p in debgPyramidsBlue1)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), 0.06f);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), 0.06f);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), 0.06f);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), 0.06f);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), 0.06f);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), 0.06f);
                            break;
                    }
                }
            }

            if (debgPyramidsBlue2 != null)
            {
                foreach (pyramidCoord p in debgPyramidsBlue2)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), 0.03f);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), 0.03f);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), 0.03f);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), 0.03f);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), 0.03f);
                            break;
                    }
                }
            }

            Gizmos.color = Color.green;

            if (debgLstGreen != null)
            {
                foreach (Vector3 p in debgLstGreen)
                {
                    Gizmos.DrawSphere(p, 0.05f);
                }
            }
        }

        public static int3 add(int3 a, int3 b)
        {
            return new int3(a.x + b.x, a.y + b.y, a.z + b.z);
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

        public List<matchingRule> generateClusterMatchingRules(HashSet<pyramidCoord> pyramids)
        {
            List<matchingRule> rules = new List<matchingRule>();

            List<(int3, int)> cubePosCount = new List<(int3, int)>();
            List<int3> cubePos = new List<int3>();

            foreach (pyramidCoord coord in pyramids)
            {
                if (cubePos.Exists(p => int3equal(p, coord.pos)) == false)
                {
                    cubePos.Add(coord.pos);
                    cubePosCount.Add((coord.pos, 1));
                }
                else
                {
                    int index = cubePosCount.FindIndex(x => (x.Item1.x == coord.pos.x && x.Item1.y == coord.pos.y && x.Item1.z == coord.pos.z));
                    (int3, int) tempCubePosCount = cubePosCount[index];
                    tempCubePosCount.Item2 += 1;
                    cubePosCount[index] = tempCubePosCount;
                }
            }
            return generateMatchingRules(cubePosCount, pyramids, 1);
        }

        public List<matchingRule> generateSingleTileMatchingRules(posRot tilePos, int display)
        {
            List<pyramidCoord> tilePyramids = generateSingleTilePyramidCoords(tilePos.pos, tilePos.rot);
            List<(int3, int)> cubePosCount = new List<(int3, int)>();
            List<int3> cubePos = new List<int3>();

            foreach (pyramidCoord coord in tilePyramids)
            {
                if (cubePos.Exists(p => int3equal(p, coord.pos)) == false)
                {
                    cubePos.Add(coord.pos);
                    cubePosCount.Add((coord.pos, 1));
                }
                else
                {
                    int index = cubePosCount.FindIndex(x => (x.Item1.x == coord.pos.x && x.Item1.y == coord.pos.y && x.Item1.z == coord.pos.z));
                    (int3, int) tempCubePosCount = cubePosCount[index];
                    tempCubePosCount.Item2 += 1;
                    cubePosCount[index] = tempCubePosCount;
                }
            }
            HashSet<pyramidCoord> tilePyramidsSet = new HashSet<pyramidCoord>();
            tilePyramidsSet.UnionWith(tilePyramids);

            return generateMatchingRules(cubePosCount, tilePyramidsSet, display);
        }

        public static bool int3equal(int3 a, int3 b)
        {
            if (a.x == b.x && a.y == b.y && a.z == b.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<matchingRule> generateMatchingRules(List<(int3, int)> cubePosCount, HashSet<pyramidCoord> tilePyramids, int display)
        {
            List<matchingRule> rules = new List<matchingRule>();

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

            foreach ((int3, int) posCount in cubePosCount)
            {
                if (posCount.Item2 > 1) // only pyramids inside 3 cubes
                {
                    for (int i = 0; i < 6; i++)
                    {
                        //(tilePyramids.Exists(new pyramidCoord(posCount.Item1, i)))
                        if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, i)) == true)
                        {
                            // ( + or | )
                            switch (i)
                            {
                                case 0:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 1)) == false) //opposite pyramid not in tilePyramids
                                    //if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 1) == false) 
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 1)) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == true)
                                        //if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == true) //<- same ERROR HERE...
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == true &&
                                        //    tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 0) == false)
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == true && 
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 0)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == false &&
                                        //    tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 0) == false)
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == false && 
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 0)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 1:
                                    //if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 0) == false)
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 0)) == false)
                                    {
                                        //if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 0) == true)
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == true &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 1)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == false &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 1)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 2:
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 3)) == false)
                                    {
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == true &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 2)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == false &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 2)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 3:
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 2)) == false)
                                    {
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        else
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == true &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 3)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == false &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 3)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 4:
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 5)) == false)
                                    {
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == true &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 4)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == false &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 4)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 5:
                                    if (tilePyramids.Contains(new pyramidCoord(posCount.Item1, 4)) == false)
                                    {
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == true &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 5)) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        if (tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == false &&
                                            tilePyramids.Contains(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 5)) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            // ( - )
                            rules.Add(new matchingRule(posCount.Item1, i, -1));
                            //Debug.Log("dir: " + i + " matching rule -");

                            //public matchingRule(int3 position, int direction, int rul)
                            //dir: 0, 1, 2, 3, 4, 5
                            // rule: +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor
                        }
                    }
                }

                // debug
                //if (int3equal(posCount.Item1, new int3(testPosX, testPosY, testPosZ)) == true && posCount.Item2 == testRot)
                //{
                //if (display == 1)
                //{
                //    debgPyramidsRed1.Clear();
                //    debgPyramidsGreen1.Clear();
                //    debgPyramidsBlue1.Clear();
                //    foreach (matchingRule rule in rules)
                //    {
                //        if (rule.rule == 1)
                //        {
                //            debgPyramidsRed1.Add(new pyramidCoord(rule.pos, rule.dir));
                //        }
                //        if (rule.rule == -1)
                //        {
                //            debgPyramidsGreen1.Add(new pyramidCoord(rule.pos, rule.dir));
                //        }
                //        //if (rule.rule == 0)
                //        //{
                //        //    debgPyramidsBlue1.Add(new pyramidCoord(rule.pos, rule.dir));
                //        //    //Debug.Log("debug blue");
                //        //}
                //
                //    }
                //}
                //if (display == 2)
                //{
                //    debgPyramidsRed2.Clear();
                //    debgPyramidsGreen2.Clear();
                //    debgPyramidsBlue2.Clear();
                //    foreach (matchingRule rule in rules)
                //    {
                //        if (rule.rule == 1)
                //        {
                //            debgPyramidsRed2.Add(new pyramidCoord(rule.pos, rule.dir));
                //        }
                //        if (rule.rule == -1)
                //        {
                //            debgPyramidsGreen2.Add(new pyramidCoord(rule.pos, rule.dir));
                //        }
                //        if (rule.rule == 0)
                //        {
                //            //debgPyramidsBlue2.Add(new pyramidCoord(rule.pos, rule.dir));
                //            //Debug.Log("debug blue");
                //        }
                //
                //    }
                //}
            }
            //Debug.Log("matchingRules count: " + rules.Count + " | tilePyramids count: " + tilePyramids.Count); // count 14, pyramids 19
            return rules;
        }

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

        void generateMesh()
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

        void generateSurroundMesh()
        {
            surroundTriangles.Clear();
            surroundVertices.Clear();
            pyramidSurroundForMesh.Clear(); // test
            if (activeNodeIndex != -1)
            {
                if (surroundNodes[activeNodeIndex].currentPyramids != null)
                {
                    pyramidSurroundForMesh.AddRange(surroundNodes[activeNodeIndex].currentPyramids);
                }
                //else
                //{
                //    //Debug.Log("activeNode.currentPyramids is null!");
                //}
            }
            //else
            //{
            //    //Debug.Log("activeNode is null!");
            //}

            int pyramidCount = 0;
            foreach (pyramidCoord p in pyramidSurroundForMesh)
            {
                surroundTriangles.Add(16 * pyramidCount + 0);
                surroundTriangles.Add(16 * pyramidCount + 1);
                surroundTriangles.Add(16 * pyramidCount + 2);

                surroundTriangles.Add(16 * pyramidCount + 3);
                surroundTriangles.Add(16 * pyramidCount + 4);
                surroundTriangles.Add(16 * pyramidCount + 5);

                surroundTriangles.Add(16 * pyramidCount + 6);
                surroundTriangles.Add(16 * pyramidCount + 7);
                surroundTriangles.Add(16 * pyramidCount + 8);

                surroundTriangles.Add(16 * pyramidCount + 9);
                surroundTriangles.Add(16 * pyramidCount + 10);
                surroundTriangles.Add(16 * pyramidCount + 11);

                surroundTriangles.Add(16 * pyramidCount + 12);
                surroundTriangles.Add(16 * pyramidCount + 14);
                surroundTriangles.Add(16 * pyramidCount + 13);

                surroundTriangles.Add(16 * pyramidCount + 12);
                surroundTriangles.Add(16 * pyramidCount + 15);
                surroundTriangles.Add(16 * pyramidCount + 14);

                pyramidCount += 1;

                switch (p.pyramid)
                {
                    case 0:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 1:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    case 2:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 3:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 4:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 5:
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));

                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    default:
                        Debug.LogError("ERROR invalid pyramid!");
                        break;
                }
            }
            int maxTriIndex = 0;
            foreach (int t in surroundTriangles)
            {
                if (maxTriIndex < t)
                {
                    maxTriIndex = t;
                }
            }

            if (surroundVertices.Count > maxTriIndex)
            {
                surroundMesh.Clear();

                surroundMesh.SetVertices(surroundVertices);
                surroundMesh.SetTriangles(surroundTriangles, 0);
                surroundMesh.RecalculateNormals();
                surroundMeshFilter.mesh = surroundMesh;
                //Debug.Log("added tile to surroundMesh!");
            }

            pyramidSurroundForMesh.Clear();
        }

        void generateBestSurroundMesh(bool recalculate)
        {
            if (recalculate)
            {
                bestSurroundTriangles.Clear();
                bestSurroundVertices.Clear();

                if (activeNodeIndex != -1)
                {
                    if (surroundNodes[activeNodeIndex].currentPyramids != null)
                    {
                        pyramidSurroundForMesh.AddRange(surroundNodes[activeNodeIndex].currentPyramids);
                    }
                    else
                    {
                        Debug.Log("activeNode.currentPyramids is null!");
                    }
                }
                else
                {
                    Debug.Log("ERROR! activeNode is null!");
                }
            }

            int pyramidCount = 0;
            foreach (pyramidCoord p in pyramidSurroundForMesh)
            {
                bestSurroundTriangles.Add(16 * pyramidCount + 0);
                bestSurroundTriangles.Add(16 * pyramidCount + 1);
                bestSurroundTriangles.Add(16 * pyramidCount + 2);

                bestSurroundTriangles.Add(16 * pyramidCount + 3);
                bestSurroundTriangles.Add(16 * pyramidCount + 4);
                bestSurroundTriangles.Add(16 * pyramidCount + 5);

                bestSurroundTriangles.Add(16 * pyramidCount + 6);
                bestSurroundTriangles.Add(16 * pyramidCount + 7);
                bestSurroundTriangles.Add(16 * pyramidCount + 8);

                bestSurroundTriangles.Add(16 * pyramidCount + 9);
                bestSurroundTriangles.Add(16 * pyramidCount + 10);
                bestSurroundTriangles.Add(16 * pyramidCount + 11);

                bestSurroundTriangles.Add(16 * pyramidCount + 12);
                bestSurroundTriangles.Add(16 * pyramidCount + 14);
                bestSurroundTriangles.Add(16 * pyramidCount + 13);

                bestSurroundTriangles.Add(16 * pyramidCount + 12);
                bestSurroundTriangles.Add(16 * pyramidCount + 15);
                bestSurroundTriangles.Add(16 * pyramidCount + 14);

                pyramidCount += 1;

                switch (p.pyramid)
                {
                    case 0:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 1:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    case 2:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 3:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
                        break;
                    case 4:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));// cw viewed from center
                        break;
                    case 5:
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));

                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
                        bestSurroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));// cw viewed from center
                        break;
                    default:
                        Debug.LogError("ERROR invalid pyramid!");
                        break;
                }
            }
            pyramidSurroundForMesh.Clear();
        }

        void getAllPyramids()
        {
            pyramids.UnionWith(rootTile.pyramidCoords);
            //foreach (tile n in firstTileInLayer[0].next)
            //{
            //    pyramids.AddRange(n.pyramidCoords);
            //}
        }

        List<possibleTilePosition> calculateAllNeighborTilePositions(HashSet<pyramidCoord> pyramidCluster)
        {
            List<possibleTilePosition> newNeighborTilePositions = new List<possibleTilePosition>(); // nrFacesTouch, overlap

            List<int3> neighborCubes = new List<int3>(); // all possible neighbor cube positions 

            foreach (pyramidCoord p in pyramidCluster)
            {
                for (int i = -2; i < 3; i++)
                {
                    for (int j = -2; j < 3; j++)
                    {
                        for (int k = -2; k < 3; k++)
                        {
                            if (neighborCubes.Exists(n => int3equal(n, add(p.pos, new int3(i, j, k)))) == false)
                            {
                                neighborCubes.Add(add(p.pos, new int3(i, j, k)));

                                //int3 newpos = add(p.pos, new int3(i, j, k));
                                //debgLstRed.Add(new Vector3((float)newpos.x, (float)newpos.y, (float)newpos.z));
                            }
                        }
                    }
                }
            }
            //Debug.Log("neighbor cubes count: " + neighborCubes.Count); // 270 

            foreach (int3 neighborCubePos in neighborCubes)
            {
                List<pyramidCoord> testTilePyramidCoords = new List<pyramidCoord>();
                for (int rot = 0; rot < 24; rot++)
                {
                    testTilePyramidCoords = generateSingleTilePyramidCoords(neighborCubePos, rot);

                    int overlap = 0;

                    int nrFacesTouchRootTile = 0;

                    if (calculateOverlap(pyramidCluster, testTilePyramidCoords) == false)
                    {
                        //Debug.Log("no overlap"); // 6076 OK

                        //Debug.Log("testTilePyramidCoords count: " + testTilePyramidCoords.Count); // 19
                        //Debug.Log("pyramidCluster count: " + pyramidCluster.Count); // 19 OK

                        //Debug.Log("pyramidSurround count: " + pyramidSurround.Count); // 192 OK

                        // debug
                        List<pyramidCoord> debgPyramids = new List<pyramidCoord>();

                        // calculate nr pyramids of testTile overlap pyramidSurround
                        foreach (pyramidCoord testPC in testTilePyramidCoords)
                        {
                            if (pyramidSurround.Contains(testPC) == true)
                            {
                                overlap += 1;

                                // debug
                                if (int3equal(neighborCubePos, new int3(testPosX, testPosY, testPosZ)) == true && rot == testRot)
                                {
                                    IEnumerable<pyramidCoord> p = pyramidSurround.Where(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == testPC.pyramid));

                                    foreach (pyramidCoord coord in p)
                                    {
                                        debgPyramids.Add(coord);
                                    }
                                    testOverlap = overlap;
                                }
                            }
                        }

                        if (overlap > 0)
                        {
                            //Debug.Log(" overlap!"); // 1464
                            foreach (pyramidCoord testPC in testTilePyramidCoords)
                            {
                                // calculate nr faces touch rootTile
                                switch (testPC.pyramid)
                                {
                                    case 0:
                                        // 2, 3, 4, 5, x+1: 1
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z), 1)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 1:
                                        // 2, 3, 4, 5, x-1: 0
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z), 0)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 2:
                                        // 0, 1, 4, 5, y+1: 3
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z), 3)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 3:
                                        // 0, 1, 4, 5, y-1: 2
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z), 2)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 4:
                                        // 0, 1, 2, 3, z+1: 5
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1), 5)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 5:
                                        // 0, 1, 2, 3, z-1: 4
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1), 4)) == true)
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                }
                            }

                            List<matchingRule> newMatchingRules = new List<matchingRule>();
                            if (neighborCubePos.x == testPosX && neighborCubePos.y == testPosY && neighborCubePos.z == testPosZ && rot == testRot)
                            {
                                newMatchingRules = generateSingleTileMatchingRules(new posRot(neighborCubePos, rot), 2);
                            }
                            else
                            {
                                newMatchingRules = generateSingleTileMatchingRules(new posRot(neighborCubePos, rot), 0);
                            }
                            //Debug.Log("newMatchingRules count: " + newMatchingRules.Count); // 14 OK
                            // 
                            // // public matchingRule(int3 position, int direction, int rul)
                            // // dir: 0, 1, 2, 3, 4, 5
                            // // rule: +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

                            (bool, bool) fitsAndSquareNeighbor = tileFitsToCluster(pyramidCluster, new posRot(neighborCubePos, rot), clusterMatchingRules);

                            if (fitsAndSquareNeighbor.Item1 == true)
                            {
                                if (fitsAndSquareNeighbor.Item2 == true)
                                {
                                    // square neighbor!
                                    addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), nrFacesTouchRootTile, overlap, true);

                                    //Debug.Log("fits! "); // 4 OK
                                }
                                else
                                {
                                    // triangle neighbor
                                    addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), nrFacesTouchRootTile, overlap, false);

                                    //Debug.Log("fits! "); // 1300 OK;
                                }
                            }
                        }
                    }
                }
            }
            Debug.Log("Nr neighbor cubes: " + neighborCubes.Count);
            Debug.Log("Nr possible tile positions: " + newNeighborTilePositions.Count);

            for (int i = 0; i < 10; i++)
            {
                Debug.Log("possibleTilePosition[" + i + "]: pos: (" + newNeighborTilePositions[i].posrot.pos.x + ", " +
                    newNeighborTilePositions[i].posrot.pos.y + ", " +
                    newNeighborTilePositions[i].posrot.pos.z + "), " + " rot: " + newNeighborTilePositions[i].posrot.rot);
            }

            Debug.Log("first: overlap: " + newNeighborTilePositions[0].overlap + "nrFacesTouch: " + newNeighborTilePositions[0].nrFacesTouch
                 + "squareFaceNeighbor: " + newNeighborTilePositions[0].squareFaceNeighbor);

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

            //public matchingRule(int3 position, int direction, int rul)
            //dir: 0, 1, 2, 3, 4, 5
            // rule: +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

            return newNeighborTilePositions;
        }

        List<possibleTilePosition> addSorted(List<possibleTilePosition> neighborTilePositions, posRot newTile, int nrFacesTouchRootTile, int overlap, bool squareFaceNeighbor)
        {
            // 1. squareFaceNeighbor == true
            //      2. nrFacesTouchRootTile bigger
            //          3. rotation
            //              4. overlap
            //                  5. minimise maxCubeDistance

            List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newTile.pos, newTile.rot);
            int dmax = 0;
            foreach (pyramidCoord coord in newPyramids)
            {
                int pdmax = 0;
                int dx = 0;
                if (coord.pos.x > 0)
                {
                    dx = coord.pos.x;
                }
                else
                {
                    dx = -coord.pos.x;
                }
                int dy = 0;
                if (coord.pos.x > 0)
                {
                    dy = coord.pos.y;
                }
                else
                {
                    dy = -coord.pos.y;
                }
                int dz = 0;
                if (coord.pos.z > 0)
                {
                    dz = coord.pos.z;
                }
                else
                {
                    dz = -coord.pos.z;
                }
                //pdmax = max(dx, max(dy, dz));
                pdmax = dx + dy + dz;
                if (dmax < pdmax)
                {
                    dmax = pdmax;
                }
            }

            if (neighborTilePositions.Count > 0)
            {
                bool added = false;
                for (int i = 0; i < neighborTilePositions.Count; i++)
                {
                    if (squareFaceNeighbor == true)
                    {
                        if (neighborTilePositions[i].squareFaceNeighbor == false) // 1.
                        {
                            // insert(i, X) => insert AT i !!!
                            // [1, 2, 3, 4]
                            // insert(1, X)
                            // [1, X, 2, 3, 4]

                            neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                            added = true;
                            break;
                        }
                        else // 1. // squareFaceNeighbor == true, squareFaceNeighbor[i] == true
                        {
                            if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch) // 2.
                            {
                                neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                added = true;
                                break;
                            }
                            else // nrFacesTouch[i] bigger or equal
                            {
                                if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch) // 2.
                                {
                                    if (newTile.rot < neighborTilePositions[i].posrot.rot) // 3.
                                    {
                                        neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                        added = true;
                                        break;
                                    }
                                    else // rot[i] smaller or equal
                                    {
                                        if (newTile.rot == neighborTilePositions[i].posrot.rot) // 3.
                                        {
                                            if (overlap > neighborTilePositions[i].overlap) // 4.
                                            {
                                                neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                                added = true;
                                                break;
                                            }
                                            else // overlap smaller or equal
                                            {
                                                if (overlap == neighborTilePositions[i].overlap) // 4. 
                                                {
                                                    if (dmax < neighborTilePositions[i].maxCubeDistance) // 5.
                                                    {
                                                        neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                                        added = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else // squareFaceNeighbor == false
                    {
                        if (neighborTilePositions[i].squareFaceNeighbor == false) // 1.
                        {
                            if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch) // 2.
                            {
                                neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                added = true;
                                break;
                            }
                            else // nrFacesTouch[i] bigger or equal
                            {
                                if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch)
                                {
                                    if (newTile.rot < neighborTilePositions[i].posrot.rot) // 3.
                                    {
                                        neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                        added = true;
                                        break;
                                    }
                                    else // rot[i] smaller or equal
                                    {
                                        if (newTile.rot == neighborTilePositions[i].posrot.rot) // 3.
                                        {
                                            if (overlap > neighborTilePositions[i].overlap) // 4.
                                            {
                                                neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                                added = true;
                                                break;
                                            }
                                            else // overlap smaller or equal
                                            {
                                                if (overlap == neighborTilePositions[i].overlap) // 4. 
                                                {
                                                    if (dmax < neighborTilePositions[i].maxCubeDistance) // 5.
                                                    {
                                                        neighborTilePositions.Insert(i, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                                                        added = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (added == false)
                {
                    neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
                }
            }
            else
            {
                neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor, dmax));
            }

            return neighborTilePositions;
        }

        List<possibleTilePosition> recalculatePossibleTilePositions(List<possibleTilePosition> possibleTilePositions, HashSet<pyramidCoord> allPyramids, node newNode)//, posRot placement)
        {
            List<possibleTilePosition> newPossibleTilePositions = new List<possibleTilePosition>();

            // newNode.nodeMatchingRules !!!

            foreach (possibleTilePosition p in possibleTilePositions)
            {
                (bool, bool) fitsAndSquareNeighbor = tileFitsToCluster(allPyramids, p.posrot, newNode.nodeMatchingRules);

                if (fitsAndSquareNeighbor.Item1 == true)
                {
                    if (fitsAndSquareNeighbor.Item2 == true)
                    {
                        // square neighbor!

                        addSorted(newPossibleTilePositions, p.posrot, p.nrFacesTouch, p.overlap, true);

                        //Debug.Log("fits! "); // 4 OK
                    }
                    else
                    {
                        // triangle neighbor
                        addSorted(newPossibleTilePositions, p.posrot, p.nrFacesTouch, p.overlap, false);

                        //Debug.Log("fits! "); // 1300 OK;
                    }

                }
            }

            //Debug.Log("possible tile positions: " + newPossibleTilePositions.Count);

            //Debug.Log("first in list: ");
            //for (int i = 0; i < 12; i++)
            //{
            
            //}

            // 1. squareFaceNeighbor == true
            //      2. nrFacesTouchRootTile bigger
            //          3. rotation
            //              4. overlap
            //                  5. minimise maxCubeDistance

            //  squareFaceNeighbor;
            //  public int maxCubeDistance;

            return newPossibleTilePositions;
        }

        List<pyramidCoord> getAllTriangleFaceNeighborPyramids(pyramidCoord p)
        {
            List<pyramidCoord> faceNeighbors = new List<pyramidCoord>();
            switch (p.pyramid)
            {
                case 0:
                    // 2, 3, 4, 5, x+1: 1
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x + 1, p.pos.y, p.pos.z), 1));
                    break;
                case 1:
                    // 2, 3, 4, 5, x-1: 0
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x - 1, p.pos.y, p.pos.z), 0));
                    break;
                case 2:
                    // 0, 1, 4, 5, y+1: 3
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y + 1, p.pos.z), 3));
                    break;
                case 3:
                    // 0, 1, 4, 5, y-1: 2
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y - 1, p.pos.z), 2));
                    break;
                case 4:
                    // 0, 1, 2, 3, z+1: 5
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z + 1), 5));
                    break;
                case 5:
                    // 0, 1, 2, 3, z-1: 4
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    //faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z - 1), 4));
                    break;
            }
            return faceNeighbors;
        }

        pyramidCoord getSquareFaceNeighborPyramid(pyramidCoord p)
        {
            switch (p.pyramid)
            {
                case 0:
                    // 2, 3, 4, 5, x+1: 1
                    return new pyramidCoord(new int3(p.pos.x + 1, p.pos.y, p.pos.z), 1);
                case 1:
                    // 2, 3, 4, 5, x-1: 0
                    return new pyramidCoord(new int3(p.pos.x - 1, p.pos.y, p.pos.z), 0);
                case 2:
                    // 0, 1, 4, 5, y+1: 3
                    return new pyramidCoord(new int3(p.pos.x, p.pos.y + 1, p.pos.z), 3);
                case 3:
                    // 0, 1, 4, 5, y-1: 2
                    return new pyramidCoord(new int3(p.pos.x, p.pos.y - 1, p.pos.z), 2);
                case 4:
                    // 0, 1, 2, 3, z+1: 5
                    return new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z + 1), 5);
                case 5:
                    // 0, 1, 2, 3, z-1: 4
                    return new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z - 1), 4);
                default:
                    Debug.LogError("ERROR invalid pyramidCoord!");
                    return p;
            }
        }


        //bool detectHoles(List<pyramidCoord> allCurrentPyramidsOfSurround)
        //{
        //    /////////////////////////////////////////////////
        //    // TODO: detect ALL unaccessable pyramids -> all pyramids of pyramidSurround must be able to fill with at least one tile position !!!
        //    //
        //    //  -> TOO SLOW !!!
        //    //
        //    /////////////////////////////////////////////////
        //
        //    bool testTilePossible = true;
        //
        //    foreach (pyramidCoord p in pyramidSurround)
        //    {
        //        if (allCurrentPyramidsOfSurround.Exists(c => int3equal(c.pos, p.pos) == true && c.pyramid == p.pyramid) == false &&
        //
        //            rootTile.pyramidCoords.Exists(f => int3equal(f.pos, p.pos) == true && f.pyramid == p.pyramid) == false)
        //        {
        //            int3 neighborCube = p.pos;
        //
        //            bool pyramidPossible = false;
        //
        //            // tile pos +-1 in x,y,z...
        //            for (int x = -1; x < 2; x++)
        //            {
        //                for (int y = -1; y < 2; y++)
        //                {
        //                    for (int z = -1; z < 2; z++)
        //                    {
        //                        for (int r = 0; r < 24; r++)
        //                        {
        //                            List<pyramidCoord> testTile = generateSingleTilePyramidCoords(add(neighborCube, new int3(x, y, z)), r);
        //
        //                            if (calculateOverlap(allCurrentPyramidsOfSurround, testTile) == false && calculateOverlap(pyramidSurround, testTile) == true)
        //                            {
        //                                // at least one tile must fit
        //                                pyramidPossible = true;
        //                                break;
        //                            }
        //                        }
        //                        if (pyramidPossible == true)
        //                        {
        //                            break;
        //                        }
        //                    }
        //                    if (pyramidPossible == true)
        //                    {
        //                        break;
        //                    }
        //                }
        //                if (pyramidPossible == true)
        //                {
        //                    break;// before break: 156
        //                }
        //            }
        //
        //            if (pyramidPossible == false)
        //            {
        //                debgPyramidsGreen1.Add(p);
        //                testTilePossible = false;
        //            }
        //        }
        //    }
        //
        //
        //
        //    //int debugCase = -1;
        //    bool hole = false;
        //    int freeTriangleNeighbors = 0;
        //    bool squareFaceNeighborFilled = false;
        //    foreach (pyramidCoord p in pyramidSurround)
        //    {
        //        if (allCurrentPyramidsOfSurround.Contains(p) == false && rootTile.pyramidCoords.Contains(p) == false) // p not in current or root -> p not yet filled
        //        {
        //            List<pyramidCoord> triangleNeighborPyramids = getAllTriangleFaceNeighborPyramids(p);
        //            pyramidCoord squareFaceNeighborPyramid = getSquareFaceNeighborPyramid(p);
        //
        //            // all neighbors of p are filled with current surround pyramids or rootTile
        //            // -> at least one NOT filled with current surround pyramids and root tile
        //
        //
        //            if ((rootTile.pyramidCoords.Contains(squareFaceNeighborPyramid) == false) && (allCurrentPyramidsOfSurround.Contains(squareFaceNeighborPyramid) == false))
        //            {
        //                //hole = false;
        //                //Debug.Log("hole set to false"); // do not set hole to false after being set to true !!!
        //                //return false;
        //            }
        //            else
        //            {
        //
        //                foreach (pyramidCoord triangleNeighborOfP in triangleNeighborPyramids)
        //                {
        //                    if ((rootTile.pyramidCoords.Contains(triangleNeighborOfP) == false) && (allCurrentPyramidsOfSurround.Contains(triangleNeighborOfP) == false))
        //                    {
        //                        freeTriangleNeighbors += 1;
        //                    }
        //                }
        //
        //                bool oppositePyramidFilled = false;
        //                switch (p.pyramid)
        //                {
        //                    case 0:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 1)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 1)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 0;
        //                        }
        //                        break;
        //                    case 1:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 0)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 0)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 1;
        //                        }
        //                        break;
        //                    case 2:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 3)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 3)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 2;
        //                        }
        //                        break;
        //                    case 3:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 2)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 2)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 3;
        //                        }
        //                        break;
        //                    case 4:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 5)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 4;//
        //                            //debgPyramidsRed.Add(new pyramidCoord(p.pos, 5));
        //                            //Debug.Log("hole ERROR here in firstTileInLayer contains");
        //                        }
        //                        if (allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 5)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 4;//
        //                            //debgPyramidsRed.Add(new pyramidCoord(p.pos, 5)); // ERROR HERE !!!
        //                            foreach (pyramidCoord t in allCurrentPyramidsOfSurround) // ERROR HERE !!! -> pyramids are not cleared !!!
        //                            {
        //                                //debgPyramidsRed.Add(t);
        //                            }
        //                            //Debug.Log("hole ERROR here in allCurrentPyramidsOfSurround contains");
        //                        }
        //                        break;
        //                    case 5:
        //                        if (rootTile.pyramidCoords.Contains(new pyramidCoord(p.pos, 4)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 4)))
        //                        {
        //                            oppositePyramidFilled = true;
        //                            //debugCase = 5;
        //                        }
        //                        break;
        //                }
        //
        //                if (rootTile.pyramidCoords.Contains(squareFaceNeighborPyramid) || allCurrentPyramidsOfSurround.Contains(squareFaceNeighborPyramid))
        //                {
        //                    squareFaceNeighborFilled = true;
        //                }
        //                // if (freeTriangleNeighbors >= 2)
        //                // {
        //                // 
        //                // }
        //                // if (oppositePyramidFilled == true)
        //                // {
        //                // 
        //                // }
        //
        //                // if (squareFaceNeighborFilled == true && oppositePyramidFilled == false && freeTriangleNeighbors >= 2)
        //                // {
        //                //     hole = false;
        //                // }
        //                // 
        //                // if (squareFaceNeighborFilled == false)
        //                // {
        //                //     hole = false;
        //                // }
        //
        //                // if (squareFaceNeighborFilled == true && oppositePyramidFilled == true)
        //                // {
        //                //     hole = true;
        //                // }
        //
        //                //if (squareFaceNeighborFilled == true && oppositePyramidFilled == true)
        //                //{
        //                //    //Debug.Log("HOLE = true ???!!! debugCase: " + debugCase);
        //                //}
        //
        //                if (testTilePossible == false)
        //                {
        //                    hole = true;
        //                }
        //
        //                if (squareFaceNeighborFilled == true && (oppositePyramidFilled == true || freeTriangleNeighbors < 3))
        //                {
        //                    hole = true;
        //                    //Debug.Log("hole set to true!");
        //
        //                    debgPyramidsBlue1.Add(p);
        //
        //                    //if (freeTriangleNeighbors < 3)
        //                    //{
        //                    //    Debug.Log("freeTriangleNeighbors: " + freeTriangleNeighbors);
        //                    //}
        //                }
        //            }
        //
        //            //              Y
        //            //              ^
        //            //              |
        //            //              +------> Z
        //            //             /
        //            //            X  
        //
        //            //                  2 1
        //            //                  |/
        //            //              5 --+-- 4
        //            //                 /|
        //            //                0 3  
        //
        //        }
        //    }
        //    return hole;
        //}

        //copy List / clone List: new List<posRot>(oldList);

        void setupNextLayer()
        {
            HashSet<pyramidCoord> nextClusterPyramids = surroundNodes[activeNodeIndex].currentPyramids;
            nextClusterPyramids.UnionWith(rootTile.pyramidCoords);

            neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = new List<possibleTilePosition>();

            pyramidSurround = new HashSet<pyramidCoord>();
            pyramidSurroundForMesh = new List<pyramidCoord>();

            bestSurroundVertices = new List<Vector3>();
            bestSurroundTriangles = new List<int>();

            surroundNodes = new node[pyramidSurround.Count];
            activeNodeIndex = -1;

            maxPyramidsFilled = 0;

            // new cluster mesh
            pyramids.Clear();
            pyramids.UnionWith(nextClusterPyramids);
            Debug.Log("nextClusterPyramids count: " + nextClusterPyramids.Count);
            generateMesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

            // // root tile
            // pyramids.Clear();
            // firstTileInLayer.Clear();
            // firstTileInLayer.Add(new tile(new int3(0, 0, 0), 0, this));

            // surround
            calculateAllNeighborPyramids(nextClusterPyramids); // calculates pyramidSurround
            surroundNodes = new node[pyramidSurround.Count];
            Debug.Log("setupNextLayer: pyramidSurround.Count: " + pyramidSurround.Count);

            activeNodeIndex = -1;

            clusterMatchingRules = generateClusterMatchingRules(nextClusterPyramids);
            Debug.Log("setupNextLayer: clusterMatchingRules.Count: " + clusterMatchingRules.Count);

            neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = calculateAllNeighborTilePositions(nextClusterPyramids);
            Debug.Log("setupNextLayer: neighborTilePositionsWithNFacesTouchingRootTileAndOverlap.Count: " + neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap.Count);
            // 3928
        }

        void splitPossibleTilePositions(int activeNodeIndex)
        {
            int NrPreviousSplits = surroundNodes[activeNodeIndex].possibleTilePositions.Count;
            // surroundNodes[activeNodeIndex].possibleTilePositions[]
        }

        void combinePossibleTilePositions()
        {

        }



        void tilingStep()
        {
            node newNode;

            if (activeNodeIndex != -1)
            {
                // active node has possibleTilePositions not yet explored
                if (surroundNodes[activeNodeIndex].possibleTilePositions[0].Count > 0 && surroundNodes[activeNodeIndex].possibleTilePositions[0].Count > surroundNodes[activeNodeIndex].currentNextIndex)
                {
                    //Debug.Log("next step:");
                    //if (surroundNodes[activeNodeIndex].possibleTilePositions.Count > 0)
                    //{
                    //    Debug.Log("new node tile slot: " + 0 + ": pos: (" + surroundNodes[activeNodeIndex].possibleTilePositions[0].posrot.pos.x + ", " +
                    //        surroundNodes[activeNodeIndex].possibleTilePositions[0].posrot.pos.y + ", " + surroundNodes[activeNodeIndex].possibleTilePositions[0].posrot.pos.z + "), " +
                    //        "rot: " + surroundNodes[activeNodeIndex].possibleTilePositions[0].posrot.rot + ", squareFaceNeighbor: " + surroundNodes[activeNodeIndex].possibleTilePositions[0].squareFaceNeighbor +
                    //        ", nrFacesTouchRootTile: " + surroundNodes[activeNodeIndex].possibleTilePositions[0].nrFacesTouch + ", overlap: " + surroundNodes[activeNodeIndex].possibleTilePositions[0].overlap +
                    //        ", maxCubeDistance: " + surroundNodes[activeNodeIndex].possibleTilePositions[0].maxCubeDistance);
                    //}
                    newNode = new node(surroundNodes[activeNodeIndex].possibleTilePositions[0][surroundNodes[activeNodeIndex].currentNextIndex].posrot);

                    // skip surroundNodes that are worse
                    if (onlyBestOptions)
                    {
                        while(true)
                        {
                            if (surroundNodes[activeNodeIndex].currentNextIndex + 1 < surroundNodes[activeNodeIndex].possibleTilePositions[0].Count - 1 && surroundNodes[activeNodeIndex].currentNextIndex + 1 > 0)
                            {
                                int i = surroundNodes[activeNodeIndex].currentNextIndex + 1;
                                //Debug.Log("index: " + i + ", count: " + surroundNodes[activeNodeIndex].possibleTilePositions.Count);
                                if (surroundNodes[activeNodeIndex].possibleTilePositions[0][surroundNodes[activeNodeIndex].currentNextIndex].squareFaceNeighbor == 
                                    surroundNodes[activeNodeIndex].possibleTilePositions[0][surroundNodes[activeNodeIndex].currentNextIndex + 1].squareFaceNeighbor)
                                {
                                    if (surroundNodes[activeNodeIndex].possibleTilePositions[0][surroundNodes[activeNodeIndex].currentNextIndex].nrFacesTouch == surroundNodes[activeNodeIndex].possibleTilePositions[0][surroundNodes[activeNodeIndex].currentNextIndex + 1].nrFacesTouch)
                                    {
                                        surroundNodes[activeNodeIndex].currentNextIndex = surroundNodes[activeNodeIndex].currentNextIndex + 1;
                                        break;
                                    }
                                    else
                                    {
                                        surroundNodes[activeNodeIndex].currentNextIndex = surroundNodes[activeNodeIndex].possibleTilePositions[0].Count;
                                        break;
                                    }
                                }
                                else
                                {
                                    surroundNodes[activeNodeIndex].currentNextIndex = surroundNodes[activeNodeIndex].possibleTilePositions[0].Count;
                                    break;
                                }
                            }
                            else
                            {
                                surroundNodes[activeNodeIndex].currentNextIndex = surroundNodes[activeNodeIndex].currentNextIndex + 1;
                            }
                        }
                    }
                    else
                    {
                        surroundNodes[activeNodeIndex].currentNextIndex = surroundNodes[activeNodeIndex].currentNextIndex + 1; // -> overflow gets checked next iteration!
                    }
                    // sorting order:
                    // 1. squareFaceNeighbor == true
                    //      2. nrFacesTouchRootTile bigger
                    //          3. rotation
                    //              4. overlap
                    //                  5. minimise maxCubeDistance

                    List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);

                    int nrNewPyramidsFilled = calculateNrPyramidsOverlap(pyramidSurround, newPyramids);

                    HashSet<pyramidCoord> allPyramids = new HashSet<pyramidCoord>(surroundNodes[activeNodeIndex].currentPyramids);

                    allPyramids.UnionWith(newPyramids);

                    newNode.nodeMatchingRules = surroundNodes[activeNodeIndex].nodeMatchingRules;
                    newNode.nodeMatchingRules.AddRange(generateSingleTileMatchingRules(newNode.placement, 1));

                    newNode.pyramidsFilled = surroundNodes[activeNodeIndex].pyramidsFilled + nrNewPyramidsFilled;

                    newNode.currentPyramids = allPyramids;

                    // remove all indices < currentNextIndex for newNode
                    List<possibleTilePosition> newPossibleTilePositions = new List<possibleTilePosition>(surroundNodes[activeNodeIndex].possibleTilePositions[0]);
                    if (surroundNodes[activeNodeIndex].currentNextIndex > 1)
                    {
                        for (int i = 0; i < surroundNodes[activeNodeIndex].currentNextIndex - 1; i++)
                        {
                            newPossibleTilePositions.RemoveAt(0);
                        }
                    }
                    newNode.possibleTilePositions.Add(new List<possibleTilePosition>());
                    newNode.possibleTilePositions[0] = recalculatePossibleTilePositions(newPossibleTilePositions, allPyramids, newNode);//, newNode.placement); // TEST
                       
                    //Debug.Log("after step: newNode: possibleTilePositions: " + newNode.possibleTilePositions.Count);

                    newNode.currentNextIndex = 0;

                    surroundNodes[activeNodeIndex + 1] = newNode;

                    activeNodeIndex = activeNodeIndex + 1;

                    splitPossibleTilePositions(activeNodeIndex);

                    //Debug.Log("after step: new active node: possibleTilePositions: " + surroundNodes[activeNodeIndex].possibleTilePositions.Count);

                    if (drawMode == draw.best)
                    {
                        if (newNode.pyramidsFilled >= maxPyramidsFilled)
                        {
                            if (newNode.pyramidsFilled == maxPyramidsFilled)
                            {
                                if (newNode.currentPyramids.Count <= bestSurroundVertices.Count / 16)
                                {
                                    generateBestSurroundMesh(true);

                                    if (newNode.pyramidsFilled == pyramidSurround.Count)
                                    {
                                        Debug.Log("tiling found! new best surround!");
                                    }
                                }
                            }
                            else // new > max
                            {
                                generateBestSurroundMesh(true);
                            }
                        }
                        else // new < max
                        {
                            generateBestSurroundMesh(false);
                        }
                        // validate triangles
                        int maxTriIndex = 0;
                        foreach (int t in bestSurroundTriangles)
                        {
                            if (maxTriIndex < t)
                            {
                                maxTriIndex = t;
                            }
                        }

                        if (bestSurroundVertices.Count > maxTriIndex)
                        {
                            surroundMesh.Clear();

                            surroundMesh.SetVertices(bestSurroundVertices);
                            surroundMesh.SetTriangles(bestSurroundTriangles, 0);
                            surroundMesh.RecalculateNormals();
                            surroundMeshFilter.mesh = surroundMesh;
                        }
                    }
                    else
                    {
                        generateSurroundMesh();
                        // validate triangles
                        int maxTriIndex = 0;
                        foreach (int t in surroundTriangles)
                        {
                            if (maxTriIndex < t)
                            {
                                maxTriIndex = t;
                            }
                        }

                        if (surroundVertices.Count > maxTriIndex)
                        {
                            surroundMesh.SetVertices(surroundVertices);
                            surroundMesh.SetTriangles(surroundTriangles, 0);
                            surroundMesh.RecalculateNormals();
                            surroundMeshFilter.mesh = surroundMesh;
                        }
                    }


                    if (maxPyramidsFilled <= newNode.pyramidsFilled)
                    {
                        Debug.Log("pyramids filled: " + newNode.pyramidsFilled + " / " + pyramidSurround.Count); // 158 (/ 192)  // 160 x24  // 165  // 171 // 176 x2 (/180)
                        maxPyramidsFilled = newNode.pyramidsFilled; // second surround layer: 710 (/781) (1min)
                    }

                    if (maxPyramidsFilled == pyramidSurround.Count && onlyOnce == false)
                    {
                        onlyOnce = true;
                        Debug.Log("tiling found !!!");
                        setupNextLayer();
                    }
                }
                else
                {
                    //Debug.Log("backtrack!");
                    //Debug.Log("activeNode before backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), rot: " + activeNode.placement.rot);
                    //Debug.Log("activeNode before backtrack currentPyramids count: " + activeNode.currentPyramids.Count);

                    //Debug.Log("active node possible tile positions count: " + activeNode.possibleTilePositions.Count);

                    if (activeNodeIndex > 0)
                    {
                        activeNodeIndex = activeNodeIndex - 1;
                        //Debug.Log("backtrack!");
                        //surroundNodes[activeNodeIndex].next = null;

                        combinePossibleTilePositions();
                    }
                    else
                    {
                        Debug.Log("ERROR! active node has no parent !!!");
                    }
                }
            }
            else // first tile
            {
                Debug.Log("first tile!");
                Debug.Log("pyramids count: " + pyramids.Count);

                Debug.Log("neighborTilePositions count: " + neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap.Count); // count == 4 ???

                newNode = new node(neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap[0].posrot);

                Debug.Log("new node: first node, pos: (" + newNode.placement.pos.x + ", " + newNode.placement.pos.y + ", " + newNode.placement.pos.z + "), rot: " + newNode.placement.rot);

                //Debug.Log("neighborTilePositions[1]: (" + neighborTilePositionsWithNrFacesTouchingRootTile[1].Item1.pos.x + ", " + neighborTilePositionsWithNrFacesTouchingRootTile[1].Item1.pos.y + ", " + neighborTilePositionsWithNrFacesTouchingRootTile[1].Item1.pos.z + "), Rot: " + neighborTilePositionsWithNrFacesTouchingRootTile[1].Item1.rot);

                List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);
                HashSet<pyramidCoord> newPyramidsSet = new HashSet<pyramidCoord>(newPyramids);
                newNode.currentPyramids = newPyramidsSet;

                newNode.pyramidsFilled = calculateNrPyramidsOverlap(pyramidSurround, newPyramids);

                List<pyramidCoord> rootAndFirstTilePyramids = new List<pyramidCoord>();

                rootAndFirstTilePyramids.AddRange(pyramids);
                rootAndFirstTilePyramids.AddRange(newPyramids);

                newNode.nodeMatchingRules = clusterMatchingRules;
                newNode.nodeMatchingRules.AddRange(generateSingleTileMatchingRules(newNode.placement, 1));

                newNode.possibleTilePositions.Add(new List<possibleTilePosition>());

                newNode.possibleTilePositions[0] = neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap;

                Debug.Log("newNode possibleTilePositions before recalculate: " + newNode.possibleTilePositions[0].Count);

                newNode.possibleTilePositions[0] = recalculatePossibleTilePositions(newNode.possibleTilePositions[0], newPyramidsSet, newNode);

                newNode.currentNextIndex = 0;

                Debug.Log("new node currentNextIndex: in first tile: " + newNode.currentNextIndex);

                Debug.Log("newNode possibleTilePositions recalculated: " + newNode.possibleTilePositions.Count);

                surroundNodes[0] = newNode;

                activeNodeIndex = 0;
                Debug.Log("activeNode = newNode in tilingStep() -> first tile");

            }
        }

        bool calculateOverlap(HashSet<pyramidCoord> cluster, List<pyramidCoord> tile)
        {
            foreach (pyramidCoord t in tile)
            {
                //foreach (pyramidCoord c in cluster)
                //{
                //    if (t.pos.x == c.pos.x && t.pos.y == c.pos.y && t.pos.z == c.pos.z)
                //    {
                //        if (t.pyramid == c.pyramid)
                //        {
                //            b = true;
                //            return b;
                //        }
                //    }
                //}
                if (cluster.Contains(t))
                {
                    return true;
                }
            }
            return false;
        }

        int calculateNrPyramidsOverlap(HashSet<pyramidCoord> cluster, List<pyramidCoord> tile)
        {
            int n = 0;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains(t) == true)
                {
                    n += 1;
                }
                //else
                //{
                //   //Debug.Log("overlap!");
                //}
            }
            return n;
        }

        int calculateNumberPyramidsOverlap(List<pyramidCoord> cluster, List<pyramidCoord> tile)
        {
            int n = 0;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains(t) == true)
                {
                    n += 1;
                }
            }
            return n;
        }

        void calculateAllNeighborPyramids(HashSet<pyramidCoord> pyramidCluster)
        {
            pyramidSurround.Clear();
            foreach (pyramidCoord p in pyramidCluster)
            {
                List<pyramidCoord> neighborPyramidsCase0 = new List<pyramidCoord>();

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 0)), 0)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 0)), 3)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 0)), 4)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 0)), 5)); // ^

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 1)), 0)); // /^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 1)), 3)); // /^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, 1)), 5)); // /^

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 1)), 0)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 1)), 2)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 1)), 3)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 1)), 5)); // ->

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 1)), 0)); // \_>
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 1)), 2)); // \_>
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 1)), 5)); // \_>

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 0)), 0)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 0)), 2)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 0)), 4)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, 0)), 5)); // \/

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, -1)), 0)); // <_/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, -1)), 2)); // <_/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, -1, -1)), 4)); // <_/

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, -1)), 0)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, -1)), 2)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, -1)), 3)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, -1)), 4)); // <-

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, -1)), 0)); // ^\
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, -1)), 3)); // ^\
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 1, -1)), 4)); // ^\

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 1)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 2)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 3)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 4)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 5)); // . 
                //------------------------------------------------------------------------------------
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 0)), 1)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 0)), 3)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 0)), 4)); // ^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 0)), 5)); // ^

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 1)), 1)); // /^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 1)), 3)); // /^
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, 1)), 5)); // /^

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 1)), 1)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 1)), 2)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 1)), 3)); // ->
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 1)), 5)); // ->

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 1)), 1)); // \_>
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 1)), 2)); // \_>
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 1)), 5)); // \_>

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 0)), 1)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 0)), 2)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 0)), 4)); // \/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, 0)), 5)); // \/

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, -1)), 1)); // <_/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, -1)), 2)); // <_/
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, -1, -1)), 4)); // <_/

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, -1)), 1)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, -1)), 2)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, -1)), 3)); // <-
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, -1)), 4)); // <-

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, -1)), 1)); // ^\
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, -1)), 3)); // ^\
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 1, -1)), 4)); // ^\

                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 1)); // -.
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 2)); // -.
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 3)); // -.
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 4)); // -.
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 5)); // -. 

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
                
                switch (p.pyramid)
                {
                    case 0:
                        foreach (pyramidCoord pc in neighborPyramidsCase0)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;
                    case 1: // use rotate...!
                        List<pyramidCoord> neighborPyramidsCase1 = rotatePyramids(rotatePyramids(neighborPyramidsCase0, 1, true, p.pos), 1, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase1)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;
                    case 2:
                        List<pyramidCoord> neighborPyramidsCase2 = rotatePyramids(neighborPyramidsCase0, 2, false, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase2)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;
                    case 3:
                        List<pyramidCoord> neighborPyramidsCase3 = rotatePyramids(neighborPyramidsCase0, 2, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase3)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;
                    case 4:
                        List<pyramidCoord> neighborPyramidsCase4 = rotatePyramids(neighborPyramidsCase0, 1, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase4)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;
                    case 5:
                        List<pyramidCoord> neighborPyramidsCase5 = rotatePyramids(neighborPyramidsCase0, 1, false, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase5)
                        {
                            if ((pyramidSurround.Contains(pc) == false) && (pyramidCluster.Contains(pc) == false))
                            {
                                pyramidSurround.Add(pc);
                            }
                        }
                        break;

                }
            }

            foreach (pyramidCoord p in pyramidCluster)
            {
                // moved down here to avoid doubles
                for (int i = 1; i < 6; i++)
                {
                    if (pyramidSurround.Contains(new pyramidCoord(p.pos, (p.pyramid + i) % 6)) == false  && //pyr => (int3equal(pyr.pos, p.pos) == true && pyr.pyramid == (p.pyramid + i) % 6)) == false &&
                        pyramidCluster.Contains(p) == false) //Exists(pyr => (int3equal(pyr.pos, p.pos) == true && pyr.pyramid == p.pyramid)) == false)
                    {
                        pyramidSurround.Add((new pyramidCoord(p.pos, (p.pyramid + i) % 6)));
                    }
                }
            }
        }

    }

}