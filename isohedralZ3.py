from ortools.sat.python import cp_model
import sys
from collections import defaultdict
from typing import List, Set, Tuple
import itertools

Pyramid = Tuple[int, int, int]
Position = Tuple[int, int, int]

verticesPyramid0 = []
verticesPyramid0.append((0.5, -0.5, 0.5))
verticesPyramid0.append((0.5, 0.5, 0.5))
verticesPyramid0.append((0.5, 0.5, -0.5))
verticesPyramid0.append((0.5, -0.5, -0.5))
verticesPyramid0.append((0, 0, 0))

verticesPyramid1 = []
verticesPyramid1.append((-0.5, -0.5, -0.5))
verticesPyramid1.append((-0.5, 0.5, -0.5))
verticesPyramid1.append((-0.5, 0.5, 0.5))
verticesPyramid1.append((-0.5, -0.5, 0.5))
verticesPyramid1.append((0, 0, 0))

verticesPyramid2 = []
verticesPyramid2.append((0.5, 0.5, 0.5))
verticesPyramid2.append((-0.5, 0.5, 0.5))
verticesPyramid2.append((-0.5, 0.5, -0.5))
verticesPyramid2.append((0.5, 0.5, -0.5))
verticesPyramid2.append((0, 0, 0))

verticesPyramid3 = []
verticesPyramid3.append((0.5, -0.5, -0.5))
verticesPyramid3.append((-0.5, -0.5, -0.5))
verticesPyramid3.append((-0.5, -0.5, 0.5))
verticesPyramid3.append((0.5, -0.5, 0.5))
verticesPyramid3.append((0, 0, 0))

verticesPyramid4 = []
verticesPyramid4.append((0.5, 0.5, 0.5))
verticesPyramid4.append((-0.5, 0.5, 0.5))
verticesPyramid4.append((-0.5, -0.5, 0.5))
verticesPyramid4.append((0.5, -0.5, 0.5))
verticesPyramid4.append((0, 0, 0))

verticesPyramid5 = []
verticesPyramid5.append((0.5, 0.5, -0.5))
verticesPyramid5.append((0.5, -0.5, -0.5))
verticesPyramid5.append((-0.5, -0.5, -0.5))
verticesPyramid5.append((-0.5, 0.5, -0.5))
verticesPyramid5.append((0, 0, 0))

def generatePyrmaidVertices(p):
    vertices = []
    if p[1] == (1, 0, 0):
        for v in verticesPyramid0:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
    if p[1] == (-1, 0, 0):
        for v in verticesPyramid1:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
    if p[1] == (0, 1, 0):
        for v in verticesPyramid2:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
    if p[1] == (0, -1, 0):
        for v in verticesPyramid3:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
    if p[1] == (0, 0, 1):
        for v in verticesPyramid4:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
    if p[1] == (0, 0, -1):
        for v in verticesPyramid5:
            vertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))

    return vertices

def exportPyramids(pyramids, fileName):
    triangles = []
    allVertices = []
    pyramidCount = 0
    #print("exporting pyramids...")
    #print(f"pyramids: {pyramids}")

    for p in pyramids:
        #print(f"exporting pyramid {p}")
        #print(f"p[0]: {p[0]}")
        #print(f"p[1]: {p[1]}")
        if p[1] == (1, 0, 0):
            for v in verticesPyramid0:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (1, 0, 0)")
        if p[1] == (-1, 0, 0):
            for v in verticesPyramid1:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (-1, 0, 0)")
        if p[1] == (0, 1, 0):
            for v in verticesPyramid2:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (0, 1, 0)")
        if p[1] == (0, -1, 0):
            for v in verticesPyramid3:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (0, -1, 0)")
        if p[1] == (0, 0, 1):
            for v in verticesPyramid4:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (0, 0, 1)")
        if p[1] == (0, 0, -1):
            for v in verticesPyramid5:
                allVertices.append((v[0] + p[0][0], v[1] + p[0][1], v[2] + p[0][2]))
                #print(f"adding vertices (0, 0, -1)")
        
        if p[1] != (1, 0, 0) and p[1] != (-1, 0, 0) and p[1] != (0, 1, 0) and p[1] != (0, -1, 0) and p[1] != (0, 0, 1) and p[1] != (0, 0, -1):
            print(f"ERROR exporting pyramid: {p}")

        triangles.append((5 * pyramidCount + 0,
                          5 * pyramidCount + 1,
                          5 * pyramidCount + 4))

        triangles.append((5 * pyramidCount + 1,
                          5 * pyramidCount + 2,
                          5 * pyramidCount + 4))

        triangles.append((5 * pyramidCount + 2,
                          5 * pyramidCount + 3,
                          5 * pyramidCount + 4))

        triangles.append((5 * pyramidCount + 3,
                          5 * pyramidCount + 0,
                          5 * pyramidCount + 4))

        triangles.append((5 * pyramidCount + 0,
                          5 * pyramidCount + 2,
                          5 * pyramidCount + 1))

        triangles.append((5 * pyramidCount + 0,
                          5 * pyramidCount + 3,
                          5 * pyramidCount + 2))

        pyramidCount += 1

    #print("allVertices: ")
    #print(allVertices)


    with open("/home/j/Documents/SAT_solver/" + fileName +".obj", 'w') as f:
        # Write vertices
        for vertex in allVertices:
            f.write(f"v {vertex[0]} {vertex[1]} {vertex[2]}\n")
        
        # Write triangles
        for triangle in triangles:
            # Add 1 to each index since OBJ format is 1-based
            f.write(f"f {triangle[0]+1} {triangle[1]+1} {triangle[2]+1}\n")

    #print("exported pyramids to obj file")
        

