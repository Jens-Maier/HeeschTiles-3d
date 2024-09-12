using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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

    public class node
    {
        public posRot placement;

        public List<pyramidCoord> currentPyramids;

        public node next;

        public node parent;

        public List<possibleTilePosition> possibleTilePositions; // int overlap

        public List<matchingRule> nodeMatchingRules;

        public int currentNextIndex;

        public int pyramidsFilled;


        public node(posRot pr)
        {
            placement = pr;
            currentPyramids = new List<pyramidCoord>();
            possibleTilePositions = new List<possibleTilePosition>();
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
        public List<tile> firstTileInLayer;
        public List<pyramidCoord> pyramids;
        public List<pyramidCoord>[] singleTilePyramidCoords; //pyramids of all orientations of a single tile in pos (0,0,0)

        public List<pyramidCoord> pyramidSurround;

        public List<pyramidCoord> pyramidSurroundForMesh;

        public List<possibleTilePosition> neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap;

        public List<Vector3> vertices;
        public List<int> triangles;

        public Mesh mesh;
        public MeshFilter meshFilter;

        public Mesh surroundMesh;
        public MeshFilter surroundMeshFilter;

        public GameObject surround;

        public List<Vector3> surroundVertices;
        public List<int> surroundTriangles;

        public List<Vector3> bestSurroundVertices;
        public List<int> bestSurroundTriangles;

        public Material material;

        //        //public int drawThisTileFromSurround;

        public node surroundRootNode;

        List<matchingRule> clusterMatchingRules;

        public node activeNode;

        //        public int step;

        //        public int numPyramidsToFill;
        public int maxPyramidsFilled; // 157 -> 160

        public List<Vector3> debgLstRed;
        public List<Vector3> debgLstGreen;
        public List<Vector3> debgLstBlue;

        public List<pyramidCoord> debgPyramidsGreen1;
        public List<pyramidCoord> debgPyramidsRed1;
        public List<pyramidCoord> debgPyramidsBlue1;

        public List<pyramidCoord> debgPyramidsGreen2;
        public List<pyramidCoord> debgPyramidsRed2;
        public List<pyramidCoord> debgPyramidsBlue2;

        //        int drawTilePyramidCount;

        //        List<posRot> finalTiles;
        //        List<pyramidCoord> finalPyramids;
        //        [HideInInspector]
        //        public bool redrawMesh;

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

        public bool tileFits;

        // Start is called before the first frame update
        void Start()
        {
            surround = new GameObject();
            surround.AddComponent<MeshFilter>();
            surround.AddComponent<MeshRenderer>();

            surround.GetComponent<MeshFilter>().mesh = new Mesh();
            surround.GetComponent<MeshRenderer>().material = material;

            firstTileInLayer = new List<tile>();
            pyramids = new List<pyramidCoord>();
            neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = new List<possibleTilePosition>();

            //            redrawMesh = false;
            //            onlyOnce = false;
            //            drawTilePyramidCount = 0;
            //            pyramidsFilled = 0;
            maxPyramidsFilled = 0;

            //            step = 0;
            onlyOnce = false;

            pyramidSurround = new List<pyramidCoord>();
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


            //            finalTiles = new List<posRot>();
            //            finalPyramids = new List<pyramidCoord>();

            vertices = new List<Vector3>();
            triangles = new List<int>();
            surroundVertices = new List<Vector3>();
            surroundTriangles = new List<int>();

            bestSurroundVertices = new List<Vector3>();
            bestSurroundTriangles = new List<int>();

            tileFits = false;


            singleTilePyramidCoords = new List<pyramidCoord>[24 * 3]; // * 3]; // TODO: *3 for large tile!
            for (int i = 0; i < 24 * 3; i++)// * 3; i++)
            {
                singleTilePyramidCoords[i] = new List<pyramidCoord>();

                singleTilePyramidCoords[i] = generateSingleTilePyramidCoords(new int3(0, 0, 0), i);
            }

            meshFilter = GetComponent<MeshFilter>();
            surroundMeshFilter = surround.GetComponent<MeshFilter>();

            mesh = new Mesh();

            surroundMesh = new Mesh();


            // setup ----------

            // root tile
            pyramids.Clear();
            firstTileInLayer.Clear();
            firstTileInLayer.Add(new tile(new int3(0, 0, 0), 0, this));

            getAllPyramids();
            generateMesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

            // surround
            calculateAllNeighborPyramids(pyramids);



            //Debug.Log("pyramids count : " + pyramids.Count); // 19 OK
            clusterMatchingRules = generateClusterMatchingRules(pyramids);

            neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap = calculateAllNeighborTilePositions(pyramids);

            //Debug.Log("setup: neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap count: " + neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap.Count);

            //rootTileMatchingRules = generateSingleTileMatchingRules(new posRot(firstTileInLayer[0].pos, firstTileInLayer[0].rot));


        }

        // Update is called once per frame
        void Update()
        {
            // add single tile OK
            //Debug.Log(" total neighbor tile positions count: " + neighborTilePositionsWithNrFacesTouchingRootTile.Count);

            // test surround
            //
            //surroundRootNode = new node(neighborTilePositionsWithNrFacesTouchingRootTile[test].Item1);
            //surroundRootNode.currentPyramids = generateSingleTilePyramidCoords(surroundRootNode.placement.pos, surroundRootNode.placement.rot);
            //activeNode = surroundRootNode;

            tilingStep();

            debgLstRed.Clear();
            debgLstGreen.Clear();
            debgLstBlue.Clear();




            debgPyramidsRed2.Clear();
            debgPyramidsGreen2.Clear();
            debgPyramidsBlue2.Clear();


            // for test:
            // neighborTilePositionsWithNrFacesTouchingRootTile = calculateAllNeighborTilePositions(pyramids);
            //

            // FUNKT !!!
            //
            // posRot testPos = new posRot(new int3(testPosX, testPosY, testPosZ), testRot);
            // bool fits = tileFitsToCluster(pyramids, testPos);
            // Debug.Log("tile fits: " + fits);
            // 
            // tileFits = fits;

            // draw test tile  ----------------------
            //
            // tileFits = false;
            // foreach (possibleTilePosition n in neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap)
            // {
            //     if (int3equal(n.posrot.pos, new int3(testPosX, testPosY, testPosZ)) == true && n.posrot.rot == testRot)
            //     {
            //         tileFits = true;
            //         testNrFacesTouchRootTile = n.nrFacesTouch;
            //         testOverlap = n.overlap;
            // 
            //         node newNode = new node(new posRot(n.posrot.pos, n.posrot.rot));
            //         List<pyramidCoord> testPyramids = generateSingleTilePyramidCoords(n.posrot.pos, n.posrot.rot);
            // 
            //         newNode.currentPyramids = testPyramids;
            //         node temp = activeNode;
            //         activeNode.next = newNode;
            //         activeNode = newNode;
            // 
            //         generateSurroundMesh();
            // 
            // 
            //         activeNode = temp;
            //     }
            // }


        }

        public (bool, bool) tileFitsToCluster(List<pyramidCoord> cluster, posRot newTilePos, List<matchingRule> clustMatchingRules)
        {
            //Debug.Log("tileFits() pos: (" + newTilePos.pos.x + ", " + newTilePos.pos.y + ", " + newTilePos.pos.z + "); Rot: " + newTilePos.rot);

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
                if (cluster.Exists(p => int3equal(p.pos, tilePyramid.pos) == true && p.pyramid == tilePyramid.pyramid) == true)
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
            foreach (matchingRule rule in tileRules)
            {
                matchingRule tileOppositeRule = getOppositeRule(rule);

                foreach (matchingRule clustRule in clustMatchingRules)
                {
                    if (int3equal(clustRule.pos, tileOppositeRule.pos) == true && clustRule.dir == tileOppositeRule.dir && clustRule.rule != tileOppositeRule.rule)
                    {
                        fits = false;
                    }

                    if (int3equal(clustRule.pos, tileOppositeRule.pos) == true && clustRule.dir == tileOppositeRule.dir && clustRule.rule == 0 && tileOppositeRule.rule == 0)
                    {
                        squareRule = true;
                    }
                }
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
            //Debug.Log("onDrawGizmos!");


            Gizmos.color = Color.blue;
            foreach (Vector3 p in debgLstBlue)
            {
                Gizmos.DrawSphere(p, 0.3f);
            }

            Gizmos.color = Color.green;

            if (debgPyramidsGreen1 != null)
            {
                //Debug.Log("debgPyramids count Green" + debgPyramidsGreen.Count);

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
                //Debug.Log("debgPyramids count Green" + debgPyramidsGreen.Count);

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

            foreach (Vector3 p in debgLstRed)
            {
                Gizmos.DrawSphere(p, 0.03f);
            }

            if (debgPyramidsRed1 != null)
            {
                //Debug.Log("debgPyramids count Red" + debgPyramidsRed.Count);

                foreach (pyramidCoord p in debgPyramidsRed1)
                {
                    //Debug.Log("debgPyramidRed! " + p.pos.x + ", " + p.pos.y + ", " + p.pos.z + ", pyramid: " + p.pyramid);
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
                //Debug.Log("debgPyramids count Red" + debgPyramidsRed.Count);

                foreach (pyramidCoord p in debgPyramidsRed2)
                {
                    //Debug.Log("debgPyramidRed! " + p.pos.x + ", " + p.pos.y + ", " + p.pos.z + ", pyramid: " + p.pyramid);
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

            foreach (Vector3 p in debgLstGreen)
            {
                Gizmos.DrawSphere(p, 0.05f);
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
                            //newCoords.Add(new pyramidCoord(new int3(p.pos.x, p.pos.z, -p.pos.y), newPyr)); // (( where new x comes from, y.., z.. ))
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
                            //newCoords.Add(new pyramidCoord(new int3(p.pos.x, -p.pos.z, p.pos.y), newPyr)); // (( where new x comes from, y.., z.. ))
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
                            newCoords.Add(new pyramidCoord(new int3(center.x - (p.pos.z - center.z), p.pos.y, center.z + (p.pos.x - center.x)), newPyr)); // (( where new x comes from, y.., z.. ))
                            //newCoords.Add(new pyramidCoord(new int3(-p.pos.z, p.pos.y, p.pos.x), newPyr)); // (( where new x comes from, y.., z.. ))
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
                            newCoords.Add(new pyramidCoord(new int3(center.x + (p.pos.z - center.z), p.pos.y, center.z - (p.pos.x - center.x)), newPyr));// (( where new x comes from, y.., z.. ))
                            //newCoords.Add(new pyramidCoord(new int3(p.pos.z, p.pos.y, -p.pos.x), newPyr));// (( where new x comes from, y.., z.. ))
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

                            newCoords.Add(new pyramidCoord(new int3(center.x + (p.pos.y - center.y), center.y - (p.pos.x - center.x), p.pos.z), newPyr)); // (( where new x comes from, y.., z.. ))
                            //newCoords.Add(new pyramidCoord(new int3(p.pos.y, -p.pos.x, p.pos.z), newPyr)); // (( where new x comes from, y.., z.. ))

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
                            newCoords.Add(new pyramidCoord(new int3(center.x - (p.pos.y - center.y), center.y + (p.pos.x - center.x), p.pos.z), newPyr)); // (( where new x comes from, y.., z.. ))
                            //newCoords.Add(new pyramidCoord(new int3(-p.pos.y, p.pos.x, p.pos.z), newPyr)); // (( where new x comes from, y.., z.. ))
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

        public List<matchingRule> generateClusterMatchingRules(List<pyramidCoord> pyramids)
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
            // -> match square face neighbors first!...

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
            return generateMatchingRules(cubePosCount, tilePyramids, display); // ???  matching rules count =  13 / 14  ???
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

        public List<matchingRule> generateMatchingRules(List<(int3, int)> cubePosCount, List<pyramidCoord> tilePyramids, int display)
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
                        if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == i) == true)
                        {
                            // ( + or | )
                            switch (i)
                            {
                                case 0:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 1)) == false) //opposite pyramid not in tilePyramids
                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 1) == false) // ERROR HERE... -> fixed???
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == true) //<- same ERROR HERE...
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 0)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 0) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 1)) == false && 
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(1, 0, 0)), 0)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 0) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 1:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 0)) == false) //opposite pyramid not in tilePyramids

                                    // case0:
                                    //if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(1, 0, 0))) && p.pyramid == 1) == false)

                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 0) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 0) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 1)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 0) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 1) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 0)) == false &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(-1, 0, 0)), 1)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 0) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(-1, 0, 0))) && p.pyramid == 1) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 2:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 3)) == false) //opposite pyramid not in tilePyramids
                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 3) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 1, 0))) && p.pyramid == 3) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 2)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 1, 0))) && p.pyramid == 3) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 1, 0))) && p.pyramid == 2) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 3)) == false &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 1, 0)), 2)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 1, 0))) && p.pyramid == 3) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 1, 0))) && p.pyramid == 2) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 3:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 2)) == false) //opposite pyramid not in tilePyramids
                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 2) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, -1, 0))) && p.pyramid == 2) == true)
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
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 3)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, -1, 0))) && p.pyramid == 2) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, -1, 0))) && p.pyramid == 3) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 2)) == false &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, -1, 0)), 3)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, -1, 0))) && p.pyramid == 2) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, -1, 0))) && p.pyramid == 3) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 4:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 5)) == false) //opposite pyramid not in tilePyramids
                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 5) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, 1))) && p.pyramid == 5) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 4)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, 1))) && p.pyramid == 5) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, 1))) && p.pyramid == 4) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 5)) == false &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, 1)), 4)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, 1))) && p.pyramid == 5) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, 1))) && p.pyramid == 4) == false)
                                        {
                                            // ( | )
                                            rules.Add(new matchingRule(posCount.Item1, i, 0));
                                            //Debug.Log("dir: " + i + " matching rule |");
                                        }
                                    }
                                    break;
                                case 5:
                                    //if (tilePyramids.Exists(new pyramidCoord(posCount.Item1, 4)) == false) //opposite pyramid not in tilePyramids
                                    if (tilePyramids.Exists(p => int3equal(p.pos, posCount.Item1) && p.pyramid == 4) == false)
                                    {
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == true)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, -1))) && p.pyramid == 4) == true)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                    }
                                    else
                                    {
                                        // opposite pyramid in tilePyramids
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == true &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 5)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, -1))) && p.pyramid == 4) == true &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, -1))) && p.pyramid == 5) == false)
                                        {
                                            // ( + )
                                            rules.Add(new matchingRule(posCount.Item1, i, 1));
                                            //Debug.Log("dir: " + i + " matching rule +");
                                        }
                                        //if (tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 4)) == false &&
                                        //    tilePyramids.Exists(new pyramidCoord(add(posCount.Item1, new int3(0, 0, -1)), 5)) == false)
                                        if (tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, -1))) && p.pyramid == 4) == false &&
                                            tilePyramids.Exists(p => int3equal(p.pos, add(posCount.Item1, new int3(0, 0, -1))) && p.pyramid == 5) == false)
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
            List<pyramidCoord> returnPyramidCoords = new List<pyramidCoord>();
            switch (r)
            {
                case 0:
                    returnPyramidCoords.AddRange(tempPyramidCoords); //+y up 0
                    break;
                case 1:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 1, true, center)); //+y up 1
                    break;
                case 2:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 1, true, center), 1, true, center)); //+y up 2
                    break;
                case 3:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 1, false, center)); //+y up 3
                    break;

                case 4:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 2, false, center)); //+x up 0
                    break;
                case 5:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center)); //+x up 1
                    break;
                case 6:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center), 1, true, center)); //+x up 2
                    break;
                case 7:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, false, center)); //+x up 3
                    break;

                case 8:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center)); //-y up 0
                    break;
                case 9:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center)); //-y up 1
                    break;
                case 10:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center), 1, true, center)); //-y up 2
                    break;
                case 11:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, false, center)); //-y up 3
                    break;

                case 12:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 2, true, center)); //-x up 0
                    break;
                case 13:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center)); //-x up 1
                    break;
                case 14:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center), 1, true, center)); //-x up 2
                    break;
                case 15:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, false, center)); //-x up 3
                    break;

                case 16:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 0, true, center)); // +z up 0
                    break;
                case 17:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center)); // +z up 1
                    break;
                case 18:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center), 1, true, center)); // +z up 2
                    break;
                case 19:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, false, center)); // +z up 3
                    break;

                case 20:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 0, false, center)); //-z up 0
                    break;
                case 21:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center)); //-z up 1
                    break;
                case 22:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center), 1, true, center)); //-z up 2
                    break;
                case 23:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, false, center)); //-z up 3
                    break;

                default:
                    returnPyramidCoords.AddRange(tempPyramidCoords);
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

        //        void drawTileFromSurround(int n)
        //        {
        //            (posRot, int) neighborTilePosRot = neighborTilePositionsWithNrFacesTouchingRootTile[n % neighborTilePositionsWithNrFacesTouchingRootTile.Count];

        //            List<pyramidCoord> pyramids = generateSingleTilePyramidCoords(neighborTilePosRot.Item1.pos, neighborTilePosRot.Item1.rot);
        //            //Debug.Log("pyramids count: " + pyramids.Count);
        //            surroundVertices.Clear();
        //            surroundTriangles.Clear();

        //            foreach (pyramidCoord p in pyramids)
        //            {
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 0);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 1);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 2);

        //                surroundTriangles.Add(16 * drawTilePyramidCount + 3);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 4);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 5);

        //                surroundTriangles.Add(16 * drawTilePyramidCount + 6);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 7);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 8);

        //                surroundTriangles.Add(16 * drawTilePyramidCount + 9);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 10);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 11);

        //                surroundTriangles.Add(16 * drawTilePyramidCount + 12);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 14);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 13);

        //                surroundTriangles.Add(16 * drawTilePyramidCount + 12);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 15);
        //                surroundTriangles.Add(16 * drawTilePyramidCount + 14);

        //                drawTilePyramidCount += 1;

        //                switch (p.pyramid)
        //                {
        //                    case 0:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
        //                        break;
        //                    case 1:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));// cw viewed from center
        //                        break;
        //                    case 2:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));// cw viewed from center
        //                        break;
        //                    case 3:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));// cw viewed from center
        //                        break;
        //                    case 4:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, 0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, 0.5f));// cw viewed from center
        //                        break;
        //                    case 5:
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));

        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, 0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(-0.5f, -0.5f, -0.5f));//
        //                        surroundVertices.Add(new Vector3((float)p.pos.x, (float)p.pos.y, (float)p.pos.z) + new Vector3(0.5f, -0.5f, -0.5f));// cw viewed from center
        //                        break;
        //                    default:
        //                        //Debug.LogError("ERROR invalid pyramid!");
        //                        break;
        //                }
        //            }
        //        }

        void generateSurroundMesh()
        {
            surroundTriangles.Clear();
            surroundVertices.Clear();
            pyramidSurroundForMesh.Clear(); // test
            if (activeNode != null)
            {
                if (activeNode.currentPyramids != null)
                {
                    pyramidSurroundForMesh.AddRange(activeNode.currentPyramids);
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
                //pyramidSurroundForMesh.Clear(); // test
                if (activeNode != null)
                {
                    if (activeNode.currentPyramids != null)
                    {
                        pyramidSurroundForMesh.AddRange(activeNode.currentPyramids);
                    }
                    else
                    {
                        Debug.Log("activeNode.currentPyramids is null!");
                    }
                }
                else
                {
                    Debug.Log("activeNode is null!"); // ERROR HERE! activeNode is null!!!
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
            pyramids.AddRange(firstTileInLayer[0].pyramidCoords);
            foreach (tile n in firstTileInLayer[0].next)
            {
                pyramids.AddRange(n.pyramidCoords);
            }
        }



        // TODO: more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!

        // TODO: matching rules !!! 


        // TODO:: visualise sorted tilePos list...

        //List<(posRot, int)> calculateAllNeighborTilePositions(List<pyramidCoord> pyramidCluster)

        List<possibleTilePosition> calculateAllNeighborTilePositions(List<pyramidCoord> pyramidCluster)
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
                            if (pyramidSurround.Exists(p => int3equal(p.pos, testPC.pos) && p.pyramid == testPC.pyramid) == true)
                            {
                                overlap += 1;

                                // debug
                                if (int3equal(neighborCubePos, new int3(testPosX, testPosY, testPosZ)) == true && rot == testRot)
                                {
                                    List<pyramidCoord> p = pyramidSurround.FindAll(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == testPC.pyramid));

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
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z), 1)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z)) == true && p.pyramid == 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 1:
                                        // 2, 3, 4, 5, x-1: 0
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z), 0)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z)) == true && p.pyramid == 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 2:
                                        // 0, 1, 4, 5, y+1: 3
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z), 3)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z)) == true && p.pyramid == 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 3:
                                        // 0, 1, 4, 5, y-1: 2
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z), 2)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z)) == true && p.pyramid == 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 4:
                                        // 0, 1, 2, 3, z+1: 5
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1), 5)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1)) == true && p.pyramid == 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 5:
                                        // 0, 1, 2, 3, z-1: 4
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, testPC.pos) == true && p.pyramid == 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1), 4)) == true)
                                        if (pyramidCluster.Exists(p => (int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1)) == true && p.pyramid == 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                }
                            }

                            //Debug.Log("nrFacesTouchRootTile: " + nrFacesTouchRootTile); // 0, 1, 2, 3, 4, 6, 7, 9 OK

                            List<matchingRule> newMatchingRules = new List<matchingRule>();
                            if (neighborCubePos.x == testPosX && neighborCubePos.y == testPosY && neighborCubePos.z == testPosZ && rot == testRot)
                            {
                                newMatchingRules = generateSingleTileMatchingRules(new posRot(neighborCubePos, rot), 2); // ???  matching rules count =  14  ???
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

                                    Debug.Log("fits! "); // 4 OK
                                }
                                else
                                {
                                    // triangle neighbor
                                    addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), nrFacesTouchRootTile, overlap, false);

                                    Debug.Log("fits! "); // 1300 OK;
                                }

                            }

                        }

                        


                        // debug
                        //if (int3equal(neighborCubePos, new int3(testPosX, testPosY, testPosZ)) == true && rot == testRot)
                        //{
                        //    debgPyramidsGreen.Clear();
                        //
                        //    debgPyramidsGreen.AddRange(debgPyramids);
                        //}

                        //// debug
                        //if (int3equal(neighborCubePos, new int3(testPosX, testPosY, testPosZ)) == true && rot == testRot)
                        //{
                        //    testNrFacesTouchRootTile = nrFacesTouchRootTile;
                        //}

                    }
                }

            }


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




            // // draw test tile
            // surroundRootNode = new node(new posRot(new int3(testPosX, testPosY, testPosZ), testRot));
            // surroundRootNode.currentPyramids = generateSingleTilePyramidCoords(surroundRootNode.placement.pos, surroundRootNode.placement.rot);
            // activeNode = surroundRootNode;
            // // draw test
            // generateSurroundMesh();


            //Debug.Log("neighborTilePositions count: " + newNeighborTilePositions.Count); // 1304

            return newNeighborTilePositions;
        }

        //void calculateAllNeighborTilePositions(List<pyramidCoord> pyramidCluster)
        //{
        //    List<matchingRule> clusterMatchingRules = generateClusterMatchingRules(pyramidCluster);

        //    Debug.Log("in calculateAllNeighborTilePositions() pyramidCluster count: " + pyramidCluster.Count); // 19 OK

        //    List<(posRot, int)> newNeighborTilePositions = new List<(posRot, int)>();
        //    List<int3> neighborCubes = new List<int3>(); // all possible neighbor cube positions 

        //    foreach (pyramidCoord p in pyramidCluster)
        //    {
        //        for (int i = -2; i < 3; i++)
        //        {
        //            for (int j = -2; j < 3; j++)
        //            {
        //                for (int k = -2; k < 3; k++)
        //                {
        //                    if (neighborCubes.Exists(n => int3equal(n, add(p.pos, new int3(i, j, k)))) == false)
        //                    {
        //                        neighborCubes.Add(add(p.pos, new int3(i, j, k)));

        //                        int3 newpos = add(p.pos, new int3(i, j, k));
        //                        //debgLstRed.Add(new Vector3((float)newpos.x, (float)newpos.y, (float)newpos.z));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    Debug.Log("neighbor cubes count: " + neighborCubes.Count); // 270 

        //    //List<posRot> neighborTilePos = new List<posRot>(); // all possible neighbor tile positons and rotations around cluster -> moved to variables

        //    neighborTilePositionsWithNrFacesTouchingRootTile.Clear();

        //    //foreach (int3 neighborCubePos in neighborCubes) // off for test
        //    //{
        //        List<pyramidCoord> testTilePyramidCoords = new List<pyramidCoord>();
        //        //for (int rot = 0; rot < 24; rot++) // off for test
        //        //{
        //            testTilePyramidCoords = generateSingleTilePyramidCoords(neighborCubes[testPosRot / 24], testPosRot % 24); // center = rot / 24

        //            int overlap = 0;

        //            int nrFacesTouchRootTile = 0;

        //            if (calculateOverlap(pyramidCluster, testTilePyramidCoords) == false)
        //            {
        //                Debug.Log("no overlap"); // 6076
        //                foreach (pyramidCoord testPC in testTilePyramidCoords)
        //                {
        //                    if (pyramidSurround.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == testPC.pyramid) == true)
        //                    {
        //                        overlap += 1;

        //                        // TODO: more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!

        //                        //              Y
        //                        //              ^
        //                        //              |
        //                        //              +------> Z
        //                        //             /
        //                        //            X  

        //                        //                  2 1
        //                        //                  |/
        //                        //              5 --+-- 4
        //                        //                 /|
        //                        //                0 3  

        //                        switch (testPC.pyramid)
        //                        {
        //                            case 0:
        //                                // 2, 3, 4, 5, x+1: 1
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 2) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 3) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 4) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 5) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z), 1)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z)) == true && p.pyramid == 1) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                            case 1:
        //                                // 2, 3, 4, 5, x-1: 0
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 2) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 3) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 4) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 5) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z), 0)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z)) == true && p.pyramid == 0) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                            case 2:
        //                                // 0, 1, 4, 5, y+1: 3
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 0) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 1) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 4) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 5) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z), 3)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z)) == true && p.pyramid == 3) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                            case 3:
        //                                // 0, 1, 4, 5, y-1: 2
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 0) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 1) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 4)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 4) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 5)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 5) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z), 2)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z)) == true && p.pyramid == 2) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                            case 4:
        //                                // 0, 1, 2, 3, z+1: 5
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 0) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 1) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 2) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 3) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1), 5)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1)) == true && p.pyramid == 5) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                            case 5:
        //                                // 0, 1, 2, 3, z-1: 4
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 0)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 0) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 1)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 1) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 2)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 2) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(testPC.pos, 3)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 3) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                //if (pyramidCluster.Exists(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1), 4)) == true)
        //                                if (pyramidCluster.Exists(p => int3equal(p.pos, new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1)) == true && p.pyramid == 4) == true)
        //                                {
        //                                    nrFacesTouchRootTile += 1;
        //                                }
        //                                break;
        //                        }
        //                    }
        //                }

        //                Debug.Log("nrFacesTouchRootTile before matchingRules: " + nrFacesTouchRootTile); // 0, 1, 2, 3, 4, 6, 7, 9 OK

        //                // at least one pyramid must touch cluster -> at least one overlap with pyramid surround
        //                int nrOverlapPyramids = calculateNumberPyramidsOverlap(pyramidSurround, testTilePyramidCoords);
        //                if (nrOverlapPyramids > 0)
        //                {
        //                    Debug.Log("nrOverlapPyramids > 0");// 1464
        //                    Debug.Log("nrOverlapPyramids: " + nrOverlapPyramids);
        //                    Debug.Log("nrOverlapPyramids: " + nrOverlapPyramids + ", nrFacesTouchRootTile: " + nrFacesTouchRootTile); // 19, 9 OK
        //                    bool fits = true;
        //                    // TODO: matching rules...
        //                    //
        //                    //debgPyramidsRed.Clear();
        //                    //debgPyramidsBlue.Clear();
        //                    //debgPyramidsGreen.Clear();

        //                  //  List<matchingRule> newMatchingRules = generateSingleTileMatchingRules(new posRot(neighborCubePos, rot)); // ???  matching rules count =  14  ???
        //                    List<matchingRule> newMatchingRules = generateSingleTileMatchingRules(new posRot(neighborCubes[testPosRot / 24], testPosRot % 24)); // ???  matching rules count =  14  ???

        //                    // rootTileMatchingRules -> compare rules ...  matchingRules of cluster: clusterMatchingRules

        //            //              Y
        //            //              ^
        //            //              |
        //            //              +------> Z
        //            //             /
        //            //            X  

        //            //                  2 1
        //            //                  |/
        //            //              5 --+-- 4
        //            //                 /|
        //            //                0 3  

        //            //public matchingRule(int3 position, int direction, int rul)
        //            //dir: 0, 1, 2, 3, 4, 5
        //            // rule: +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

        //                    foreach (matchingRule tileRule in newMatchingRules)
        //                    {
        //                        if (tileRule.rule != 0)
        //                        {
        //                            Debug.Log("tileRule > 0!"); // 19032
        //                            switch (tileRule.dir)
        //                            {

        //                                //if (pyramidCluster.Exists(p => int3equal(p.pos, testPC.pos) == true && p.pyramid == 2))


        //                                case 0: // tile dir = +X
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(1, 0, 0)), 1, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(1, 0, 0)), 1, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(1, 0, 0))) == true && r.dir == 1 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(1, 0, 0))) == true && r.dir == 1 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                        // does not fit!
        //                                    }
        //                                    break;
        //                                case 1: // tile dir = -X
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(-1, 0, 0)), 0, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(-1, 0, 0)), 0, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(-1, 0, 0))) == true && r.dir == 0 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(-1, 0, 0))) == true && r.dir == 0 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 2: // tile dir = +Y
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 1, 0)), 3, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 1, 0)), 3, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 1, 0))) == true && r.dir == 3 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 1, 0))) == true && r.dir == 3 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 3: // tile dir = -Y
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, -1, 0)), 2, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, -1, 0)), 2, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, -1, 0))) == true && r.dir == 2 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, -1, 0))) == true && r.dir == 2 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 4: // tile dir = +Z
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, 1)), 5, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, 1)), 5, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, 1))) == true && r.dir == 5 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, 1))) == true && r.dir == 5 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 5: // tile dir = -Z
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, -1)), 4, tileRule.rule)) == true ||
        //                                    //    clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, -1)), 4, 0)) == true)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, -1))) == true && r.dir == 4 && r.rule == tileRule.rule) ||
        //                                        clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, -1))) == true && r.dir == 4 && r.rule == 0) == true)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                            }
        //                        }
        //                        else // rule = 0 // ERROR HERE // 1464
        //                        {
        //                            Debug.Log("tile rule 0!"); // 1464
        //                            switch (tileRule.dir)
        //                            {
        //                                case 0:
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(1, 0, 0)), 1, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(1, 0, 0))) == true && r.dir == 1 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 1:
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(-1, 0, 0)), 0, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(-1, 0, 0))) == true && r.dir == 0 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 2:
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 1, 0)), 3, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(-1, 0, 0))) == true && r.dir == 3 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 3:
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, -1, 0)), 2, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, -1, 0))) == true && r.dir == 2 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 4: // tile rule dir +Z
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, 1)), 5, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, 1))) == true && r.dir == 5 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                                case 5:
        //                                    //if (clusterMatchingRules.Exists(new matchingRule(add(tileRule.pos, new int3(0, 0, -1)), 4, 0)) == false)
        //                                    if (clusterMatchingRules.Exists(r => int3equal(r.pos, add(tileRule.pos, new int3(0, 0, -1))) == true && r.dir == 4 && r.rule == 0) == false)
        //                                    {
        //                                        fits = false;
        //                                    }
        //                                    break;
        //                            }

        //                            //              Y
        //                            //              ^
        //                            //              |
        //                            //              +------> Z
        //                            //             /
        //                            //            X  

        //                            //                  2 1
        //                            //                  |/
        //                            //              5 --+-- 4
        //                            //                 /|
        //                            //                0 3  
        //                        }
        //                    }

        //                    if (fits == true)
        //                    {
        //                //clusterMatchingRules

        //                // struct matichingRule: 
        //                // public int3 pos;
        //                // public int dir; // 0, 1, 2, 3, 4, 5
        //                // public int rule; // +1, 0, -1 of cluster / tile  // -> -1, 0, 1 for neighbor

        //                //addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), nrFacesTouchRootTile); //   test only nr faces touch...TODO: with overlap...
        //                // test off

        //                //List<pyramidCoord> testPyramids = generateSingleTilePyramidCoords(neighborCubePos, rot); // OFF FOR TEST
        //                List<pyramidCoord> testPyramids = generateSingleTilePyramidCoords(neighborCubes[testPosRot / 24], testPosRot % 24);

        //                // node newNode = new node(new posRot(neighborCubePos, rot));// OFF FOR TEST
        //                node newNode = new node(new posRot(neighborCubes[testPosRot / 24], testPosRot % 24));

        //                newNode.currentPyramids = testPyramids;
        //                        node temp = activeNode;
        //                        activeNode.next = newNode;
        //                        activeNode = newNode;

        //                        generateSurroundMesh();
        //                        

        //                        activeNode = temp;

        //                        Debug.Log("calculateAllNeighborTile Positions: addSorted! nrFacesTouchRootTile after sort: " + nrFacesTouchRootTile);
        //                        //
        //                        // ERROR HERE !!! max is 1 -> is reset !!!

        //                    }
        //                    else
        //                    {
        //                        // fits = false
        //                        //List<pyramidCoord> testPyramids = generateSingleTilePyramidCoords(neighborCubes[testPosRot / 24], testPosRot % 24);

        //                        //node newNode = new node(new posRot(neighborCubes[testPosRot / 24], testPosRot % 24));
        //                        //
        //                        //newNode.currentPyramids = testPyramids;
        //                        //node temp = activeNode;
        //                        //activeNode.next = newNode;
        //                        //activeNode = newNode;
        //                        //
        //                        //generateSurroundMesh();
        //                        
        //                        //
        //                        //activeNode = temp;
        //                    }

        //                    //addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), overlap); // overlap     // ^ test only nr faces touch...TODO: with overlap...

        //                    //newNeighborTilePositions.Add((new posRot(neighborCubePos, rot), 0));

        //                    //Debug.Log("neighborTilePos: " + neighborCubePos.x + ", " + neighborCubePos.y + ", " + neighborCubePos.z + ", rot: " + rot);
        //                }
        //                //else
        //                //{
        //                //    Debug.Log("no pyramids overlap: nr faces touch root tile: " + nrFacesTouchRootTile); // 0 OK
        //                //}

        //                //}
        //            }
        //        //}
        //    //}
        //    // sort list by number overlap pyramids -> insert sorted !!!

        //    // TODO: more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!

        //    Debug.Log("in calculateAllNeighborTile Positions() returns newNeighborTilePositions.Count = " + newNeighborTilePositions.Count); // count = 4 (??)  ERROR HERE !!!

        //    // Debug.Log("neighborTilePositions[0]: nrFacesTouchRootTile: " + newNeighborTilePositions[0].Item2); // 1 // ERROR HERE !!! // TEST OFF

        //    //foreach ((posRot,int) pr in newNeighborTilePositions)
        //    //{
        //    //    Debug.Log("(" + pr.Item1.pos.x + ", " + pr.Item1.pos.y + ", " + pr.Item1.pos.z + ") | overlap: " + pr.Item2);
        //    //}

        //    //return newNeighborTilePositions; // test off
        //}

        public static int max(int a, int b)
        {
            if (a >= b)
            {
                return a;
            }
            else
            {
                return b;
            }
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

        //if (neighborTilePositions.Count > 0)
        //{
        //    if (squareFaceNeighbor == true) // 1.
        //    {
        //        if (neighborTilePositions[0].squareFaceNeighbor == false) // 1.
        //        {
        //            neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //        }
        //        else
        //        {
        //            if (nrFacesTouchRootTile > neighborTilePositions[0].nrFacesTouch) // 2.
        //            {
        //                neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //            }
        //            else
        //            {
        //                // squareFaceNeighbor == true
        //                // nrFacesTouchRootTile <= pos[0].nrFacesTouchRootTile
        //                // rotation < rot[0] -> add at 0
        //                
        //                bool added = false;
        //
        //                if (nrFacesTouchRootTile == neighborTilePositions[0].nrFacesTouch) // test! new !!!
        //                {
        //                    if (newTile.rot < neighborTilePositions[0].posrot.rot)
        //                    {
        //                        neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                        added = true;
        //                    }
        //                }
        //
        //                if (added == false)
        //                {
        //                    for (int i = 1; i < neighborTilePositions.Count; i++)
        //                    {
        //                        if (neighborTilePositions[i].squareFaceNeighbor == true) // 1.
        //                        {
        //                            if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch) // 2.
        //                            {
        //                                if (newTile.rot < neighborTilePositions[i].posrot.rot) // 3.
        //                                {
        //                                    neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                                    added = true;
        //                                    break;
        //                                }
        //                            }
        //
        //                            if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch) // 2.
        //                            {
        //                                neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                                added = true;
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                            added = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //                if (added == false)
        //                {
        //                    neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                }
        //            }
        //        }
        //    }
        //    else // squareFaceNeighbor == false
        //    {
        //        if (neighborTilePositions[0].squareFaceNeighbor == false) // 1.
        //        {
        //            if (nrFacesTouchRootTile > neighborTilePositions[0].nrFacesTouch) // 2.
        //            {
        //                neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //            }
        //            else
        //            {
        //                // squareFaceNeighbor == false
        //                // nrFacesTouchRootTile <= pos[0].nrFacesTouchRootTile
        //                // rotation < rot[0] -> add at 0
        //
        //                bool added = false;
        //
        //                if (nrFacesTouchRootTile == neighborTilePositions[0].nrFacesTouch) // test! new !!!
        //                {
        //                    if (newTile.rot < neighborTilePositions[0].posrot.rot)
        //                    {
        //                        neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                        added = true;
        //                    }
        //                }
        //
        //                if (added == false)
        //                {
        //                    for (int i = 1; i < neighborTilePositions.Count; i++)
        //                    {
        //                        if (neighborTilePositions[i].squareFaceNeighbor == false) // 1.
        //                        {
        //                            if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch) // 2.
        //                            {
        //                                if (newTile.rot < neighborTilePositions[i].posrot.rot) // 3.
        //                                {
        //                                    neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                                    added = true;
        //                                    break;
        //                                }
        //                            }
        //
        //                            if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch) // 2.
        //                            {
        //                                neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                                added = true;
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //                if (added == false)
        //                {
        //                    neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                }
        //            }
        //        }
        //        else // squareFaceNeighbor == false, neighborTilePositions[0].squareFaceNeighbor == true
        //        {
        //            bool added = false;
        //            for (int i = 1; i < neighborTilePositions.Count; i++)
        //            {
        //                if (neighborTilePositions[i].squareFaceNeighbor == false) // 1.
        //                {
        //                    if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch) // 2.
        //                    {
        //                        if (newTile.rot < neighborTilePositions[i].posrot.rot) // 3.
        //                        {
        //                            neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                            added = true;
        //                            break;
        //                        }
        //                    }
        //
        //                    if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch) // 2.
        //                    {
        //                        neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                        added = true;
        //                        break;
        //                    }
        //                }
        //            }
        //            if (added == false)
        //            {
        //                neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //            }
        //        }
        //    }
        //}
        //else
        //{
        //    neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //}

        // if (neighborTilePositions.Count > 0)
        // {
        //     if (squareFaceNeighbor == true)
        //     {
        //         if (neighborTilePositions[0].squareFaceNeighbor == false)
        //         {
        //             neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //         }
        //         else
        //         {
        //             if (nrFacesTouchRootTile > neighborTilePositions[0].nrFacesTouch)
        //             {
        //                 neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //             }
        //             else
        //             {
        //                 bool added = false;
        //                 for (int i = 1; i < neighborTilePositions.Count; i++)
        //                 {
        //                     if (neighborTilePositions[i].squareFaceNeighbor == true)
        //                     {
        //                         if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch)
        //                         {
        //                             neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                             added = true;
        //                             break;
        //                         }
        // 
        //                         // TODO: sort by rotation 
        //                         if (nrFacesTouchRootTile == neighborTilePositions[i].nrFacesTouch)
        //                         {
        //                             if (newTile.rot < neighborTilePositions[i].posrot.rot)
        //                             {
        //                                 neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                                 added = true;
        //                                 break;
        //                             }
        //                         }
        //                         
        // 
        //                     }
        //                     else
        //                     {
        //                         neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                         added = true;
        //                         break;
        //                     }
        //                 }
        //                 if (added == false)
        //                 {
        //                     neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                 }
        //             }
        //         }
        //     }
        //     else // squareFaceNeighbor == false
        //     {
        //         if (neighborTilePositions[0].squareFaceNeighbor == false)
        //         {
        //             if (nrFacesTouchRootTile > neighborTilePositions[0].nrFacesTouch)
        //             {
        //                 neighborTilePositions.Insert(0, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //             }
        //             else
        //             {
        //                 bool added = false;
        //                 for (int i = 1; i < neighborTilePositions.Count; i++)
        //                 {
        //                     if (neighborTilePositions[i].squareFaceNeighbor == false)
        //                     {
        //                         if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch)
        //                         {
        //                             neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                             added = true;
        //                             break;
        //                         }
        //                     }
        //                 }
        //                 if (added == false)
        //                 {
        //                     neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             bool added = false;
        //             for (int i = 1; i < neighborTilePositions.Count; i++)
        //             {
        //                 if (neighborTilePositions[i].squareFaceNeighbor == false)
        //                 {
        //                     if (nrFacesTouchRootTile > neighborTilePositions[i].nrFacesTouch)
        //                     {
        //                         neighborTilePositions.Insert(i - 1, new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //                         added = true;
        //                         break;
        //                     }
        //                 }
        //             }
        //             if (added == false)
        //             {
        //                 neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     neighborTilePositions.Add(new possibleTilePosition(newTile, overlap, nrFacesTouchRootTile, squareFaceNeighbor));
        // }


        //        node addNodeFirstIn(posRot pr)
        //        {
        //            node newNode = new node(pr);
        //            newNode.next = surroundRootNode;
        //            surroundRootNode = newNode;
        //            return newNode;
        //        }

        //        void deleteFirstNode()
        //        {
        //            node next = surroundRootNode.next;
        //            surroundRootNode = next;
        //        }

        List<possibleTilePosition> recalculatePossibleTilePositions(List<possibleTilePosition> possibleTilePositions, List<pyramidCoord> allPyramids, node newNode)//, posRot placement)
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

        //foreach (possibleTilePosition p in possibleTilePositions)
        //{
        //    if (calculateOverlap(newPyramids, generateSingleTilePyramidCoords(p.posrot.pos, p.posrot.rot)) == false)
        //    {
        //        newPossibleTilePositions = addSorted(newPossibleTilePositions, p.posrot, p.nrFacesTouch, p.overlap, p.squareFaceNeighbor);
        //
        //        //newPossibleTilePositions.Add(p); // TODO: sort...
        //    }
        //}


        // TODO: update matching rules of cluster! -> store in node!!!


        //
        //// ERROR in recalculate -> some positions overlap existing tiles ( / are not displayed ??? ) !!!
        //// ERROR: some positions are double !!! (( is OK)
        //
        //Debug.Log("old possibleTilePositions count " + possibleTilePositions.Count); // ERROR count = 0 !!!
        //List<(posRot, int)> newPossibleTilePositions = new List<(posRot, int)>();
        //foreach ((posRot, int) p in possibleTilePositions)
        //{
        //    if ((!calculateOverlap(newPyramids, generateSingleTilePyramidCoords(p.Item1.pos, p.Item1.rot))) )// &&
        //        //(!(placement.pos.x == p.Item1.pos.x && placement.pos.y == p.Item1.pos.y && placement.pos.z == p.Item1.pos.z))) // ??? placement is relevant !!!
        //    {
        //        //newPossibleTilePositions.Add(p); // ???
        //
        //        List<(posRot, int)> TEMPnewPossibleTilePositions = addSorted(newPossibleTilePositions, p.Item1, calculateNrPyramidsOverlap(newPyramids, generateSingleTilePyramidCoords(p.Item1.pos, p.Item1.rot))); //TODO !!! // TODO  !!!
        //
        //        Debug.Log("addSorted"); // called 405 times
        //        Debug.Log("current count TEMP: " + TEMPnewPossibleTilePositions.Count); // ERROR count always 1 !!!
        //        newPossibleTilePositions = TEMPnewPossibleTilePositions;
        //        Debug.Log("current count new: " + newPossibleTilePositions.Count); // ERROR count always 1 !!!
        //
        //        // TODO: sort with new existing pyramids -> positions of high face neighbors has changed !!!
        //    }
        //}
        //Debug.Log("recalculatePossibleTilePosit() positions count " + newPossibleTilePositions.Count); // ERROR count = 0 !!!
        //return newPossibleTilePositions;


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


        bool detectHoles(List<pyramidCoord> allCurrentPyramidsOfSurround)
        {
            /////////////////////////////////////////////////
            // TODO: detect ALL unaccessable pyramids -> all pyramids of pyramidSurround must be able to fill with at least one tile position !!!
            //
            //  -> TOO SLOW !!!
            //
            /////////////////////////////////////////////////

            bool testTilePossible = true;

            foreach (pyramidCoord p in pyramidSurround)
            {
                if (allCurrentPyramidsOfSurround.Exists(c => int3equal(c.pos, p.pos) == true && c.pyramid == p.pyramid) == false &&

                    firstTileInLayer[0].pyramidCoords.Exists(f => int3equal(f.pos, p.pos) == true && f.pyramid == p.pyramid) == false)
                {
                    int3 neighborCube = p.pos;

                    bool pyramidPossible = false;

                    // tile pos +-1 in x,y,z...
                    for (int x = -1; x < 2; x++)
                    {
                        for (int y = -1; y < 2; y++)
                        {
                            for (int z = -1; z < 2; z++)
                            {
                                for (int r = 0; r < 24; r++)
                                {
                                    List<pyramidCoord> testTile = generateSingleTilePyramidCoords(add(neighborCube, new int3(x, y, z)), r);

                                    if (calculateOverlap(allCurrentPyramidsOfSurround, testTile) == false && calculateOverlap(pyramidSurround, testTile) == true)
                                    {
                                        // at least one tile must fit
                                        pyramidPossible = true;
                                        break;
                                    }
                                }
                                if (pyramidPossible == true)
                                {
                                    break;
                                }
                            }
                            if (pyramidPossible == true)
                            {
                                break;
                            }
                        }
                        if (pyramidPossible == true)
                        {
                            break;// before break: 156
                        }
                    }

                    if (pyramidPossible == false)
                    {
                        debgPyramidsGreen1.Add(p);
                        testTilePossible = false;
                    }
                }
            }



            //int debugCase = -1;
            bool hole = false;
            int freeTriangleNeighbors = 0;
            bool squareFaceNeighborFilled = false;
            foreach (pyramidCoord p in pyramidSurround)
            {
                if (allCurrentPyramidsOfSurround.Contains(p) == false && firstTileInLayer[0].pyramidCoords.Contains(p) == false) // p not in current or root -> p not yet filled
                {
                    List<pyramidCoord> triangleNeighborPyramids = getAllTriangleFaceNeighborPyramids(p);
                    pyramidCoord squareFaceNeighborPyramid = getSquareFaceNeighborPyramid(p);

                    // all neighbors of p are filled with current surround pyramids or rootTile
                    // -> at least one NOT filled with current surround pyramids and root tile


                    if ((firstTileInLayer[0].pyramidCoords.Contains(squareFaceNeighborPyramid) == false) && (allCurrentPyramidsOfSurround.Contains(squareFaceNeighborPyramid) == false))
                    {
                        //hole = false;
                        //Debug.Log("hole set to false"); // do not set hole to false after being set to true !!!
                        //return false;
                    }
                    else
                    {

                        foreach (pyramidCoord triangleNeighborOfP in triangleNeighborPyramids)
                        {
                            if ((firstTileInLayer[0].pyramidCoords.Contains(triangleNeighborOfP) == false) && (allCurrentPyramidsOfSurround.Contains(triangleNeighborOfP) == false))
                            {
                                freeTriangleNeighbors += 1;
                            }
                        }

                        bool oppositePyramidFilled = false;
                        switch (p.pyramid)
                        {
                            case 0:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 1)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 1)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 0;
                                }
                                break;
                            case 1:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 0)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 0)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 1;
                                }
                                break;
                            case 2:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 3)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 3)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 2;
                                }
                                break;
                            case 3:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 2)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 2)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 3;
                                }
                                break;
                            case 4:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 5)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 4;//
                                    //debgPyramidsRed.Add(new pyramidCoord(p.pos, 5));
                                    //Debug.Log("hole ERROR here in firstTileInLayer contains");
                                }
                                if (allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 5)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 4;//
                                    //debgPyramidsRed.Add(new pyramidCoord(p.pos, 5)); // ERROR HERE !!!
                                    foreach (pyramidCoord t in allCurrentPyramidsOfSurround) // ERROR HERE !!! -> pyramids are not cleared !!!
                                    {
                                        //debgPyramidsRed.Add(t);
                                    }
                                    //Debug.Log("hole ERROR here in allCurrentPyramidsOfSurround contains");
                                }
                                break;
                            case 5:
                                if (firstTileInLayer[0].pyramidCoords.Contains(new pyramidCoord(p.pos, 4)) || allCurrentPyramidsOfSurround.Contains(new pyramidCoord(p.pos, 4)))
                                {
                                    oppositePyramidFilled = true;
                                    //debugCase = 5;
                                }
                                break;
                        }

                        if (firstTileInLayer[0].pyramidCoords.Contains(squareFaceNeighborPyramid) || allCurrentPyramidsOfSurround.Contains(squareFaceNeighborPyramid))
                        {
                            squareFaceNeighborFilled = true;
                        }
                        // if (freeTriangleNeighbors >= 2)
                        // {
                        // 
                        // }
                        // if (oppositePyramidFilled == true)
                        // {
                        // 
                        // }

                        // if (squareFaceNeighborFilled == true && oppositePyramidFilled == false && freeTriangleNeighbors >= 2)
                        // {
                        //     hole = false;
                        // }
                        // 
                        // if (squareFaceNeighborFilled == false)
                        // {
                        //     hole = false;
                        // }

                        // if (squareFaceNeighborFilled == true && oppositePyramidFilled == true)
                        // {
                        //     hole = true;
                        // }

                        //if (squareFaceNeighborFilled == true && oppositePyramidFilled == true)
                        //{
                        //    //Debug.Log("HOLE = true ???!!! debugCase: " + debugCase);
                        //}

                        if (testTilePossible == false)
                        {
                            hole = true;
                        }

                        if (squareFaceNeighborFilled == true && (oppositePyramidFilled == true || freeTriangleNeighbors < 3))
                        {
                            hole = true;
                            //Debug.Log("hole set to true!");

                            debgPyramidsBlue1.Add(p);

                            //if (freeTriangleNeighbors < 3)
                            //{
                            //    Debug.Log("freeTriangleNeighbors: " + freeTriangleNeighbors);
                            //}
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

                }
            }
            //if (hole == true)
            //{
            //    Debug.Log("hole detected !!!");
            //}
            //else
            //{
            //    Debug.Log("no hole detected");
            //}

            //Debug.Log("detectHoles(): return " + hole);

            return hole;
        }

        //        //copy List / clone List: new List<posRot>(oldList);

        void tilingStep()
        {
            //Debug.Log("tilingStep()");

            //            // TODO: rate tile placements by # filled pyramids -> sort possibletilepositions


            node newNode;

            if (activeNode != null)
            {
                //Debug.Log("current nr possibleTilePositions: " + activeNode.possibleTilePositions.Count); // ERROR 0 !!!

                if (activeNode.possibleTilePositions.Count > 0 && activeNode.possibleTilePositions.Count > activeNode.currentNextIndex)
                {
                    Debug.Log("next step:");
                    if (activeNode.possibleTilePositions.Count > 0)
                    {
                        Debug.Log("new node tile slot: " + 0 + ": pos: (" + activeNode.possibleTilePositions[0].posrot.pos.x + ", " +
                            activeNode.possibleTilePositions[0].posrot.pos.y + ", " + activeNode.possibleTilePositions[0].posrot.pos.z + "), " +
                            "rot: " + activeNode.possibleTilePositions[0].posrot.rot + ", squareFaceNeighbor: " + activeNode.possibleTilePositions[0].squareFaceNeighbor +
                            ", nrFacesTouchRootTile: " + activeNode.possibleTilePositions[0].nrFacesTouch + ", overlap: " + activeNode.possibleTilePositions[0].overlap +
                            ", maxCubeDistance: " + activeNode.possibleTilePositions[0].maxCubeDistance);
                    }
                    //for (int i = 0; i < 5; i++)
                    //{
                    //    Debug.Log("possibleTilePosition[" + activeNode.currentNextIndex + " + " + i + "]: pos: (" + activeNode.possibleTilePositions[activeNode.currentNextIndex + i].posrot.pos.x + ", " +
                    //        activeNode.possibleTilePositions[activeNode.currentNextIndex + i].posrot.pos.y + ", " +
                    //        activeNode.possibleTilePositions[activeNode.currentNextIndex + i].posrot.pos.z + "), " +" rot: " + activeNode.possibleTilePositions[activeNode.currentNextIndex + i].posrot.rot);
                    //}
                    //Debug.Log("new node possibleTilePositions Count: " + activeNode.possibleTilePositions.Count);
                    newNode = new node(activeNode.possibleTilePositions[activeNode.currentNextIndex].posrot);

                    //Debug.Log("new node currentNextIndex: " + activeNode.currentNextIndex + "pos: (" + newNode.placement.pos.x + ", " + newNode.placement.pos.y + ", " + newNode.placement.pos.z + "), rot: " + newNode.placement.rot);

                    //Debug.Log("new node: nrFacesTouchRootTile: " + activeNode.possibleTilePositions[activeNode.currentNextIndex].nrFacesTouch);


                    // TODO: backtrack only to equal best tiling positions !!!
                    //
                    activeNode.currentNextIndex = activeNode.currentNextIndex + 1; // -> overflow gets checked next iteration!







                    List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);

                    int nrNewPyramidsFilled = calculateNrPyramidsOverlap(pyramidSurround, newPyramids);

                    List<pyramidCoord> allPyramids = new List<pyramidCoord>(activeNode.currentPyramids);

                    allPyramids.AddRange(newPyramids);

                    //bool hole = detectHoles(allPyramids);
                    //
                    //if (hole == false)
                    //{
                        //Debug.Log("new tile: (" + newNode.placement.pos.x + ", " + newNode.placement.pos.y + ", " + newNode.placement.pos.z + "), Rot: " + newNode.placement.rot);


                        newNode.nodeMatchingRules = activeNode.nodeMatchingRules;
                        newNode.nodeMatchingRules.AddRange(generateSingleTileMatchingRules(newNode.placement, 1));


                        newNode.pyramidsFilled = activeNode.pyramidsFilled + nrNewPyramidsFilled;


                        newNode.currentPyramids = allPyramids;

                        newNode.parent = activeNode;
                        activeNode.next = newNode;


                        // TEST remove all indices < currentNextIndex for newNode
                        List<possibleTilePosition> newPossibleTilePositions = new List<possibleTilePosition>(activeNode.possibleTilePositions);
                        if (activeNode.currentNextIndex > 1) // was: 0
                        {
                            for (int i = 0; i < activeNode.currentNextIndex - 1; i++)
                            {
                                newPossibleTilePositions.RemoveAt(0); // remove
                            }
                        }
                        newNode.possibleTilePositions = recalculatePossibleTilePositions(newPossibleTilePositions, allPyramids, newNode);//, newNode.placement); // TEST
                        //newNode.possibleTilePositions = recalculatePossibleTilePositions(activeNode.possibleTilePositions, newPyramids);//, newNode.placement);




                        //                    //newNode.currentNextIndex = activeNode.currentNextIndex; // should be 0 ???
                        newNode.currentNextIndex = 0;

                        //                    // TODO: newNode.currentNextIndex = ??? // TODO !!!  new node(...) -> currentNextIndex set to 0 !!! -> set in recalculatePossibleTilePositions

                        activeNode = newNode;


                        if (drawMode == draw.best)
                        {
                            if (newNode.pyramidsFilled >= maxPyramidsFilled)
                            {
                                if (newNode.pyramidsFilled == maxPyramidsFilled)
                                {
                                    if (newNode.currentPyramids.Count < bestSurroundVertices.Count / 16)
                                    {
                                        generateBestSurroundMesh(true);
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
                                //surroundMesh.Clear(); // off for test

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
                                //surroundMesh.Clear(); // off for test

                                surroundMesh.SetVertices(surroundVertices);
                                surroundMesh.SetTriangles(surroundTriangles, 0);
                                surroundMesh.RecalculateNormals();
                                surroundMeshFilter.mesh = surroundMesh;
                            }
                        }




                        if (maxPyramidsFilled <= newNode.pyramidsFilled)
                        {
                            Debug.Log("pyramids filled: " + newNode.pyramidsFilled + " / " + pyramidSurround.Count); // 158 (/ 192)  // 160 x24  // 165  // 171 // 176 x2 (/180)
                            maxPyramidsFilled = newNode.pyramidsFilled;
                        }

                        if (maxPyramidsFilled == pyramidSurround.Count && onlyOnce == false)
                        {
                            onlyOnce = true;
                            Debug.Log("tiling found !!!");
                        }


                        //Debug.Log("activeNode = newNode in tilingStep()");
                    //}
                    //else
                    //{
                    //    // hole detected with new tile !!!
                    //    //Debug.Log("hole detected -> skip tile position!");
                    //
                    //    //activeNode.currentNextIndex += 1; TEST OFF
                    //}

                }
                else
                //if (activeNode.possibleTilePositions.Count == 0 || activeNode.currentNextIndex >= activeNode.possibleTilePositions.Count) // TEST no else...
                {
                    // TODO: backtrack ...

                    // TODO: backtrack only to equal best tiling positions !!!

                    //Debug.Log("backtrack!");
                    //Debug.Log("activeNode before backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), rot: " + activeNode.placement.rot);
                    //Debug.Log("activeNode before backtrack currentPyramids count: " + activeNode.currentPyramids.Count);

                    //Debug.Log("active node possible tile positions count: " + activeNode.possibleTilePositions.Count);

                    if (activeNode.parent != null)
                    {
                        activeNode = activeNode.parent;
                        activeNode.next = null;
                    }
                    else
                    {
                        Debug.Log("ERROR! active node has no parent !!!");
                    }


                    //activeNode.currentNextIndex = (activeNode.currentNextIndex + 1) % activeNode.possibleTilePositions.Count;

                    //generateSurroundMesh();

                    // TEST
                    //if (drawMode == draw.all) // TODO: snap back to best tiling !
                    //{
                    //    // redraw every step!
                    //    generateSurroundMesh(); // test
                    //}
                    //else
                    //{
                    //    //redrawMesh = true;
                    //    if (maxPyramidsFilled <= activeNode.pyramidsFilled)
                    //    {
                    //        generateBestSurroundMesh(true);
                    //        //generateBestSurroundMesh(); // test
                    //
                    //        maxPyramidsFilled = activeNode.pyramidsFilled; // 157 -> 160 (sorted neighborTilePositions) -> 149 (sorted overlap) -> 154 ( test moved up here) -> 41 BROKEN !!!
                    //        //                                                    -> 160 (x24) (detect holes)
                    //
                    //        //Debug.Log(activeNode.pyramidsFilled + " pyramids filled!"); // ERROR is called but mesh is not re-drawn !!!
                    //    }
                    //    else
                    //    {
                    //        generateBestSurroundMesh(false);
                    //    }
                    //
                    //}

                    //                    if (activeNode.possibleTilePositions.Count == 0)
                    //                    {
                    //                        Debug.Log("backtrack: no possible tile positions!"); // does not get called
                    //                    }
                    //                    if (activeNode.currentNextIndex >= activeNode.possibleTilePositions.Count)
                    //                    {
                    //                        Debug.Log("backtrack: currentNextIndex >= activeNode.possibleTilePositions.Count, count = " + activeNode.possibleTilePositions.Count); // is ccalled
                    //                    }
                    //                    //Debug.Log("activeNode after backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), rot: " + activeNode.placement.rot);
                    //                    //Debug.Log("activeNode.next after backtrack: " + activeNode.next)

                    //Debug.Log("current tile after backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), Rot: " + activeNode.placement.rot);

                    //Debug.Log("activeNode after backtrack currentPyramids count: " + activeNode.currentPyramids.Count);

                    //Debug.Log("possibleTilePositions after backtrack " + activeNode.possibleTilePositions.Count);
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

                newNode.currentPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);

                newNode.pyramidsFilled = calculateNrPyramidsOverlap(pyramidSurround, newPyramids); // NEW !!!

                List<pyramidCoord> rootAndFirstTilePyramids = new List<pyramidCoord>();

                rootAndFirstTilePyramids.AddRange(pyramids);
                rootAndFirstTilePyramids.AddRange(newPyramids);

                newNode.nodeMatchingRules = clusterMatchingRules;
                newNode.nodeMatchingRules.AddRange(generateSingleTileMatchingRules(newNode.placement, 1));

                newNode.possibleTilePositions = neighborTilePositionsWithNrFacesTouchingRootTileAndOverlap;  // ERROR HERE: 4 !!!

                Debug.Log("newNode possibleTilePositions before recalculate: " + newNode.possibleTilePositions.Count); // ERROR HERE: 4 !!!  ((571)) 

                newNode.possibleTilePositions = recalculatePossibleTilePositions(newNode.possibleTilePositions, newPyramids, newNode); // TODO

                newNode.currentNextIndex = 0; // new !!!

                Debug.Log("new node currentNextIndex: in first tile: " + newNode.currentNextIndex);

                Debug.Log("newNode possibleTilePositions recalculated: " + newNode.possibleTilePositions.Count); // ERROR HERE: 0 !!!  ((405))

                activeNode = newNode;
                Debug.Log("activeNode = newNode in tilingStep() -> first tile");

            }
        }

        //        }

        //        // -----
        //        //public IEnumerator startUnifyCoroutine(IEnumerator target)
        //        //{
        //        //    while (target.MoveNext())
        //        //    {
        //        //        if (target.Current?.GetType() == typeof(List<pyramidCoord>))
        //        //        {
        //        //            pyramidSurroundForMesh = (List<pyramidCoord>)target.Current; // ???
        //        //            generateSurroundMesh(); // !!!
        //        //        }
        //        //
        //        //        yield return target.Current;
        //        //    }
        //        //}
        //        // -----



        //        void markPyramidSurroundWithNewTile(posRot newTile)
        //        {
        //            //Debug.Log("markPyramidSurroundWithNewNile() called!"); // called once!
        //            List<pyramidCoord> newTilePyramidCoords = generateSingleTilePyramidCoords(newTile.pos, newTile.rot);

        //            debgPyramidsGreen.Clear();

        //            //foreach ((pyramidCoord, bool) pyrbool in pyramidSurround)
        //            for (int i = 0; i < pyramidSurround.Count; i++)
        //            {
        //                if (newTilePyramidCoords.Exists(pyramidSurround[i].Item1))
        //                {
        //                    (pyramidCoord, bool) temp = pyramidSurround[i];
        //                    temp.Item2 = true;
        //                    pyramidsFilled += 1;
        //                    pyramidSurround[i] = temp;
        //                    //Debug.Log("markPyramidSurroundWithNewTile (" + pyramidSurround[i].Item1.pos.x + ", " + pyramidSurround[i].Item1.pos.y + ", " + pyramidSurround[i].Item1.pos.z + ")"); // 3 times executed! (???)

        //                    //debgLstGreen.Add(new Vector3((float)pyramidSurround[i].Item1.pos.x, (float)pyramidSurround[i].Item1.pos.y, (float)pyramidSurround[i].Item1.pos.z));

        //                    debgPyramidsGreen.Add(pyramidSurround[i].Item1);
        //                }
        //            }
        //        }

        //        //bool calculateOverlapWithMarkedTiles(List<(posRot, bool)> cluster, List<pyramidCoord> tile)
        //        //{
        //        //    bool b = false;
        //        //    
        //        //    foreach ((posRot, bool) c in cluster)
        //        //    {
        //        //        List<pyramidCoord> clusterTilePyrCoord = generateSingleTilePyramidCoords(c.Item1.pos, c.Item1.rot);
        //        //
        //        //        foreach (pyramidCoord tilePyr in tile)
        //        //        {
        //        //            if (clusterTilePyrCoord.Exists(tilePyr))
        //        //            {
        //        //                b = true;
        //        //                //Debug.Log("tilePyr (" + tilePyr.pos.x + ", " + tilePyr.pos.y + ", " + tilePyr.pos.z + ")"); // ???
        //        //
        //        //                debgLstBlue.Add(new Vector3((float)tilePyr.pos.x, (float)tilePyr.pos.y, (float)tilePyr.pos.z));
        //        //            }
        //        //        }
        //        //    }
        //        //    return b;
        //        //}

        bool calculateOverlap(List<pyramidCoord> cluster, List<pyramidCoord> tile)
        {
            bool b = false;
            foreach (pyramidCoord t in tile)
            {
                foreach (pyramidCoord c in cluster)
                {
                    if (t.pos.x == c.pos.x && t.pos.y == c.pos.y && t.pos.z == c.pos.z)
                    {
                        if (t.pyramid == c.pyramid)
                        {
                            b = true;
                        }
                    }
                }
                //if (cluster.Exists(t)) // contains works with structs ??? // TEST...
                //{
                //    b = true;
                //}


            }
            return b;
        }

        int calculateNrPyramidsOverlap(List<pyramidCoord> cluster, List<pyramidCoord> tile)
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


        //        bool calculateOverlapSTrue(List<(pyramidCoord, bool)> cluster, List<pyramidCoord> tile)
        //        {
        //            bool b = false;
        //            foreach (pyramidCoord t in tile)
        //            {
        //                if (cluster.Exists((t, true))) // ???
        //                {
        //                    b = true;
        //                }
        //                //else
        //                //{
        //                //    //Debug.Log("overlap!");
        //                //}
        //            }
        //            return b;
        //        }

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

        //        List<pyramidCoord> pyramidsOverlapSTrueFalse(List<(pyramidCoord, bool)> pyramidSurround, List<pyramidCoord> newPyramids)
        //        {
        //            List<pyramidCoord> overlap = new List<pyramidCoord>();
        //            foreach ((pyramidCoord, bool) p in pyramidSurround)
        //            {
        //                if (newPyramids.Exists(p.Item1))
        //                {
        //                    overlap.Add(p.Item1);
        //                }
        //            }
        //            return overlap;
        //        }

        void calculateAllNeighborPyramids(List<pyramidCoord> pyramidCluster)
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
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(0, 0, 0)), 5)); // . (0,0,0) ERROR HERE ??? (was: 100)
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
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 5)); // -. // ERROR HERE was missing..._> added!!!

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
                    //if (pyramidSurround.Contains(new pyramidCoord(p.pos, (p.pyramid + i) % 6)) == false)
                    if (pyramidSurround.Exists(pyr => (int3equal(pyr.pos, p.pos) == true && pyr.pyramid == (p.pyramid + i) % 6)) == false &&
                        pyramidCluster.Exists(pyr => (int3equal(pyr.pos, p.pos) == true && pyr.pyramid == p.pyramid)) == false)
                    {
                        pyramidSurround.Add((new pyramidCoord(p.pos, (p.pyramid + i) % 6)));
                    }
                }
            }

            //Debug.Log("pyramidSurround Count: " + pyramidSurround.Count); // 103 OK (simple tile)

            //foreach (pyramidCoord p in pyramidSurround)
            //{
            //    //Debug.Log("(" + p.pos.x + ", " + p.pos.y + ", " + p.pos.z +"), " + p.pyramid);
            //    debgPyramidsGreen1.Add(p);
            //
            //    //debgLstGreen.Add(new Vector3((float)pyramidSurround[i].Item1.pos.x, (float)pyramidSurround[i].Item1.pos.y, (float)pyramidSurround[i].Item1.pos.z));
            //}
        }

        //        public IEnumerator coroutineTest() // funkt!!!
        //        {
        //            yield return "start";
        //            yield return new WaitForSeconds(1f);
        //            yield return "step 1";
        //            yield return new WaitForSeconds(1f);
        //            yield return "step 2";
        //            yield return new WaitForSeconds(1f);
        //            yield return "step 3";
        //            yield return new WaitForSeconds(1f);

        //            yield return "end";
        //        }


        //        // in Start: StartCoroutine(startUnifyCoroutine(calculateTilingRecursive(0)));

        //        public IEnumerator startUnifyCoroutine(IEnumerator target)
        //        {
        //            while (target.MoveNext())
        //            {
        //                if (target.Current?.GetType() == typeof(List<pyramidCoord>))
        //                {
        //                    pyramidSurroundForMesh = (List<pyramidCoord>)target.Current; // ???
        //                    //pyramidSurroundForMesh = target.Current;
        //                    generateSurroundMesh(); // !!!
        //                }

        //                yield return target.Current;
        //            }
        //        }



        //        // Update is called once per frame
        //        void Update()
        //        {
        //            if (onlyOnce == false)
        //            {
        //                onlyOnce = true;

        //                pyramids.Clear();
        //                firstTileInLayer.Clear();
        //                firstTileInLayer.Add(new tile(new int3(0, 0, 0), 0, this));

        //                getAllPyramids(); // pyramids (root tile)
        //                generateMesh(); // pyramids (root tile)
        //                mesh.SetVertices(vertices);
        //                mesh.SetTriangles(triangles, 0);
        //                mesh.RecalculateNormals();
        //                meshFilter.mesh = mesh;

        //                calculateAllNeighborPyramids(pyramids); // initialised with (pyramidCoord, false)

        //                Debug.Log("pyramidSurround count: " + pyramidSurround.Count);

        //                neighborTilePositionsWithNrFacesTouchingRootTile = calculateAllNeighborTilePositions(pyramids);

        //                Debug.Log("neighborTilePositions count: " + neighborTilePositionsWithNrFacesTouchingRootTile.Count);


        //            }
        //            else
        //            {
        //                // TEST -- first tile ----- OK
        //                surroundRootNode = new node(neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1);
        //                List<pyramidCoord> firstTilePyramids = generateSingleTilePyramidCoords(surroundRootNode.placement.pos, surroundRootNode.placement.rot);
        //                surroundRootNode.currentPyramids = firstTilePyramids;
        //                activeNode = surroundRootNode;

        //                // TEST recalculate possible tile positions // ERROR duplicate pos...-> ERROR in addSorted... !!!
        //                activeNode.possibleTilePositions = recalculatePossibleTilePositions(neighborTilePositionsWithNrFacesTouchingRootTile, firstTilePyramids);
        //                Debug.Log("possible tile positions after first tile: " + activeNode.possibleTilePositions.Count); // ERROR! count = 1 !!!


        //                // TODO: ALL NEW FROM SCRATCH !!!!!



        //                // TEST -- second tile  // 
        //                node secondNode = new node(activeNode.possibleTilePositions[(test + neighborTilePositionsWithNrFacesTouchingRootTile.Count) % neighborTilePositionsWithNrFacesTouchingRootTile.Count].Item1);

        //                secondNode.currentPyramids = activeNode.currentPyramids;
        //                secondNode.currentPyramids.AddRange(generateSingleTilePyramidCoords(secondNode.placement.pos, secondNode.placement.rot));

        //                activeNode.next = secondNode;
        //                activeNode = secondNode;
        //                // --------

        //                //tilingStep(); // ERROR: possible tile positions are wrong !!!


        //                surroundVertices.Clear();
        //                surroundTriangles.Clear();
        //                drawTilePyramidCount = 0;
        //                //drawTileFromSurround(test);
        //                int testmaxTriangleIndex = 0;

        //                for (int t = 0; t < surroundTriangles.Count; t++)
        //                {
        //                    if (surroundTriangles[t] > testmaxTriangleIndex)
        //                    {
        //                        testmaxTriangleIndex = surroundTriangles[t];
        //                    }
        //                }

        //                int testvertexCount = surroundVertices.Count;

        //                if (testmaxTriangleIndex < testvertexCount)
        //                {
        //                    surroundMesh.Clear();
        //                    surroundMesh.SetVertices(surroundVertices);
        //                    surroundMesh.SetTriangles(surroundTriangles, 0);
        //                    surroundMesh.RecalculateNormals();

        //                    surroundMeshFilter.mesh = surroundMesh;
        //                    //Debug.Log("new Mesh!");
        //                }

        //                redrawMesh = true;

        //                //----------------------------


        //                if (redrawMesh == true)
        //                {
        //                    generateSurroundMesh();
        //                    


        //                    if (drawBestTilingOrEveryTiling)
        //                    {
        //                        for (int t = 0; t < bestSurroundTriangles.Count; t++)
        //                        {
        //                            if (bestSurroundTriangles[t] > maxTriangleIndex)
        //                            {
        //                                maxTriangleIndex = bestSurroundTriangles[t];
        //                            }
        //                        }

        //                        int bestVertexCount = bestSurroundVertices.Count;

        //                        if (maxTriangleIndex < bestVertexCount)
        //                        {
        //                            surroundMesh.Clear();
        //                            surroundMesh.SetVertices(bestSurroundVertices);
        //                            surroundMesh.SetTriangles(bestSurroundTriangles, 0);
        //                            surroundMesh.RecalculateNormals();

        //                            surroundMeshFilter.mesh = surroundMesh;
        //                            //Debug.Log("new Mesh!");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        for (int t = 0; t < surroundTriangles.Count; t++)
        //                        {
        //                            if (surroundTriangles[t] > maxTriangleIndex)
        //                            {
        //                                maxTriangleIndex = surroundTriangles[t];
        //                            }
        //                        }

        //                        int vertexCount = surroundVertices.Count;

        //                        if (maxTriangleIndex < vertexCount)
        //                        {
        //                            surroundMesh.Clear();
        //                            surroundMesh.SetVertices(surroundVertices);
        //                            surroundMesh.SetTriangles(surroundTriangles, 0);
        //                            surroundMesh.RecalculateNormals();

        //                            surroundMeshFilter.mesh = surroundMesh;
        //                            //Debug.Log("new Mesh!");
        //                        }
        //                    }
        //                    redrawMesh = false;
        //                }

        //            }
        //        }


        //    }


    }

}