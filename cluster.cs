using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

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

    public class node
    {
        public posRot placement;

        public List<pyramidCoord> currentPyramids;

        public node next;

        public node parent;

        public List<(posRot, int)> possibleTilePositions; // int overlap

        public int currentNextIndex;

        public int pyramidsFilled;


        public node(posRot pr)
        {
            placement = pr;
            currentPyramids = new List<pyramidCoord>();
            possibleTilePositions = new List<(posRot, int)>();
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

        public List<(pyramidCoord, bool)> pyramidSurround;

        public List<pyramidCoord> pyramidSurroundForMesh;

        public List<(posRot, int)> neighborTilePositionsWithNrFacesTouchingRootTile;

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

        //public int drawThisTileFromSurround;
        [HideInInspector]
        public bool onlyOnce;

        public node surroundRootNode;

        public node activeNode;

        public int step;

        public int pyramidsFilled;

        public int numPyramidsToFill;
        public int maxPyramidsFilled; // 157 -> 160

        public List<Vector3> debgLstRed;
        public List<Vector3> debgLstGreen;
        public List<Vector3> debgLstBlue;

        public List<pyramidCoord> debgPyramidsGreen;
        public List<pyramidCoord> debgPyramidsRed;

        int drawTilePyramidCount;

        List<posRot> finalTiles;
        List<pyramidCoord> finalPyramids;
        [HideInInspector]
        public bool redrawMesh;

        public bool drawBestTilingOrEveryTiling;

        public int test;

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
            neighborTilePositionsWithNrFacesTouchingRootTile = new List<(posRot, int)>();

            redrawMesh = false;
            onlyOnce = false;
            drawTilePyramidCount = 0;
            pyramidsFilled = 0;
            maxPyramidsFilled = 0;

            step = 0;

            pyramidSurround = new List<(pyramidCoord, bool)>();
            pyramidSurroundForMesh = new List<pyramidCoord>();

            debgLstRed = new List<Vector3>();
            debgLstGreen = new List<Vector3>();
            debgLstBlue = new List<Vector3>();
            debgPyramidsRed = new List<pyramidCoord>();
            debgPyramidsGreen = new List<pyramidCoord>();

            finalTiles = new List<posRot>();
            finalPyramids = new List<pyramidCoord>();

            vertices = new List<Vector3>();
            triangles = new List<int>();
            surroundVertices = new List<Vector3>();
            surroundTriangles = new List<int>();

            bestSurroundVertices = new List<Vector3>();
            bestSurroundTriangles = new List<int>();


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

        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 p in debgLstBlue)
            {
                Gizmos.DrawSphere(p, 0.15f);
            }

            Gizmos.color = Color.green;
            //foreach (Vector3 p in debgLstGreen)
            //{
            //    Gizmos.DrawSphere(p, 0.2f);
            //}

            if (debgPyramidsGreen != null)
            {
                foreach (pyramidCoord p in debgPyramidsGreen)
                {
                    switch (p.pyramid)
                    {
                        case 0:
                            Gizmos.DrawSphere(new Vector3(p.pos.x + 0.25f, p.pos.y, p.pos.z), 0.1f);
                            break;
                        case 1:
                            Gizmos.DrawSphere(new Vector3(p.pos.x - 0.25f, p.pos.y, p.pos.z), 0.1f);
                            break;
                        case 2:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y + 0.25f, p.pos.z), 0.1f);
                            break;
                        case 3:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y - 0.25f, p.pos.z), 0.1f);
                            break;
                        case 4:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z + 0.25f), 0.1f);
                            break;
                        case 5:
                            Gizmos.DrawSphere(new Vector3(p.pos.x, p.pos.y, p.pos.z - 0.25f), 0.1f);
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
            //foreach (Vector3 p in debgLstRed)
            //{
            //    Gizmos.DrawSphere(p, 0.1f);
            //}
            if (debgPyramidsRed != null)
            {
                foreach (pyramidCoord p in debgPyramidsRed)
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
            int center = r / 24;

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
            //                0 1  


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
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 0));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 2));
            //tempPyramidCoords.Add(new pyramidCoord(add(pos, new int3(0, 0, 0)), 4));


            return createRotatedPyramidCoords(r % 24, tempPyramidCoords, pos);
        }

        List<pyramidCoord> createRotatedPyramidCoords(int r, List<pyramidCoord> tempPyramidCoords, int3 center)
        {
            List<pyramidCoord> returnPyramidCoords = new List<pyramidCoord>();
            switch (r)
            {
                case 0:
                    returnPyramidCoords.AddRange(tempPyramidCoords); //+y up
                    break;
                case 1:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 1, true, center));
                    break;
                case 2:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 1, true, center), 1, true, center));
                    break;
                case 3:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 1, false, center));
                    break;

                case 4:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 0, true, center)); // +z up
                    break;
                case 5:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center));
                    break;
                case 6:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, true, center), 1, true, center));
                    break;
                case 7:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 1, false, center));
                    break;

                case 8:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 2, false, center)); //+x up
                    break;
                case 9:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center));
                    break;
                case 10:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, true, center), 1, true, center));
                    break;
                case 11:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, false, center), 1, false, center));
                    break;

                case 12:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center)); //-y up
                    break;
                case 13:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center));
                    break;
                case 14:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, true, center), 1, true, center));
                    break;
                case 15:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, true, center), 0, true, center), 1, false, center));
                    break;

                case 16:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 0, false, center)); //-z up
                    break;
                case 17:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center));
                    break;
                case 18:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, true, center), 1, true, center));
                    break;
                case 19:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 0, false, center), 1, false, center));
                    break;

                case 20:
                    returnPyramidCoords.AddRange(rotatePyramids(tempPyramidCoords, 2, true, center)); //-x up
                    break;
                case 21:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center));
                    break;
                case 22:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, true, center), 1, true, center));
                    break;
                case 23:
                    returnPyramidCoords.AddRange(rotatePyramids(rotatePyramids(tempPyramidCoords, 2, true, center), 1, false, center));
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
        }

        void drawTileFromSurround(int n)
        {
            (posRot, int) neighborTilePosRot = neighborTilePositionsWithNrFacesTouchingRootTile[n % neighborTilePositionsWithNrFacesTouchingRootTile.Count];

            List<pyramidCoord> pyramids = generateSingleTilePyramidCoords(neighborTilePosRot.Item1.pos, neighborTilePosRot.Item1.rot);

            foreach (pyramidCoord p in pyramids)
            {
                surroundTriangles.Add(16 * drawTilePyramidCount + 0);
                surroundTriangles.Add(16 * drawTilePyramidCount + 1);
                surroundTriangles.Add(16 * drawTilePyramidCount + 2);

                surroundTriangles.Add(16 * drawTilePyramidCount + 3);
                surroundTriangles.Add(16 * drawTilePyramidCount + 4);
                surroundTriangles.Add(16 * drawTilePyramidCount + 5);

                surroundTriangles.Add(16 * drawTilePyramidCount + 6);
                surroundTriangles.Add(16 * drawTilePyramidCount + 7);
                surroundTriangles.Add(16 * drawTilePyramidCount + 8);

                surroundTriangles.Add(16 * drawTilePyramidCount + 9);
                surroundTriangles.Add(16 * drawTilePyramidCount + 10);
                surroundTriangles.Add(16 * drawTilePyramidCount + 11);

                surroundTriangles.Add(16 * drawTilePyramidCount + 12);
                surroundTriangles.Add(16 * drawTilePyramidCount + 14);
                surroundTriangles.Add(16 * drawTilePyramidCount + 13);

                surroundTriangles.Add(16 * drawTilePyramidCount + 12);
                surroundTriangles.Add(16 * drawTilePyramidCount + 15);
                surroundTriangles.Add(16 * drawTilePyramidCount + 14);

                drawTilePyramidCount += 1;

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
        }

        void generateSurroundMesh()
        {
            surroundTriangles.Clear();
            surroundVertices.Clear();
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
            pyramidSurroundForMesh.Clear();
        }

        void generateBestSurroundMesh()
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
        //
        List<(posRot, int)> calculateAllNeighborTilePositions(List<pyramidCoord> pyramidCluster)
        {
            Debug.Log("in calculateAllNeighborTilePositions() pyramidCluster count: " + pyramidCluster.Count);

            List<(posRot, int)> newNeighborTilePositions = new List<(posRot, int)>();
            List<int3> neighborCubes = new List<int3>(); // all possible neighbor cube positions 

            foreach (pyramidCoord p in pyramidCluster)
            {
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            //if (!(i == 0 && j == 0 && k == 0)) // for single cube // TODO : for 3 cubes...
                            //{
                            if (!neighborCubes.Contains(add(p.pos, new int3(i, j, k))))
                            {
                                neighborCubes.Add(add(p.pos, new int3(i, j, k)));

                                int3 newpos = add(p.pos, new int3(i, j, k));
                                //debgLstRed.Add(new Vector3((float)newpos.x, (float)newpos.y, (float)newpos.z));
                            }
                            //}
                        }
                    }
                }
            }

            //List<posRot> neighborTilePos = new List<posRot>(); // all possible neighbor tile positons and rotations around cluster -> moved to variables
            //neighborTilePositions.Clear();

            //Debug.Log("neighborCubes.Count " + neighborCubes.Count); // 84 ((should be 42))
            foreach (int3 neighborCubePos in neighborCubes)
            {
                List<pyramidCoord> testTilePyramidCoords = new List<pyramidCoord>();
                for (int rot = 0; rot < 24 * 3; rot++) // *3 for large tile
                {
                    testTilePyramidCoords = generateSingleTilePyramidCoords(neighborCubePos, rot); // center = rot / 24

                    int overlap = 0;

                    int nrFacesTouchRootTile = 0;

                    if (!calculateOverlap(pyramidCluster, testTilePyramidCoords)) // TEST...
                    {
                        foreach (pyramidCoord testPC in testTilePyramidCoords)
                        {
                            if (pyramidSurround.Contains((testPC, true)) || pyramidSurround.Contains((testPC, false))) // ???
                            {
                                overlap += 1;

                                // TODO: more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!

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

                                switch (testPC.pyramid)
                                {
                                    case 0:
                                        // 2, 3, 4, 5, x+1: 1
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x + 1, testPC.pos.y, testPC.pos.z), 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 1:
                                        // 2, 3, 4, 5, x-1: 0
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x - 1, testPC.pos.y, testPC.pos.z), 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 2:
                                        // 0, 1, 4, 5, y+1: 3
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y + 1, testPC.pos.z), 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 3:
                                        // 0, 1, 4, 5, y-1: 2
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y - 1, testPC.pos.z), 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 4:
                                        // 0, 1, 2, 3, z+1: 5
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z + 1), 5)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                    case 5:
                                        // 0, 1, 2, 3, z-1: 4
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 0)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 1)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 2)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(testPC.pos, 3)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        if (pyramidCluster.Contains(new pyramidCoord(new int3(testPC.pos.x, testPC.pos.y, testPC.pos.z - 1), 4)))
                                        {
                                            nrFacesTouchRootTile += 1;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    if (overlap > 0)
                    {
                        // at least one pyramid must touch cluster -> at least one overlap with pyramid surround
                        int nrOverlapPyramids = calculateNumberPyramidsOverlapSTrueFalse(pyramidSurround, testTilePyramidCoords);
                        if (nrOverlapPyramids > 0)
                        {
                            addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), nrFacesTouchRootTile); //   test only nr faces touch...TODO: with overlap...
                            //addSorted(newNeighborTilePositions, new posRot(neighborCubePos, rot), overlap); // overlap     // ^ test only nr faces touch...TODO: with overlap...

                            //newNeighborTilePositions.Add((new posRot(neighborCubePos, rot), 0));

                            //Debug.Log("neighborTilePos: " + neighborCubePos.x + ", " + neighborCubePos.y + ", " + neighborCubePos.z + ", rot: " + rot);
                        }

                    }
                }
            }
            // sort list by number overlap pyramids -> insert sorted !!!

            // TODO: more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!



            Debug.Log("in calculateAllNeighborTilePositions() returns newNeighborTilePositions.Count = " + newNeighborTilePositions.Count);

            //foreach ((posRot,int) pr in newNeighborTilePositions)
            //{
            //    Debug.Log("(" + pr.Item1.pos.x + ", " + pr.Item1.pos.y + ", " + pr.Item1.pos.z + ") | overlap: " + pr.Item2);
            //}

            return newNeighborTilePositions;
        }

        List<(posRot, int)> addSorted(List<(posRot, int)> neighborTilePositions, posRot newTile, int overlap)
        {
            if (neighborTilePositions.Count > 0)
            {
                if (overlap > neighborTilePositions[0].Item2)
                {
                    neighborTilePositions.Insert(0, (newTile, overlap));
                }
                else
                {
                    for (int i = 1; i < neighborTilePositions.Count; i++)
                    {
                        if (overlap > neighborTilePositions[i].Item2)
                        {
                            neighborTilePositions.Insert(i - 1, (newTile, overlap));
                            break;
                        }
                    }
                }
            }
            else
            {
                neighborTilePositions.Add((newTile, overlap));
            }

            return neighborTilePositions;
        }

        node addNodeFirstIn(posRot pr)
        {
            node newNode = new node(pr);
            newNode.next = surroundRootNode;
            surroundRootNode = newNode;
            return newNode;
        }

        void deleteFirstNode()
        {
            node next = surroundRootNode.next;
            surroundRootNode = next;
        }

        List<(posRot, int)> recalculatePossibleTilePositions(List<(posRot, int)> possibleTilePositions, List<pyramidCoord> newPyramids, posRot placement)
        {

            Debug.Log("old possibleTilePositions count " + possibleTilePositions.Count); // ERROR count = 0 !!!
            List<(posRot, int)> newPossibleTilePositions = new List<(posRot, int)>();
            foreach ((posRot, int) p in possibleTilePositions)
            {
                if ((!calculateOverlap(newPyramids, generateSingleTilePyramidCoords(p.Item1.pos, p.Item1.rot))) &&
                    (!(placement.pos.x == p.Item1.pos.x && placement.pos.y == p.Item1.pos.y && placement.pos.z == p.Item1.pos.z)))
                {
                    newPossibleTilePositions.Add(p);
                }
            }
            Debug.Log("recalculatePossibleTilePositions() positions count " + newPossibleTilePositions.Count); // ERROR count = 0 !!!
            return newPossibleTilePositions;
        }

        List<pyramidCoord> getAllFaceNeighborPyramids(pyramidCoord p)
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
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x + 1, p.pos.y, p.pos.z), 1));
                    break;
                case 1:
                    // 2, 3, 4, 5, x-1: 0
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x - 1, p.pos.y, p.pos.z), 0));
                    break;
                case 2:
                    // 0, 1, 4, 5, y+1: 3
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y + 1, p.pos.z), 3));
                    break;
                case 3:
                    // 0, 1, 4, 5, y-1: 2
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 4));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 5));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y - 1, p.pos.z), 2));
                    break;
                case 4:
                    // 0, 1, 2, 3, z+1: 5
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z + 1), 5));
                    break;
                case 5:
                    // 0, 1, 2, 3, z-1: 4
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 0));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 1));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 2));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z), 3));
                    faceNeighbors.Add(new pyramidCoord(new int3(p.pos.x, p.pos.y, p.pos.z - 1), 4));
                    break;
            }
            return faceNeighbors;
        }


        bool detectHoles(List<pyramidCoord> allCurrentPyramidsOfSurround)
        {
            bool b = true;
            foreach ((pyramidCoord, bool) p in pyramidSurround)
            {
                if (!allCurrentPyramidsOfSurround.Contains(p.Item1)) // p not in current -> p not yet filled
                {
                    List<pyramidCoord> neighborPyramids = getAllFaceNeighborPyramids(p.Item1);

                    // all neighbors of p are filled with current surround pyramids or rootTile
                    // -> at least one NOT filled with current surround pyramids and root tile
                    foreach (pyramidCoord faceNeighborOfP in neighborPyramids)
                    {
                        if (!firstTileInLayer[0].pyramidCoords.Contains(faceNeighborOfP) && !allCurrentPyramidsOfSurround.Contains(faceNeighborOfP))
                        {
                            b = false; // -> one neighbor path open -> no hole
                            return b;
                        }

                        //if (firstTileInLayer[0].Contains(faceNeighborOfP) || allCurrentPyramidsOfSurround.Contains(faceNeighborOfP))
                        //{
                        //    // -> this way is blocked
                        //}
                    }
                }
            }
            return b;
        }

        //copy List / clone List: new List<posRot>(oldList);

        void tilingStep()
        {
            Debug.Log("tilingStep()");

            // TODO: rate tile placements by # filled pyramids -> sort possibletilepositions

            node newNode;

            if (activeNode != null)
            {
                if (activeNode.possibleTilePositions.Count > 0 && activeNode.possibleTilePositions.Count > activeNode.currentNextIndex + 1)
                {
                    Debug.Log("next step:");

                    newNode = new node(activeNode.possibleTilePositions[activeNode.currentNextIndex].Item1);

                    //newNode.currentNextIndex = activeNode.currentNextIndex; // test moved up here

                    activeNode.currentNextIndex = (activeNode.currentNextIndex + 1) % activeNode.possibleTilePositions.Count;

                    List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);

                    List<pyramidCoord> newPyramidsFilled = pyramidsOverlapSTrueFalse(pyramidSurround, newPyramids);

                    List<pyramidCoord> allPyramids = new List<pyramidCoord>(activeNode.currentPyramids);
                    allPyramids.AddRange(newPyramids);

                    // TODO: detect holes !!!
                    //bool hole = detectHoles(allPyramids);

                    newNode.pyramidsFilled = activeNode.pyramidsFilled + newPyramidsFilled.Count;

                    //Debug.Log("number pyramids filled: " + newNode.pyramidsFilled);
                    pyramidsFilled = newNode.pyramidsFilled;

                    if (!drawBestTilingOrEveryTiling) // TODO: snap back to best tiling !
                    {
                        // redraw every step!
                        redrawMesh = true;
                        generateSurroundMesh(); // test
                    }
                    else
                    {
                        generateBestSurroundMesh();
                        //redrawMesh = true;
                        if (maxPyramidsFilled <= pyramidsFilled)
                        {
                            redrawMesh = true;
                            //generateBestSurroundMesh(); // test

                            maxPyramidsFilled = pyramidsFilled; // 157 -> 160 (sorted neighborTilePositions) -> 149 (sorted overlap) -> 154 ( test moved up here) -> 41 BROKEN !!!


                            Debug.Log(pyramidsFilled + " pyramids filled!"); // ERROR is called but mesh is not re-drawn !!!
                        }

                    }

                    //  more weight to neighborPyramids that share more faces -> sort by nr faces touch root tile !!!

                    if (newNode.pyramidsFilled == numPyramidsToFill)
                    {
                        Debug.Log("SUCCESS! Tiling found !!!");
                    }

                    //newNode.currentPyramids = new List<pyramidCoord>(activeNode.currentPyramids); //clone list /  copy list! //  new List<posRot>(oldList);
                    //newNode.currentPyramids.AddRange(newPyramids);
                    newNode.currentPyramids = allPyramids;
                    newNode.parent = activeNode;
                    activeNode.next = newNode;

                    newNode.possibleTilePositions = recalculatePossibleTilePositions(activeNode.possibleTilePositions, newPyramids, newNode.placement);

                    newNode.currentNextIndex = activeNode.currentNextIndex; // test moved up

                    // TODO: newNode.currentNextIndex = ??? // TODO !!!  new node(...) -> currentNextIndex set to 0 !!! -> set in recalculatePossibleTilePositions

                    activeNode = newNode;
                    Debug.Log("activeNode = newNode in tilingStep()");
                }
                else
                {
                    // TODO: backtrack ...
                    //Debug.Log("activeNode before backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), rot: " + activeNode.placement.rot);
                    //Debug.Log("activeNode before backtrack currentPyramids count: " + activeNode.currentPyramids.Count);

                    activeNode = activeNode.parent;
                    activeNode.next = null;

                    //activeNode.currentNextIndex = (activeNode.currentNextIndex + 1) % activeNode.possibleTilePositions.Count;

                    if (activeNode.possibleTilePositions.Count > activeNode.currentNextIndex + 1)
                    {
                        activeNode.currentNextIndex += 1; // TODO: recursive backtrack()...
                    }

                    //Debug.Log("backtrack");
                    //Debug.Log("activeNode after backtrack: (" + activeNode.placement.pos.x + ", " + activeNode.placement.pos.y + ", " + activeNode.placement.pos.z + "), rot: " + activeNode.placement.rot);
                    //Debug.Log("activeNode.next after backtrack: " + activeNode.next)

                    Debug.Log("activeNode after backtrack currentPyramids count: " + activeNode.currentPyramids.Count); // ERROR is same as before!
                }

            }
            else // first tile
            {
                Debug.Log("first tile!");
                Debug.Log("pyramids count: " + pyramids.Count); //19 OK
                neighborTilePositionsWithNrFacesTouchingRootTile = calculateAllNeighborTilePositions(pyramids);

                newNode = new node(neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1);

                Debug.Log("neighborTilePositions[0]: (" + neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1.pos.x + ", " + neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1.pos.y + ", " + neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1.pos.z + "), Rot: " + neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1.rot);

                List<pyramidCoord> newPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);

                newNode.currentPyramids = generateSingleTilePyramidCoords(newNode.placement.pos, newNode.placement.rot);
                List<(posRot, int)> newNodePossibleTilePositions = new List<(posRot, int)>();
                newNode.possibleTilePositions = recalculatePossibleTilePositions(newNodePossibleTilePositions, newPyramids, neighborTilePositionsWithNrFacesTouchingRootTile[0].Item1);
                List<pyramidCoord> rootAndFirstTilePyramids = new List<pyramidCoord>();

                rootAndFirstTilePyramids.AddRange(pyramids); // ERROR here ???
                rootAndFirstTilePyramids.AddRange(newPyramids);


                // ???
                newNode.possibleTilePositions = calculateAllNeighborTilePositions(rootAndFirstTilePyramids);
                //newNode.possibleTilePositions = recalculatePossibleTilePositions(activeNode.possibleTilePositions, newPyramids, newNode.placement);

                newNode.possibleTilePositions = recalculatePossibleTilePositions(newNode.possibleTilePositions, newPyramids, newNode.placement);

                activeNode = newNode;
                Debug.Log("activeNode = newNode in tilingStep() -> first tile");
                //newNode.possibleTilePositions = calculateAllNeighborTilePositions(newNode.currentPyramids);

                redrawMesh = true;
            }

        }

        // -----
        //public IEnumerator startUnifyCoroutine(IEnumerator target)
        //{
        //    while (target.MoveNext())
        //    {
        //        if (target.Current?.GetType() == typeof(List<pyramidCoord>))
        //        {
        //            pyramidSurroundForMesh = (List<pyramidCoord>)target.Current; // ???
        //            generateSurroundMesh(); // !!!
        //        }
        //
        //        yield return target.Current;
        //    }
        //}
        // -----



        void markPyramidSurroundWithNewTile(posRot newTile)
        {
            //Debug.Log("markPyramidSurroundWithNewNile() called!"); // called once!
            List<pyramidCoord> newTilePyramidCoords = generateSingleTilePyramidCoords(newTile.pos, newTile.rot);

            debgPyramidsGreen.Clear();

            //foreach ((pyramidCoord, bool) pyrbool in pyramidSurround)
            for (int i = 0; i < pyramidSurround.Count; i++)
            {
                if (newTilePyramidCoords.Contains(pyramidSurround[i].Item1))
                {
                    (pyramidCoord, bool) temp = pyramidSurround[i];
                    temp.Item2 = true;
                    pyramidsFilled += 1;
                    pyramidSurround[i] = temp;
                    //Debug.Log("markPyramidSurroundWithNewTile (" + pyramidSurround[i].Item1.pos.x + ", " + pyramidSurround[i].Item1.pos.y + ", " + pyramidSurround[i].Item1.pos.z + ")"); // 3 times executed! (???)

                    //debgLstGreen.Add(new Vector3((float)pyramidSurround[i].Item1.pos.x, (float)pyramidSurround[i].Item1.pos.y, (float)pyramidSurround[i].Item1.pos.z));

                    debgPyramidsGreen.Add(pyramidSurround[i].Item1);
                }
            }
        }

        //bool calculateOverlapWithMarkedTiles(List<(posRot, bool)> cluster, List<pyramidCoord> tile)
        //{
        //    bool b = false;
        //    
        //    foreach ((posRot, bool) c in cluster)
        //    {
        //        List<pyramidCoord> clusterTilePyrCoord = generateSingleTilePyramidCoords(c.Item1.pos, c.Item1.rot);
        //
        //        foreach (pyramidCoord tilePyr in tile)
        //        {
        //            if (clusterTilePyrCoord.Contains(tilePyr))
        //            {
        //                b = true;
        //                //Debug.Log("tilePyr (" + tilePyr.pos.x + ", " + tilePyr.pos.y + ", " + tilePyr.pos.z + ")"); // ???
        //
        //                debgLstBlue.Add(new Vector3((float)tilePyr.pos.x, (float)tilePyr.pos.y, (float)tilePyr.pos.z));
        //            }
        //        }
        //    }
        //    return b;
        //}

        bool calculateOverlap(List<pyramidCoord> cluster, List<pyramidCoord> tile)
        {
            bool b = false;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains(t))
                {
                    b = true;
                }
                //else
                //{
                //   //Debug.Log("overlap!");
                //}
            }
            return b;
        }


        bool calculateOverlapSTrue(List<(pyramidCoord, bool)> cluster, List<pyramidCoord> tile)
        {
            bool b = false;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains((t, true))) // ???
                {
                    b = true;
                }
                //else
                //{
                //    //Debug.Log("overlap!");
                //}
            }
            return b;
        }

        int calculateNumberPyramidsOverlapSTrueFalse(List<(pyramidCoord, bool)> cluster, List<pyramidCoord> tile)
        {
            int n = 0;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains((t, true)) || cluster.Contains((t, false)))
                {
                    n += 1;
                }
                //else
                //{
                //    //Debug.Log("overlap!");
                //}
            }
            return n;
        }

        List<pyramidCoord> pyramidsOverlapSTrueFalse(List<(pyramidCoord, bool)> pyramidSurround, List<pyramidCoord> newPyramids)
        {
            List<pyramidCoord> overlap = new List<pyramidCoord>();
            foreach ((pyramidCoord, bool) p in pyramidSurround)
            {
                if (newPyramids.Contains(p.Item1))
                {
                    overlap.Add(p.Item1);
                }
            }
            return overlap;
        }

        void calculateAllNeighborPyramids(List<pyramidCoord> pyramidCluster)
        {
            pyramidSurround.Clear();
            foreach (pyramidCoord p in pyramidCluster)
            {
                for (int i = 1; i < 6; i++)
                {
                    if (!pyramidSurround.Contains((new pyramidCoord(p.pos, (p.pyramid + i) % 6), false)) &&
                        (!pyramidCluster.Contains(new pyramidCoord(p.pos, (p.pyramid + i) % 6))))
                    {
                        pyramidSurround.Add((new pyramidCoord(p.pos, (p.pyramid + i) % 6), false)); // added double ... ???
                    }
                }

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
                                                                                                //------------------------------------------------------------------------------------
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 1)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 2)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 3)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 4)); // .
                neighborPyramidsCase0.Add(new pyramidCoord(add(p.pos, new int3(1, 0, 0)), 5)); // .

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

                switch (p.pyramid)
                {
                    case 0:
                        foreach (pyramidCoord pc in neighborPyramidsCase0)
                        {

                            //if ( !pyramidSurround.Contains(new pyramidCoord(p.pos, (p.pyramid + i) % 6)) && 
                            //      (!pyramidCluster.Contains(new pyramidCoord(p.pos, (p.pyramid + i) % 6))))
                            //{
                            //      pyramidSurround.Add(new pyramidCoord(p.pos, (p.pyramid + i) % 6));
                            //}

                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;
                    case 1: // use rotate...!
                        List<pyramidCoord> neighborPyramidsCase1 = rotatePyramids(rotatePyramids(neighborPyramidsCase0, 1, true, p.pos), 1, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase1)
                        {
                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;
                    case 2:
                        List<pyramidCoord> neighborPyramidsCase2 = rotatePyramids(neighborPyramidsCase0, 2, false, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase2)
                        {
                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;
                    case 3:
                        List<pyramidCoord> neighborPyramidsCase3 = rotatePyramids(neighborPyramidsCase0, 2, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase3)
                        {
                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;
                    case 4:
                        List<pyramidCoord> neighborPyramidsCase4 = rotatePyramids(neighborPyramidsCase0, 1, true, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase4)
                        {
                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;
                    case 5:
                        List<pyramidCoord> neighborPyramidsCase5 = rotatePyramids(neighborPyramidsCase0, 1, false, p.pos);
                        foreach (pyramidCoord pc in neighborPyramidsCase5)
                        {
                            if (!pyramidSurround.Contains((pc, false)) && (!pyramidCluster.Contains(pc)))
                            {
                                pyramidSurround.Add((pc, false));
                            }
                        }
                        break;

                }
            }
            foreach ((pyramidCoord, bool) p in pyramidSurround)
            {
                //Debug.Log("(" + p.pos.x + ", " + p.pos.y + ", " + p.pos.z +"), " + p.pyramid);
                debgPyramidsRed.Add(p.Item1);

                //debgLstGreen.Add(new Vector3((float)pyramidSurround[i].Item1.pos.x, (float)pyramidSurround[i].Item1.pos.y, (float)pyramidSurround[i].Item1.pos.z));
            }
            numPyramidsToFill = pyramidSurround.Count;
        }

        public IEnumerator coroutineTest() // funkt!!!
        {
            yield return "start";
            yield return new WaitForSeconds(1f);
            yield return "step 1";
            yield return new WaitForSeconds(1f);
            yield return "step 2";
            yield return new WaitForSeconds(1f);
            yield return "step 3";
            yield return new WaitForSeconds(1f);

            yield return "end";
        }


        // in Start: StartCoroutine(startUnifyCoroutine(calculateTilingRecursive(0)));

        public IEnumerator startUnifyCoroutine(IEnumerator target)
        {
            while (target.MoveNext())
            {
                if (target.Current?.GetType() == typeof(List<pyramidCoord>))
                {
                    pyramidSurroundForMesh = (List<pyramidCoord>)target.Current; // ???
                    //pyramidSurroundForMesh = target.Current;
                    generateSurroundMesh(); // !!!
                }

                yield return target.Current;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (onlyOnce == false)
            {
                onlyOnce = true;

                pyramids.Clear();
                firstTileInLayer.Clear();
                firstTileInLayer.Add(new tile(new int3(0, 0, 0), 0, this));

                getAllPyramids(); // pyramids (root tile)
                generateMesh(); // pyramids (root tile)
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                meshFilter.mesh = mesh;

                calculateAllNeighborPyramids(pyramids); // initialised with (pyramidCoord, false)

                Debug.Log("pyramidSurround count: " + pyramidSurround.Count);

                neighborTilePositionsWithNrFacesTouchingRootTile = calculateAllNeighborTilePositions(pyramids);

                Debug.Log("neighborTilePositions count: " + neighborTilePositionsWithNrFacesTouchingRootTile.Count);
            }
            else
            {
                // --- TEST visualise one neighbor tile
                //
                //posRot neighborTestPos = neighborTilePositionsWithNrFacesTouchingRootTile[test].Item1;
                //List<pyramidCoord> neighborTestPyramids = generateSingleTilePyramidCoords(neighborTestPos.pos, neighborTestPos.rot);
                //pyramidSurroundForMesh.Clear();
                //pyramidSurroundForMesh.AddRange(neighborTestPyramids);
                //
                //generateSurroundMesh(); // gets currentPyramids from activeNode into pyramidSurroundForMesh! pyramidSurroundForMesh.AddRange(activeNode.currentPyramids);
                //surroundMesh.SetVertices(surroundVertices);
                //surroundMesh.SetTriangles(surroundTriangles, 0);
                //surroundMesh.RecalculateNormals();
                //surroundMeshFilter.mesh = surroundMesh;
                //
                // ---

                tilingStep();

                if (redrawMesh == true)
                {
                    generateSurroundMesh();
                    // validate triangles
                    int maxTriangleIndex = 0;

                    for (int t = 0; t < surroundTriangles.Count; t++)
                    {
                        if (surroundTriangles[t] > maxTriangleIndex)
                        {
                            maxTriangleIndex = surroundTriangles[t];
                        }
                    }

                    int vertexCount = surroundVertices.Count;

                    if (maxTriangleIndex < vertexCount)
                    {
                        surroundMesh.SetVertices(surroundVertices);
                        surroundMesh.SetTriangles(surroundTriangles, 0);
                        surroundMesh.RecalculateNormals();

                        surroundMeshFilter.mesh = surroundMesh;
                        Debug.Log("new Mesh!");
                    }
                    redrawMesh = false;
                }

                

            }
        }


    }
}