def rotatePyramids(pyramids, axis, direction, center, basisVectors):
    newCoords = []
    #print(f"rotatePyramids: axis: {axis}, direction: {direction}, center: {center}")
    for (pos, pyr) in pyramids:
        #print(f"pyramid: {pyr}")
        if axis == 0:
            if direction:  # x -> x, y -> z, z -> -y
                newPyr = (pyr[0], -pyr[2], pyr[1])
                newPos = (pos[0], center[1] - (pos[2] - center[2]), center[2] + (pos[1] - center[1]))
            else:  # x -> x, y -> -z, z -> y
                newPyr = (pyr[0], pyr[2], -pyr[1])
                newPos = (pos[0], center[1] + (pos[2] - center[2]), center[2] - (pos[1] - center[1]))
        elif axis == 1:
            if direction:  # x -> -z, y -> y, z -> x
                newPyr = (pyr[2], pyr[1], -pyr[0])
                newPos = (center[0] + (pos[2] - center[2]), pos[1], center[2] - (pos[0] - center[0]))
            else:  # x -> z, y -> y, z -> -x
                newPyr = (-pyr[2], pyr[1], pyr[0])
                newPos = (center[0] - (pos[2] - center[2]), pos[1], center[2] + (pos[0] - center[0]))
        elif axis == 2:
            if direction:  # x -> y, y -> -x, z -> z
                newPyr = (-pyr[1], pyr[0], pyr[2])
                newPos = (center[0] - (pos[1] - center[1]), center[1] + (pos[0] - center[0]), pos[2])
            else:  # x -> -y, y -> x, z -> z
                newPyr = (pyr[1], -pyr[0], pyr[2])
                newPos = (center[0] + (pos[1] - center[1]), center[1] - (pos[0] - center[0]), pos[2])
        #print(f"new_pos: {newPos}, new_pyr: {newPyr}")
        newCoords.append((newPos, newPyr))

    newBasisVectors = basisVectors.copy()
    for i, b in enumerate(basisVectors):
        if axis == 0:
            if direction:  # x -> x, y -> z, z -> -y
                newBasisVectors[i] = (b[0], -b[2], b[1])
            else:  # x -> x, y -> -z, z -> y
                newBasisVectors[i] = (b[0], b[2], -b[1])

        elif axis == 1:
            if direction:  # x -> -z, y -> y, z -> x
                newBasisVectors[i] = (b[2], b[1], -b[0])
            else:  # x -> z, y -> y, z -> -x
                newBasisVectors[i] = (-b[2], b[1], b[0])

        elif axis == 2:
            if direction:  # x -> y, y -> -x, z -> z
                newBasisVectors[i] = (-b[1], b[0], b[2])
            else:  # x -> -y, y -> x, z -> z
                newBasisVectors[i] = (b[1], -b[0], b[2])

    #print(f"old basis vectors: {oldBasisVectors}")
    #print(f"new basis vectors: {newBasisVectors}")
    return (newCoords, newBasisVectors)

def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])

def transformPyramids(pyramids, startPyrCoord, endPyrCoord, rot):
    #print(f"in transformPyramids(): startPyrCoord: {startPyrCoord}")
    #print(f"                        endPyrCoord:   {endPyrCoord}")
    #print(f"                        rot: {rot}")
    (startPos, startPyr) = startPyrCoord
    (endPos, endPyr) = endPyrCoord

    #print(f"in transformPyramids(): pyramids: {pyramids}")

    basisVectors = [(1, 0, 0), (0, 1, 0), (0, 0, 1)]

    rotatedPyramids = []

    if startPyr == endPyr:
        #print("startPyr == endPyr")
        rotatedPyramids = pyramids
    elif startPyr == (-endPyr[0], -endPyr[1], -endPyr[2]):
        #print("startPyr == (-endPyr[0], -endPyr[1], -endPyr[2])")
        #print(f"endPos: {endPos}")
        if abs(startPyr[0]) == 1: 
            (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 1, True, startPos, basisVectors)
            (rotatedPyramids, basisVectors) = rotatePyramids(rotatedPyramids, 1, True, startPos, basisVectors)
        elif abs(startPyr[1]) == 1:
            (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 2, True, startPos, basisVectors)
            (rotatedPyramids, basisVectors) = rotatePyramids(rotatedPyramids, 2, True, startPos, basisVectors)
        elif abs(startPyr[2]) == 1:
            (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 0, True, startPos, basisVectors)
            (rotatedPyramids, basisVectors) = rotatePyramids(rotatedPyramids, 0, True, startPos, basisVectors)

    axis = cross(startPyr, endPyr)
    #print(f"axis: {axis}, startPyr: {startPyr}, endPyr: {endPyr}")
    
    if axis == (1, 0, 0):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 0, True, startPos, basisVectors)
    elif axis == (-1, 0, 0):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 0, False, startPos, basisVectors)
    elif axis == (0, 1, 0):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 1, True, startPos, basisVectors)
    elif axis == (0, -1, 0):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 1, False, startPos, basisVectors)
    elif axis == (0, 0, 1):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 2, True, startPos, basisVectors)
    elif axis == (0, 0, -1):
        (rotatedPyramids, basisVectors) = rotatePyramids(pyramids, 2, False, startPos, basisVectors)

    #print(f"rotatedPyramids: {rotatedPyramids}")


    transformedPyramids = []
    diff = (endPos[0] - startPos[0], endPos[1] - startPos[1], endPos[2] - startPos[2])
    #print(f"diff: {diff}")
    for (pos, pyr) in rotatedPyramids:
        newPos = (pos[0] + diff[0], pos[1] + diff[1], pos[2] + diff[2])
        transformedPyramids.append((newPos, pyr))

    #print(f"transformedPyramids (before 4-rotations): {transformedPyramids}")
    #(end_pos, end_pyr) = endPyr
    #print(f"endPyr: {endPyr}") #endPyr: (0, -1, 0)
    #print(f"endPos: {endPos}")
    
    if abs(endPyr[0]) == 1:
        for r in range(rot):
            (transformedPyramids, basisVectors) = rotatePyramids(transformedPyramids, 0, True, endPos, basisVectors)
    elif abs(endPyr[1]) == 1:
        for r in range(rot):
            (transformedPyramids, basisVectors) = rotatePyramids(transformedPyramids, 1, True, endPos, basisVectors)
    elif abs(endPyr[2]) == 1:
        for r in range(rot):
            (transformedPyramids, basisVectors) = rotatePyramids(transformedPyramids, 2, True, endPos, basisVectors)

    #print(f"startPyrCoord: {startPyrCoord}")
    #print(f"endPyrCoord: {endPyrCoord}")
    #print(f"axis: ({axis[0]}, {axis[1]}, {axis[2]})")
    #print(f"diff: ({diff[0]}, {diff[1]}, {diff[2]})")

    #print(f"transformedPyramids: {transformedPyramids}")

    return (transformedPyramids, basisVectors)

# Will be initialized after function definition
neighborPyramidsCase0 = [] 

