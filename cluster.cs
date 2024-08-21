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

        public node next;


        public node(posRot pr)
        {
            placement = pr;
            //next = new List<node>();
        }

        public void addNode(posRot pr)
        {
            if (next == null)
            {
                next = new node(pr);
            }
            else
            {
                next.addNode(pr);
            }
        }

        public void addNode(node n)
        {
            if (next == null)
            {
                next = n;
            }
            else
            {
                next.addNode(n);
            }
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

        public List<posRot> neighborTilePositions;

        public List<Vector3> vertices;
        public List<int> triangles;

        public Mesh mesh;
        public MeshFilter meshFilter;

        public Mesh surroundMesh;
        public MeshFilter surroundMeshFilter;

        public GameObject surround;

        public List<Vector3> surroundVertices;
        public List<int> surroundTriangles;

        public Material material;

        //public int drawThisTileFromSurround;
        [HideInInspector]
        public bool onlyOnce;

        public node surroundRootNode;

        public node activeNode;

        public int step;

        public int pyramidsFilled;

        public int numPyramidsToFill;

        public List<Vector3> debgLstRed;
        public List<Vector3> debgLstGreen;
        public List<Vector3> debgLstBlue;

        public List<pyramidCoord> debgPyramidsGreen;
        public List<pyramidCoord> debgPyramidsRed;

        int drawTilePyramidCount;

        List<posRot> finalTiles;
        List<pyramidCoord> finalPyramids;

        public int test;
        public int test2;

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
            neighborTilePositions = new List<posRot>();
            //drawThisTileFromSurround = 0;

            onlyOnce = false;
            drawTilePyramidCount = 0;
            pyramidsFilled = 0;

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
            posRot neighborTilePosRot = neighborTilePositions[n % neighborTilePositions.Count];

            List<pyramidCoord> pyramids = generateSingleTilePyramidCoords(neighborTilePosRot.pos, neighborTilePosRot.rot);
            
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

        void getAllPyramids()
        {
            pyramids.AddRange(firstTileInLayer[0].pyramidCoords);
            foreach (tile n in firstTileInLayer[0].next)
            {
                pyramids.AddRange(n.pyramidCoords);
            }
        }


        void calculateAllNeighborTilePositions(List<pyramidCoord> pyramidCluster)
        {
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
            neighborTilePositions.Clear();

            //Debug.Log("neighborCubes.Count " + neighborCubes.Count); // 84 ((should be 42))
            foreach (int3 neighborCubePos in neighborCubes)
            {
                List<pyramidCoord> testTilePyramidCoords = new List<pyramidCoord>();
                for (int rot = 0; rot < 24 * 3; rot++) // TODO: *3 for large tile
                {
                    testTilePyramidCoords = generateSingleTilePyramidCoords(neighborCubePos, rot); // center = rot / 24
                    bool overlap = false;
                    foreach (pyramidCoord testPC in testTilePyramidCoords)
                    {
                        if (calculateOverlap(pyramids, testTilePyramidCoords))
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (overlap == false)
                    {
                        // at least one pyramid must touch cluster -> at least one overlap with pyramid surround
                        if (calculateOverlapSTrueFalse(pyramidSurround, testTilePyramidCoords)) // ERROR HERE !!!
                        {
                            neighborTilePositions.Add(new posRot(neighborCubePos, rot));
                            //Debug.Log("neighborTilePos: " + neighborCubePos.x + ", " + neighborCubePos.y + ", " + neighborCubePos.z + ", rot: " + rot);
                        }

                    }
                }
            }
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

        IEnumerator calculateTilingRecursive(List<posRot> currentTiles, List<pyramidCoord> currentPyramids, int stepCount)
        {
            //yield return currentPyramids;

            List<posRot> possibleNextPositions = new List<posRot>(neighborTilePositions); // copy of List of all neighborTilePositions

            foreach (posRot pr in currentTiles)
            {
                if (neighborTilePositions.Contains(pr))
                {
                    possibleNextPositions.Remove(pr);
                }
            }

            //yield return currentPyramids; //--> all tile positions

            foreach (posRot pr in possibleNextPositions)
            {
                if (!calculateOverlap(currentPyramids, generateSingleTilePyramidCoords(pr.pos, pr.rot)))
                {
                    currentTiles.Add(pr);

                    currentPyramids.AddRange(generateSingleTilePyramidCoords(pr.pos, pr.rot));

                    //yield return currentPyramids;

                    //StartCoroutine(calculateTilingRecursive(currentTiles, currentPyramids, stepCount + 1));

                    yield return startUnifyCoroutine(calculateTilingRecursive(currentTiles, currentPyramids, stepCount + 1));
                }
                //yield return currentPyramids; // --> all tile positions
            }
            yield return currentPyramids; // --> full cluster
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

        bool calculateOverlapSTrueFalse(List<(pyramidCoord, bool)> cluster, List<pyramidCoord> tile)
        {
            bool b = false;
            foreach (pyramidCoord t in tile)
            {
                if (cluster.Contains((t, true)) || cluster.Contains((t, false)))
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
                        pyramidSurround.Add((new pyramidCoord(p.pos, (p.pyramid + i) % 6), false));
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


        // in Start: StartCoroutine(startUnifyCoroutine(calculateTilingRecursive(currentTiles, currentPyramids, 0)));

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
                // ---- TEST ----
                //StartCoroutine(startUnifyCoroutine(coroutineTest()));

                // --------------

                onlyOnce = true;

                pyramids.Clear();
                firstTileInLayer.Clear();
                firstTileInLayer.Add(new tile(new int3(0, 0, 0), 0, this));
                
                getAllPyramids();
                
                calculateAllNeighborPyramids(pyramids);

                calculateAllNeighborTilePositions(pyramids);
                Debug.Log("neighbor tile positions: " + neighborTilePositions.Count);
                
                List<posRot> currentTiles = new List<posRot>();
                List<pyramidCoord> currentPyramids = new List<pyramidCoord>();

                StartCoroutine(startUnifyCoroutine(calculateTilingRecursive(currentTiles, currentPyramids, 0)));

                // async test
                //Task.Start(myTask);
                
                mesh.Clear(false);
                surroundVertices.Clear();
                surroundTriangles.Clear();
                                
                generateMesh();
                
                //generateSurroundMesh();
                
                Debug.Log("final tiles: ");
                foreach (posRot p in finalTiles)
                {
                    Debug.Log("pos: (" + p.pos.x + ", " + p.pos.y + ", " + p.pos.z + "), rot: " + p.rot);
                }
            }

            //if (myTask.IsCompleted)
            //{

                mesh.SetVertices(vertices);
                //mesh.SetUVs(0, UVs);
                //mesh.triangles = triangles.ToArray();

                mesh.SetTriangles(triangles, 0);

                //mesh.SetNormals(normals);
                mesh.RecalculateNormals();

                meshFilter.mesh = mesh;

                // validate triangles
                int maxTriangleIndex = 0;
                
                foreach (int t in triangles)
                {
                    if (t > maxTriangleIndex)
                    {
                        maxTriangleIndex = t;
                    }
                }
                
                int vertexCount = surroundVertices.Count;
                
                if (maxTriangleIndex < vertexCount)
                {
                    surroundMesh.SetVertices(surroundVertices);
                    surroundMesh.SetTriangles(surroundTriangles, 0);
                    surroundMesh.RecalculateNormals();

                    surroundMeshFilter.mesh = surroundMesh;
                }

            //}

        }
    }

}

//void calculateSurroundTiling(int neighborTileIndex) // off
//{
//    //Debug.Log("in calculateSurroundTiling(): neighborTilePos count: " + neighborTilePositions.Count); //
//
//    List<pyramidCoord> surroundPyramids = new List<pyramidCoord>();
//   
//    posRot firstTile = neighborTilePositions[neighborTileIndex];
//
//    //surroundRootNode = new node(firstTile);
//    addNodeFirstIn(firstTile);
//
//    drawTileFromSurround(neighborTileIndex);
//
//    markPyramidSurroundWithNewTile(firstTile); // OK
//
//    posRot secondTile = neighborTilePositions[test2];
//
//    bool overlapMarkedPyramidSurroundWithSecondTile = calculateOverlapSTrue(pyramidSurround, generateSingleTilePyramidCoords(secondTile.pos, secondTile.rot));
//    //Debug.Log("overlap second tile with marked pyramidSurround " + overlapMarkedPyramidSurroundWithSecondTile); // OK
//
//    drawTileFromSurround(test2);
//
//    if (!overlapMarkedPyramidSurroundWithSecondTile)
//    {
//        //surroundRootNode.next = new node(secondTile);
//        addNodeFirstIn(secondTile);
//        markPyramidSurroundWithNewTile(secondTile);
//    }
//}

//void tilingStep(int neighborPos)
//{
//    //steps -> called in update...
//
//    posRot pr = neighborTilePositions[neighborPos];
//
//    if (!calculateOverlapSTrue(pyramidSurround, generateSingleTilePyramidCoords(pr.pos, pr.rot)))
//    {
//        node n = new node(pr);
//        if (activeNode != null)
//        {
//            activeNode.addNode(n);
//            activeNode = n;
//        }
//        else
//        {
//            surroundRootNode = n;
//            activeNode = n;
//        }
//
//        markPyramidSurroundWithNewTile(pr);
//        //Debug.Log("tilingLoop tile: (" + pr.pos.x + ", " + pr.pos.y + ", " + pr.pos.z + "), rot: " + pr.rot);
//    }
//}

//void calculateTiling(int step)
//{
//    posRot currentPosRot;
//    if (activeNode.next != null)
//    {
//        currentPosRot = activeNode.next.placement;
//    }
//    else
//    {
//        currentPosRot = activeNode.placement;
//    }
//
//    List<posRot> possibleNextPositions = new List<posRot>();
//
//
//    if (!calculateOverlapSTrue(pyramidSurround, generateSingleTilePyramidCoords(neighborTilePositions[step].pos, neighborTilePositions[step].rot)))
//    {
//        possibleNextPositions.Add(neighborTilePositions[step]);
//    }
//}