def getAllNeighborPyramids(pyramids):
    neighborPyramids = set()
    candidates = set()
    for p in pyramids:
        (transformedPyramids, basisVectors) = transformPyramids(neighborPyramidsCase0, ((0, 0, 0), (1, 0, 0)), p, 0)
        candidates.update(transformedPyramids)

    
    #print(f"candidates: {candidates}")

    for c in candidates:
        if c not in pyramids:
            #print(f"adding candidate: {c}")
            neighborPyramids.add(c) # set -> no duplicates
    #print(f"nr neighborPyramids: {len(neighborPyramids)}")
    return neighborPyramids

def calculateNeighborPyramidsCase0():
    neighborPyramids = []
    tileVertices = verticesPyramid0
    allPyramids = []
    allPyramids.append((1, 0, 0))
    allPyramids.append((-1, 0, 0))
    allPyramids.append((0, 1, 0))
    allPyramids.append((0, -1, 0))
    allPyramids.append((0, 0, 1))
    allPyramids.append((0, 0, -1))


    for x in range(-1, 2):
        for y in range(-1, 2):
            for z in range(-1, 2):
                for p in allPyramids:
                    if x == 0 and y == 0 and z == 0 and p == (1, 0, 0):
                        continue
                    coord = ((x, y, z), p)
                    vertices = generatePyrmaidVertices(coord)
                    if any(v in tileVertices for v in vertices):
                        neighborPyramids.append(coord)
    return neighborPyramids

# Initialize the global variable
neighborPyramidsCase0 = calculateNeighborPyramidsCase0()

def getAllNeighborTilePositions(pyramids, neighborPyramids, surroundNr, forbiddenPyramids=None):
    neighborTilePositions = set()
    
    forbiddenSet = set(forbiddenPyramids) if forbiddenPyramids is not None else None

    #print(f"in getAllNeighborTilePositions(): pyramids: {pyramids}")
    #print(f"in getAllNeighborTilePositions(): neighborPyramids: {neighborPyramids}")

    for n in neighborPyramids:
        for p in pyramids:
            for r in range(4):
                (transformedPyramids, basisVectors) = transformPyramids(pyramids, p, n, r)

                if not any(pyr in neighborPyramids for pyr in transformedPyramids):
                    print(f"ERROR: in getAllNeighborTilePositions(): no overlap of tile position and neighborPyrmaids")
                    print(f"n: {n}")
                    print(f"p: {p}")
                    print(f"r: {r}")
                    #print(f"transformedPyramids: {transformedPyramids}")
                    if n in transformedPyramids:
                        print("n IS in transformedPyramids")
                    else:
                        print("n IS NOT in transformedPyramids")
                        # Find where p moved to
                        # It should be at n's position
    
                if not any(pyr in pyramids for pyr in transformedPyramids):
                    if forbiddenSet is None or not any(pyr in forbiddenSet for pyr in transformedPyramids):
                        # Calculate R(p)
                        bx, by, bz = basisVectors[0], basisVectors[1], basisVectors[2]
                        px, py, pz = p[0]
                        rot_px = px * bx[0] + py * by[0] + pz * bz[0]
                        rot_py = px * bx[1] + py * by[1] + pz * bz[1]
                        rot_pz = px * bx[2] + py * by[2] + pz * bz[2]
                        newPos = (n[0][0] - rot_px, n[0][1] - rot_py, n[0][2] - rot_pz, tuple(basisVectors))
                        neighborTilePositions.add(newPos)

    #print(f"neighborTilePositions[0]: {neighborTilePositions[0]}")
    #print(f"neighborTilePositions[1]: {neighborTilePositions[1]}")
    #print(f"neighborTilePositions[2]: {neighborTilePositions[2]}")
    #print(f"neighborTilePositions[3]: {neighborTilePositions[3]}")
    #print(f"neighborTilePositions[4]: {neighborTilePositions[4]}")
    #print(f"neighborTilePositions[5]: {neighborTilePositions[5]}")
    #print("neighborTilePositions[6]: ...")
    #print(f"Nr neighborTilePositions: {len(neighborTilePositions)}")
    # count = 0
    # for p in neighborTilePositions:
    #     if count < 10:
    #         count += 1
    #         print(f"neighborTilePosition S{surroundNr}: {p}")
    #     if count == 10:
    #         count += 1
    #         print(f"neighborTilePosition S{surroundNr}: ...")


    return list(neighborTilePositions)

def generatePyramidsFromTransform(pyramids, transX, transY, transZ, basisVectors):
    newPyramids = []

    #print(f"in generatePyramidsFromTransform(): pyramids: {pyramids}")
    #print(f"in generatePyramidsFromTransform(): x: {transX}, y: {transY}, z: {transZ}")
    #print(f"in generatePyramidsFromTransform(): basisVectors: {basisVectors}")

    bx, by, bz = basisVectors[0], basisVectors[1], basisVectors[2]

    for (pos, pyr) in pyramids:
        x, y, z = pos
        px, py, pz = pyr
        #print(f"pos: {pos}, pyr: {pyr}")

        new_x = x * bx[0] + y * by[0] + z * bz[0]
        new_y = x * bx[1] + y * by[1] + z * bz[1]
        new_z = x * bx[2] + y * by[2] + z * bz[2]

        new_px = px * bx[0] + py * by[0] + pz * bz[0]
        new_py = px * bx[1] + py * by[1] + pz * bz[1]
        new_pz = px * bx[2] + py * by[2] + pz * bz[2]

        new_pos = (transX + new_x, transY + new_y, transZ + new_z)
        new_pyr = (new_px, new_py, new_pz)

        newPyramids.append((new_pos, new_pyr))
        #print(f"new_pos: {new_pos}, new_pyr: {new_pyr}")

    return newPyramids

def get_face_neighbor_candidates(p):
    allPyramids = []
    allPyramids.append((1, 0, 0))
    allPyramids.append((-1, 0, 0))
    allPyramids.append((0, 1, 0))
    allPyramids.append((0, -1, 0))
    allPyramids.append((0, 0, 1))
    allPyramids.append((0, 0, -1))

    candidates = []
    print(f"in get_face_neighbor_candidates(): p: {p}")
    (pos, pyr) = p
    
    neg_pyr = tuple(-x for x in pyr)

    for op in allPyramids:
        if op != pyr and op != neg_pyr:
            candidates.append((pos, op))

    new_pos = (pos[0] + pyr[0], pos[1] + pyr[1], pos[2] + pyr[2])
    candidates.append((new_pos, neg_pyr))
    return candidates

class GenNode:
    def __init__(self, placement, ExistingPyramids=None):
        self.placement = placement
        if ExistingPyramids:
            self.pyramids = list(ExistingPyramids)
            if placement not in self.pyramids:
                print(f"adding placement to pyramids, placement. {placement}")
                self.pyramids.append(placement)
        else:
            print(f"setting pyramids to placement: {placement}")
            self.pyramids = [placement]
        self.face_neighbors = []

    def calculateFaceNeighbors(self):
        candidates = []
        for p in self.pyramids:
            print(f"in calculateFaceNeighbors(): p: {p}")
            candidates.extend(get_face_neighbor_candidates(p))

        self.face_neighbors = []
        pyr_set = set(self.pyramids)
        seen_candidates = set()

        for f in candidates:
            if f not in pyr_set and f not in seen_candidates:
                self.face_neighbors.append(f)
                seen_candidates.add(f)

    def detect_holes(self):
        pyr_set = set(self.pyramids)
        for f in self.face_neighbors:
            surround = get_face_neighbor_candidates(f)
            # Check if ALL surround are in pyr_set
            is_hole = True
            for s in surround:
                if s not in pyr_set:
                    is_hole = False
                    break
            if is_hole:
                return True
        return False

def cluster_is_new(nodesToCompareTo, newPyramidsToTest):
    # Test if new cluster is rotation of previous cluster
    clusterIsNew = True

    for node_to_compare in nodesToCompareTo:
        if len(node_to_compare.pyramids) != len(newPyramidsToTest):
            print("ERROR different count!!!")
            continue
    
    for nodeToCompareTo in nodesToCompareTo:
        for t in newPyramidsToTest:
            for r in range(4):            
                transformedNewPyramids, _ = transformPyramids(newPyramidsToTest, t, nodeToCompareTo.pyramids[0], r)
                
                difference = False
                if any(p not in nodeToCompareTo.pyramids for p in transformedNewPyramids):
                    difference = True

                if not difference:
                    clusterIsNew = False
                    break
            
            if not clusterIsNew:
                break
        
        if not clusterIsNew:
            break
        
    return clusterIsNew
            

            
        

def generate_polypyramids(n):
    """Generates poly-pyramids of size n using BFS."""
    print(f"Generating poly-pyramids of size {n}...")
    nodes = []
    
    # Layer 0 (Size 1)
    layer0 = []
    print("Generating root node...")
    rootPlacement = ((0, 0, 0), (1, 0, 0))
    #(testPos, testPyr) = rootPlacement
    #print(f"testPos: {testPos}")
    #print(f"testPyr: {testPyr}")
    root = GenNode(rootPlacement)
    #print(f"test root placement: {root.placement}")
    rootPos = root.placement[0]
    rootPyr = root.placement[1]

    #print(f"root placement pos: ({rootPos[0]}, {rootPos[1]}, {rootPos[2]})")
    #print(f"root placement pyr: ({rootPyr[0]}, {rootPyr[1]}, {rootPyr[2]})")
    #print(f"root pyramids: {root.pyramids}")
    root.calculateFaceNeighbors()
    layer0.append(root)
    nodes.append(layer0)
    
    # Generate layers up to n-1 (Size n)
    for layer in range(1, n):
        current_layer_nodes = []
        prev_layer_nodes = nodes[layer-1]
        
        for node in prev_layer_nodes:
            for p in node.face_neighbors:
                new_pyramids = list(node.pyramids)
                new_pyramids.append(p)
                
                if not current_layer_nodes:
                    print(f"creating node: placement: {p}, existingPyramids: {node.pyramids}, newLayer")
                    new_node = GenNode(p, node.pyramids)
                    new_node.calculateFaceNeighbors()
                    current_layer_nodes.append(new_node)
                else:
                    if cluster_is_new(current_layer_nodes, new_pyramids):
                        print(f"creating node: placement: {p}, existingPyramids: {node.pyramids}")
                        new_node = GenNode(p, node.pyramids)
                        new_node.calculateFaceNeighbors()
                        if not new_node.detect_holes():
                            current_layer_nodes.append(new_node)
                            
        nodes.append(current_layer_nodes)
        print(f"Layer {layer} (Size {layer+1}) generated {len(current_layer_nodes)} shapes.")
        
    return [n.pyramids for n in nodes[n-1]]


# --- Isohedral Number Solver ---

ROT_MAP_0_TRUE = {0: 0, 1: 1, 2: 5, 3: 4, 4: 2, 5: 3}
ROT_MAP_0_FALSE = {0: 0, 1: 1, 2: 4, 3: 5, 4: 3, 5: 2}
ROT_MAP_1_TRUE = {0: 4, 1: 5, 2: 2, 3: 3, 4: 1, 5: 0}
ROT_MAP_1_FALSE = {0: 5, 1: 4, 2: 2, 3: 3, 4: 0, 5: 1}
ROT_MAP_2_TRUE = {0: 3, 1: 2, 2: 0, 3: 1, 4: 4, 5: 5}
ROT_MAP_2_FALSE = {0: 2, 1: 3, 2: 1, 3: 0, 4: 4, 5: 5}

def rotate_pyramid_indices(pyr_coords, axis, direction, center=(0,0,0)):
    new_coords = []
    cx, cy, cz = center
    for x, y, z, p in pyr_coords:
        nx, ny, nz, np_ = 0, 0, 0, 0
        if axis == 0:
            if direction: # x -> x, y -> -z, z -> y
                np_ = ROT_MAP_0_TRUE[p]
                nx, ny, nz = x, cy + z - cz, cz - (y - cy)
            else:
                np_ = ROT_MAP_0_FALSE[p]
                nx, ny, nz = x, cy - (z - cz), cz + (y - cy)
        elif axis == 1:
            if direction: # x -> z, y -> y, z -> -x
                np_ = ROT_MAP_1_TRUE[p]
                nx, ny, nz = cx - (z - cz), y, cz + (x - cx)
            else:
                np_ = ROT_MAP_1_FALSE[p]
                nx, ny, nz = cx + (z - cz), y, cz - (x - cx)
        elif axis == 2:
            if direction: # x -> -y, y -> x, z -> z
                np_ = ROT_MAP_2_TRUE[p]
                nx, ny, nz = cx + (y - cy), cy - (x - cx), z
            else:
                np_ = ROT_MAP_2_FALSE[p]
                nx, ny, nz = cx - (y - cy), cy + (x - cx), z
        new_coords.append((nx, ny, nz, np_))
    return tuple(sorted(new_coords))

def reflect_pyramid_indices(pyr_coords):
    # Reflection across x=0 plane
    # (x, y, z, p) -> (-x, y, z, map(p))
    # Map: 0<->1, others same
    new_coords = []
    for x, y, z, p in pyr_coords:
        np_ = p
        if p == 0: np_ = 1
        elif p == 1: np_ = 0
        new_coords.append((-x, y, z, np_))
    return tuple(sorted(new_coords))

def get_all_orientations(base_shape):
    # Generate all 48 symmetries (24 rotations * 2 reflections)
    orientations = set()
    queue = [tuple(sorted(base_shape))]
    seen = set(queue)
    
    while queue:
        curr = queue.pop(0)
        orientations.add(curr)
        
        transforms = []
        # 3 axes, 2 directions for rotation
        for axis in range(3):
            transforms.append(rotate_pyramid_indices(curr, axis, True))
            transforms.append(rotate_pyramid_indices(curr, axis, False))
        # Reflection
        transforms.append(reflect_pyramid_indices(curr))
        
        for t in transforms:
            if t not in seen:
                seen.add(t)
                queue.append(t)
    return list(orientations)

def convert_to_indices(pyramids):
    res = []
    vec_map = {
        (1, 0, 0): 0, (-1, 0, 0): 1,
        (0, 1, 0): 2, (0, -1, 0): 3,
        (0, 0, 1): 4, (0, 0, -1): 5
    }
    for pos, vec in pyramids:
        res.append((pos[0], pos[1], pos[2], vec_map[vec]))
    return res

def exportPyramids(pyramids, fileName):
    triangles = []
    allVertices = []
    pyramidCount = 0
    
    vec_map_inv = {
        0: (1, 0, 0), 1: (-1, 0, 0),
        2: (0, 1, 0), 3: (0, -1, 0),
        4: (0, 0, 1), 5: (0, 0, -1)
    }

    for p in pyramids:
        if len(p) == 2 and isinstance(p[1], tuple):
             # Format: ((x,y,z), (vx,vy,vz))
             pos = p[0]
             vec = p[1]
        elif len(p) == 4: 
             # Format: (x, y, z, p_idx)
             pos = (p[0], p[1], p[2])
             vec = vec_map_inv[p[3]]
        else:
             continue

        current_vertices = []
        if vec == (1, 0, 0): current_vertices = verticesPyramid0
        elif vec == (-1, 0, 0): current_vertices = verticesPyramid1
        elif vec == (0, 1, 0): current_vertices = verticesPyramid2
        elif vec == (0, -1, 0): current_vertices = verticesPyramid3
        elif vec == (0, 0, 1): current_vertices = verticesPyramid4
        elif vec == (0, 0, -1): current_vertices = verticesPyramid5
        
        for v in current_vertices:
            allVertices.append((v[0] + pos[0], v[1] + pos[1], v[2] + pos[2]))

        triangles.append((5 * pyramidCount + 0, 5 * pyramidCount + 1, 5 * pyramidCount + 4))
        triangles.append((5 * pyramidCount + 1, 5 * pyramidCount + 2, 5 * pyramidCount + 4))
        triangles.append((5 * pyramidCount + 2, 5 * pyramidCount + 3, 5 * pyramidCount + 4))
        triangles.append((5 * pyramidCount + 3, 5 * pyramidCount + 0, 5 * pyramidCount + 4))
        triangles.append((5 * pyramidCount + 0, 5 * pyramidCount + 2, 5 * pyramidCount + 1))
        triangles.append((5 * pyramidCount + 0, 5 * pyramidCount + 3, 5 * pyramidCount + 2))
        
        pyramidCount += 1

    with open(fileName + ".obj", 'w') as f:
        for v in allVertices:
            f.write(f"v {v[0]} {v[1]} {v[2]}\n")
        for t in triangles:
            f.write(f"f {t[0]+1} {t[1]+1} {t[2]+1}\n")

def solve_tiling(shape_indices, num_tiles, box_dims, orientations=None, allowed_placements=None):
    L, M, N = box_dims
    model = cp_model.CpModel()
    
    # Generate all unique orientations
    if orientations is None:
        orientations = get_all_orientations(shape_indices)
    
    grid_cells = defaultdict(list) # (x,y,z,p) -> list of placement_vars
    placement_vars = {}
    
    for x in range(L):
        for y in range(M):
            for z in range(N):
                for rot_idx, shape in enumerate(orientations):
                    placement_key = ((x, y, z), rot_idx)
                    # Filter by allowed_placements if provided
                    if allowed_placements is not None:
                        if placement_key not in allowed_placements:
                            continue

                    p_var = model.NewBoolVar(f'P_{x}_{y}_{z}_{rot_idx}')
                    placement_vars[placement_key] = p_var
                    
                    for (sx, sy, sz, sp) in shape:
                        # Map to box coordinates
                        bx = (x + sx) % L
                        by = (y + sy) % M
                        bz = (z + sz) % N
                        grid_cells[(bx, by, bz, sp)].append(p_var)

    # Constraint: Each cell in the box must be covered by exactly one tile.
    for cell_coord in itertools.product(range(L), range(M), range(N), range(6)):
        # If a cell cannot be covered by any placement, sum will be 0, and
        # the constraint 0 == 1 will correctly make the model infeasible.
        model.Add(sum(grid_cells.get(cell_coord, [])) == 1)
            
    # Constraint: Exactly num_tiles placed
    model.Add(sum(placement_vars.values()) == num_tiles)
    
    solver = cp_model.CpSolver()
    solver.parameters.max_time_in_seconds = 10
    status = solver.Solve(model)
    
    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        solution_tiles = [key for key, var in placement_vars.items() if solver.Value(var)]
        return solution_tiles, orientations
    
    return None, None

def count_orbits(tiles, orientations, box_dims):
    # tiles: list of ((x,y,z), rot_idx)
    # box_dims: (L, M, N)
    L, M, N = box_dims
    
    # Represent tiles as sets of (x,y,z,p) in the periodic box for easy comparison
    tile_sets = []
    for (tx, ty, tz), rot_idx in tiles:
        cells = set()
        for sx, sy, sz, sp in orientations[rot_idx]:
            cells.add(((tx + sx) % L, (ty + sy) % M, (tz + sz) % N, sp))
        tile_sets.append(frozenset(cells))
    
    # Find equivalence classes
    # Two tiles T_i, T_j are equivalent if there exists a symmetry S of the grid (rot/ref + translation)
    # such that S(T_i) = T_j AND S({all_tiles}) = {all_tiles}
    
    # Symmetries of the grid on the torus:
    # 48 point symmetries * (L*M*N) translations
    
    # Precompute point symmetries of the grid cells
    # A symmetry maps (x,y,z,p) -> (x',y',z',p')
    
    # We can just iterate all symmetries and merge orbits
    parent = list(range(len(tiles)))
    def find(i):
        if parent[i] == i: return i
        parent[i] = find(parent[i])
        return parent[i]
    def union(i, j):
        root_i = find(i)
        root_j = find(j)
        if root_i != root_j:
            parent[root_i] = root_j

    # Generate all 48 point symmetries (as functions)
    # We can reuse get_all_orientations logic but applied to a single cell to get the map
    # Actually, simpler: Apply symmetry to the set of all tiles, check if it maps to itself.
    # If so, union the indices.
    
    # Optimization: Only check symmetries that map T_0 to T_k.
    
    all_tiles_set = set(tile_sets)
    
    # Iterate over all possible symmetries of the torus
    # Translations
    for dx in range(L):
        for dy in range(M):
            for dz in range(N):
                # Point symmetries
                # We need to apply rotation/reflection to the whole set of tiles
                # Since we have the orientations list, we know how shapes transform.
                # But we need to transform the centers too.
                
                # Let's just use the raw cell transformation
                # Transform a tile_set
                
                # To avoid iterating 48*L*M*N symmetries for every check,
                # we can pick T_0, and try to map it to T_k.
                # If we find a map S s.t. S(T_0) = T_k, we check if S(All) = All.
                # If yes, union(0, k).
                # Repeat for all representatives.
                pass

    # Brute force orbit counting (since N is small, usually < 10)
    # Iterate all symmetries S.
    #   Map all tiles.
    #   If mapped_tiles == current_tiles (as a set of sets):
    #       For each t_idx:
    #           Union(t_idx, mapped_t_idx)
    
    # Generating symmetries
    # Base: Identity
    # Generators: RotX, RotY, RotZ, RefX, TransX, TransY, TransZ
    
    # Construct list of all 48 point ops on (x,y,z,p)
    # Then combine with translations
    
    # Helper to apply point op to a set of cells
    def apply_point_op(cells, op_func):
        res = set()
        for x, y, z, p in cells:
            # op_func returns list of coords, but here we have single cell
            # We can use the rotate_pyramid_indices with single cell list
            # But that function sorts.
            # Let's just use a dummy list
            rx, ry, rz, rp = op_func([(x,y,z,p)])[0]
            res.add((rx % L, ry % M, rz % N, rp))
        return frozenset(res)

    point_ops = []
    # Identity
    point_ops.append(lambda c: c)
    # We need all 48.
    # We can generate them by composing basic ops.
    # But let's just use the fact that we can map the "canonical" shape to any of the 48 orientations.
    # A symmetry is defined by how it transforms the coordinate system.
    # It maps the standard basis to one of 48 frames.
    
    # Let's try a simpler approach for the user request:
    # Just check translations first (lattice tiling).
    # If N=1, orbit is 1.
    # If N>1, check if tiles are same orientation.
    
    # Full check:
    # 1. Generate all 48 transformed versions of tile_sets[0].
    # 2. For each transformed version, try to translate it to match tile_sets[k].
    # 3. If match found (S maps T_0 to T_k), check if S maps the whole tiling to itself.
    
    for i in range(len(tiles)):
        for j in range(i + 1, len(tiles)):
            if find(i) == find(j): continue
            
            # Try to find symmetry S s.t. S(T_i) = T_j
            # Iterate 48 point symmetries
            # For each, find translation that aligns center (or just one cell)
            
            # We need a robust way to generate the 48 symmetries of the torus grid.
            # Let's just iterate the 48 orientations of the shape.
            # If T_i is orientation A and T_j is orientation B.
            # The relative rotation is B * inv(A).
            # But we also have reflection.
            
            # Let's just iterate 48 ops on the cells.
            # We can generate them by BFS on the operations (RotX, RotY, RotZ, RefX)
            # applied to the set of cells of T_i.
            
            queue = [tile_sets[i]]
            visited_shapes = {tile_sets[i]: []} # shape -> list of ops (functions)
            # We need to store the op to apply it to other tiles
            
            # Actually, just pre-calculate the 48 symmetries of the grid coordinates
            # A symmetry is a function f(x,y,z,p) -> (x',y',z',p')
            pass

    # Simplified orbit counter:
    # Just check if we can map T_i to T_j by translation (Orbit under translation).
    # Then check if we can map the *set of translation orbits* to each other using point symmetries.
    
    # Step 1: Translation orbits
    # Two tiles are translationally equivalent if T_i = T_j + (dx, dy, dz)
    # Since we are in a periodic box, this means they have the same orientation and relative position matches.
    # Actually, if they have same orientation, they are translationally equivalent in the infinite tiling 
    # IF the tiling is periodic. In our box solution, they are just distinct tiles.
    # But "orbits of tiles in any tiling".
    # If we find a tiling with N tiles in box, and they are all same orientation, 
    # then they are all translationally equivalent (1 orbit).
    
    # So, group by orientation index.
    # Tiles with same orientation index are in same orbit (via translation).
    # Now we have K groups (distinct orientations).
    # Can we map one orientation to another via a symmetry that preserves the tiling?
    
    # For each pair of groups (O_a, O_b):
    #   Find point symmetry R that maps orientation O_a to O_b.
    #   Check if R(Tiling) = Tiling (modulo translation).
    #   i.e. for every tile T in Tiling, R(T) must overlap perfectly with some tile T' in Tiling (modulo translation).
    
    # Map rot_idx to list of tile indices
    by_orientation = defaultdict(list)
    for idx, t in enumerate(tiles):
        by_orientation[t[1]].append(idx)
        
    # Initial orbits: each orientation is a candidate orbit
    # (Assuming translations are always symmetries of the tiling, which is true for periodic)
    orbit_reps = list(by_orientation.keys())
    
    # Union-Find on orientations
    uf = {r: r for r in orbit_reps}
    def find_rep(r):
        if uf[r] == r: return r
        uf[r] = find_rep(uf[r])
        return uf[r]
    def union_rep(r1, r2):
        uf[find_rep(r1)] = find_rep(r2)
        
    # Check symmetries
    # We need the actual transformation matrices/functions for the 48 symmetries.
    # This is getting complicated to implement from scratch.
    # Heuristic: If we find a tiling with 1 orientation, return 1.
    # If we find multiple, return number of orientations (upper bound).
    # But we want "lowest number".
    
    # Let's assume if we find a tiling with N tiles, and they use K distinct orientations,
    # the isohedral number is likely 1 if K can be reduced, or K if not.
    # But usually, "Isohedral" means 1 orbit.
    # If we find a tiling with 1 orientation, it is 1-isohedral.
    
    return len(set(find_rep(r) for r in orbit_reps))

def get_grid_symmetries():
    # Generate all 48 symmetries of the grid (fixing origin)
    # A symmetry is a function that takes a list of cells and returns transformed cells
    
    ops = []
    ops.append(lambda c: rotate_pyramid_indices(c, 0, True)) # Rot X
    ops.append(lambda c: rotate_pyramid_indices(c, 1, True)) # Rot Y
    ops.append(lambda c: rotate_pyramid_indices(c, 2, True)) # Rot Z
    ops.append(lambda c: reflect_pyramid_indices(c))         # Ref X
    
    # Use a test shape to distinguish symmetries
    test_points = tuple(sorted([(1,0,0,0), (0,1,0,1), (0,0,1,2)]))
    
    symmetries = []
    seen = set()
    
    # Identity
    identity = lambda c: c
    symmetries.append(identity)
    seen.add(test_points)
    
    queue = [(identity, test_points)]
    
    while queue:
        curr_func, curr_img = queue.pop(0)
        for op in ops:
            # Compose: new_func(c) = op(curr_func(c))
            def make_comp(f, g):
                return lambda c: f(g(c))
            
            new_func = make_comp(op, curr_func)
            new_img = tuple(sorted(new_func(list(test_points))))
            
            if new_img not in seen:
                seen.add(new_img)
                symmetries.append(new_func)
                queue.append((new_func, new_img))
    return symmetries

GRID_SYMMETRIES = get_grid_symmetries()

def analyze_tiling_symmetry(tiles, orientations, box_dims):
    L, M, N = box_dims
    
    # Convert tiles to sets of cells for easy lookup
    tile_sets = []
    for (tx, ty, tz), rot_idx in tiles:
        cells = set()
        for sx, sy, sz, sp in orientations[rot_idx]:
            cells.add(((tx + sx) % L, (ty + sy) % M, (tz + sz) % N, sp))
        tile_sets.append(frozenset(cells))
        
    tile_map = {t: i for i, t in enumerate(tile_sets)}
    
    # Union-Find to merge equivalent tiles
    parent = list(range(len(tiles)))
    def find(i):
        if parent[i] == i: return i
        parent[i] = find(parent[i])
        return parent[i]
    def union(i, j):
        root_i = find(i)
        root_j = find(j)
        if root_i != root_j:
            parent[root_i] = root_j

    # Check all 48 point symmetries combined with all possible translations
    # To optimize, we pick tile 0 and try to map it to every other tile k.
    # If a symmetry S maps T0 -> Tk, we check if S preserves the whole tiling.
    
    t0_cells = list(tile_sets[0])
    
    # Iterate over all possible symmetries of the periodic box
    # A symmetry is (PointOp, Translation)
    # There are 48 PointOps * (L*M*N) Translations
    
    # To avoid iterating all L*M*N translations blindly, we derive the translation
    # by trying to map Tile 0 to every other Tile k.
    
    for point_sym in GRID_SYMMETRIES:
        # Apply point symmetry to Tile 0
        t0_transformed = point_sym(t0_cells)
        
        # Try to match transformed Tile 0 to every Tile k in the set
        for k in range(len(tiles)):
            # We need a translation (dx, dy, dz) such that point_sym(T0) + t = Tk
            # Pick the first cell of transformed T0 to calculate candidate translation
            src = t0_transformed[0]
            
            # Try to match with any cell in Tk that has the same pyramid index
            for dst in tile_sets[k]:
                if src[3] != dst[3]: continue
                
                dx = (dst[0] - src[0]) % L
                dy = (dst[1] - src[1]) % M
                dz = (dst[2] - src[2]) % N
                
                # Now check if this symmetry (point_sym + translation) maps the ENTIRE tiling to itself
                mapping = {} # Map from old_tile_idx -> new_tile_idx
                is_valid_symmetry = True
                
                for i in range(len(tiles)):
                    ti_trans = point_sym(list(tile_sets[i]))
                    shifted_set = set()
                    for (x, y, z, p) in ti_trans:
                        shifted_set.add(((x + dx) % L, (y + dy) % M, (z + dz) % N, p))
                    
                    shifted_frozenset = frozenset(shifted_set)
                    if shifted_frozenset in tile_map:
                        mapping[i] = tile_map[shifted_frozenset]
                    else:
                        is_valid_symmetry = False
                        break
                
                if is_valid_symmetry:
                    # If valid, merge all mapped pairs
                    for i, target in mapping.items():
                        union(i, target)
                    # Optimization: Once we found a valid symmetry mapping T0->Tk with this point_sym,
                    # we don't need to check other translations for the same T0->Tk pair 
                    # (though technically different translations could exist, usually one is enough to establish equivalence)
                    break

        # Generate the final orbit IDs for each tile
        orbit_reps = [find(i) for i in range(len(tiles))]
        # Normalize IDs so they are 0, 1, 2... instead of raw indices
        unique_reps = sorted(list(set(orbit_reps)))
        rep_to_id = {rep: i for i, rep in enumerate(unique_reps)}
        orbit_map = [rep_to_id[r] for r in orbit_reps]

    return len(unique_reps), orbit_map


def match_basis_to_orientation(basis, base_pyramids, orientations_list):
    # Transform base shape by basis at (0,0,0)
    transformed = generatePyramidsFromTransform(base_pyramids, 0, 0, 0, basis)
    indices = convert_to_indices(transformed)
    indices = tuple(sorted(indices))
    try:
        return orientations_list.index(indices)
    except ValueError:
        return -1

def export_tiling(tiles, orientations, filename, orbit_map):
    all_pyramids = []
    for i, ((tx, ty, tz), rot_idx) in enumerate(tiles):
        shape = orientations[rot_idx]
        tile_pyramids = []
        for sx, sy, sz, sp in shape:
            tile_pyramids.append((tx + sx, ty + sy, tz + sz, sp))

        exportPyramids(tile_pyramids, f"{filename}_tile_{i}_orbit_{orbit_map[i]}")
        all_pyramids.extend(tile_pyramids)

def solve_isohedral_number(shape_pyramids, shape_index=None):
    shape_indices = convert_to_indices(shape_pyramids)
    shape_size = len(shape_indices)
    
    orientations = get_all_orientations(shape_indices)
    neighbor_pyramids = getAllNeighborPyramids(shape_pyramids)

    print(f"  Shape size: {shape_size}")
    
    # Try to find tiling with k tiles in unit cell
    # We iterate k = 1, 2, ...
    # For each k, we need a box of volume k * shape_size.
    # Box volume V = 6 * L * M * N.
    # We need 6 * L * M * N = k * shape_size.
    
    for k in range(1, 10): # Check up to 9 tiles
        target_vol = k * shape_size
        if target_vol % 6 != 0:
            print(f"    Skipping k={k} (Volume {target_vol} is not a multiple of 6)")
            continue # Cannot be tiled by integer box
            
        box_vol_cells = target_vol // 6
        
        # Factorize box_vol_cells into L, M, N
        # Try to keep box somewhat cubic
        factors = []
        for L in range(1, box_vol_cells + 1):
            if box_vol_cells % L == 0:
                rem = box_vol_cells // L
                for M in range(1, rem + 1):
                    if rem % M == 0:
                        N = rem // M
                        factors.append((L, M, N))
        
        # Sort factors by "cubeness" (sum of dims)
        factors.sort(key=lambda t: sum(t))
        
        for dims in factors:
            # Initialize candidates for this search
            # We start with the identity tile at (0,0,0)
            start_basis = ((1,0,0), (0,1,0), (0,0,1))
            start_rot = match_basis_to_orientation(start_basis, shape_pyramids, orientations)
            
            # BFS State
            bfs_queue = [((0,0,0), start_basis)]
            visited_states = set()
            visited_states.add(((0,0,0), start_basis))
            
            candidates = set()
            candidates.add((0, 0, 0, start_rot))
            
            while True:
                # Map candidates to box coordinates
                box_candidates = set()
                L, M, N = dims
                for (tx, ty, tz, trot) in candidates:
                    box_candidates.add(((tx % L, ty % M, tz % N), trot))
                
                # print(f"    Checking k={k}, box={dims}, candidates={len(box_candidates)}...")
                tiles, _ = solve_tiling(shape_indices, k, dims, orientations, box_candidates)
                
                if tiles:
                    # Perform symmetry analysis to count actual orbits
                    num_orbits, orbit_map = analyze_tiling_symmetry(tiles, orientations, dims)
                    
                    print(f"  Found tiling with {k} tiles in {dims} box. Orbits: {num_orbits}")
                    
                    if shape_index is not None:
                        base_path = f"/home/j/Documents/SAT_solver/isohedral_solutions/solution_shape_{shape_index}_k{k}"
                        export_tiling(tiles, orientations, base_path, orbit_map)

                    if num_orbits == 1:
                        return 1 # Found a 1-isohedral tiling
                    return num_orbits # Return the number of orbits found (upper bound for isohedral number)
                
                # Check if box is saturated
                total_possible = L * M * N * len(orientations)
                if len(box_candidates) >= total_possible:
                    break # Box saturated, no solution

                # Expand candidates
                new_found = False
                count_to_process = len(bfs_queue)
                if count_to_process == 0:
                    break

                for _ in range(count_to_process):
                    curr_pos, curr_basis = bfs_queue.pop(0)
                    
                    # Reconstruct pyramids
                    # curr_pyramids = generatePyramidsFromTransform(shape_pyramids, curr_pos[0], curr_pos[1], curr_pos[2], curr_basis)
                    curr_neighbor_pyramids = generatePyramidsFromTransform(neighbor_pyramids, curr_pos[0], curr_pos[1], curr_pos[2], curr_basis)
                    
                    neighbors = getAllNeighborTilePositions(shape_pyramids, curr_neighbor_pyramids, 0)
                    
                    for nx, ny, nz, nbasis in neighbors:
                        if ((nx, ny, nz), nbasis) not in visited_states:
                            visited_states.add(((nx, ny, nz), nbasis))
                            bfs_queue.append(((nx, ny, nz), nbasis))
                            
                            nrot = match_basis_to_orientation(nbasis, shape_pyramids, orientations)
                            if nrot != -1:
                                candidates.add((nx, ny, nz, nrot))
                                new_found = True
                
                if not new_found:
                    break
                
                # print(f"      Expanded candidates to {len(candidates)} (Box: {len(box_candidates)})")
                
    return -1 # Not found

if __name__ == '__main__':
    log_file = open("isohedral_log.txt", "w")

    class Tee:
        def __init__(self, *files):
            self.files = files
        def write(self, obj):
            for f in self.files:
                f.write(obj)
                f.flush()
        def flush(self):
            for f in self.files:
                f.flush()

    sys.stdout = Tee(sys.stdout, log_file)

    for nrPyramids in range(2, 5):
        polypyramids = generate_polypyramids(nrPyramids)
        # for polypyramid in polypyramids:
        #     print(f"polypyramid: {polypyramid}")

        for shapeIndex, pyramids in enumerate(polypyramids):
            print(f"Checking Shape {shapeIndex}...")
            print(f"Pyramids: {pyramids}")
            
            #exportPyramids(pyramids, f"/home/j/Documents/SAT_solver/isohedral_solutions/shape_{shapeIndex}_base")
            indices = convert_to_indices(pyramids)
            orientations = get_all_orientations(indices)
            
            
            iso_num = solve_isohedral_number(pyramids, shape_index=shapeIndex)
            if iso_num == 1:
                print(f"Shape {shapeIndex} is Isohedral (1-anisohedral).")
            elif iso_num > 1:
                print(f"Shape {shapeIndex} has isohedral number <= {iso_num}.")
            else:
                print(f"Shape {shapeIndex}: No small periodic tiling found.")