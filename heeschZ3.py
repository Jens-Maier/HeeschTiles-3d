from ortools.sat.python import cp_model
import sys
from collections import defaultdict
from typing import List, Set, Tuple

# Type alias for a pyramid coordinate for better readability
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
    count = 0
    for p in neighborTilePositions:
        if count < 10:
            count += 1
            print(f"neighborTilePosition S{surroundNr}: {p}")
        if count == 10:
            count += 1
            print(f"neighborTilePosition S{surroundNr}: ...")


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
#
#def cluster_is_new(nodes_to_compare, new_pyramids):
#    # Test if new cluster is rotation of previous cluster
#    for node_to_compare in nodes_to_compare:
#        if len(node_to_compare.pyramids) != len(new_pyramids):
#            print("ERROR different count!!!")
#            continue
#            
#        # Try to map every pyramid in new_pyramids to the first pyramid of node_to_compare
#        anchor = node_to_compare.pyramids[0]
#        anchor_pos = (anchor[0], anchor[1], anchor[2])
#        
#        for t in new_pyramids:
#            transformed_new = transform_pyramids(new_pyramids, t, anchor)
#            
#            # 4 rotations around pyramid axis
#            axis = 0
#            if anchor[3] == 0 or anchor[3] == 1: axis = 0
#            elif anchor[3] == 2 or anchor[3] == 3: axis = 1
#            elif anchor[3] == 4 or anchor[3] == 5: axis = 2
#            
#            for r in range(4):
#                check_pyramids = transformed_new
#                if r == 1:
#                    check_pyramids = rotatePyramids(transformed_new, axis, True, anchor_pos)
#                elif r == 2:
#                    check_pyramids = rotatePyramids(rotatePyramids(transformed_new, axis, True, anchor_pos), axis, True, anchor_pos)
#                elif r == 3:
#                    check_pyramids = rotatePyramids(transformed_new, axis, False, anchor_pos)
#                
#                # Check equality
#                difference = False
#                compare_set = set(node_to_compare.pyramids)
#                for pt in check_pyramids:
#                    if pt not in compare_set:
#                        difference = True
#                        break
#                
#                if not difference:
#                    return False # Found a match, so it is NOT new
#                    
#    return True

def solve_monolithic(search_surrounds, basePyramids, shapeIndex, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, neighborTilePositionsS4, surroundPyramidsLayer1, surroundPyramidsLayer2, surroundPyramidsLayer3):
    model = cp_model.CpModel()

    baseSet = set(basePyramids)

    s1_placements = {}
    s2_placements = {}
    s3_placements = {}
    s4_placements = {}

    cell_covered_by_s1 = defaultdict(list)
    cell_covered_by_s2 = defaultdict(list)
    cell_covered_by_s3 = defaultdict(list)
    cell_covered_by_s4 = defaultdict(list)

    i = 0
    log = 10

    # S1 variables
    for pos in neighborTilePositionsS1:
        name = f's1_{pos}'.replace(' ', '')
        s1_var = model.NewBoolVar(name)
        if i < log:
            print(f"generated variable for S1: {name}")
            #print(f"pos[0]: {pos[0]}")
            #print(f"pos[1]: {pos[1]}")
            #print(f"pos[2]: {pos[2]}")
            #print(f"pos[3]: {pos[3]}")
            i += 1
        if i == log:
            print("...")
            i += 1
        s1_placements[pos] = s1_var

        

        cells = generatePyramidsFromTransform(basePyramids, pos[0], pos[1], pos[2], pos[3])
        for c in cells:
            cell_covered_by_s1[c].append(s1_var)

    i = 0
    log = 10
    if search_surrounds >= 2:
        # S2 variables
        for pos in neighborTilePositionsS2:
            name = f's2_{pos}'.replace(' ', '')
            s2_var = model.NewBoolVar(name)
            if i < log:
                print(f"generated variable for S2: {name}")
                i += 1
            if i == log:
                print("...")
                i += 1
            s2_placements[pos] = s2_var

            cells = generatePyramidsFromTransform(basePyramids, pos[0], pos[1], pos[2], pos[3])
            for c in cells:
                cell_covered_by_s2[c].append(s2_var)

    i = 0
    log = 10
    if search_surrounds >= 3:
        # S3 variables
        for pos in neighborTilePositionsS3:
            name = f's3_{pos}'.replace(' ', '')
            s3_var = model.NewBoolVar(name)
            if i < log:
                print(f"generated variable for S3: {name}")
                i += 1
            if i == log:
                print("...")
                i += 1
            s3_placements[pos] = s3_var

            cells = generatePyramidsFromTransform(basePyramids, pos[0], pos[1], pos[2], pos[3])
            for c in cells:
                cell_covered_by_s3[c].append(s3_var)

    i = 0
    log = 10
    if search_surrounds >= 4:
        # S4 variables
        for pos in neighborTilePositionsS4:
            name = f's4_{pos}'.replace(' ', '')
            s4_var = model.NewBoolVar(name)
            if i < log:
                print(f"generated variable for S4: {name}")
                i += 1
            if i == log:
                print("...")
                i += 1
            s4_placements[pos] = s4_var

            cells = generatePyramidsFromTransform(basePyramids, pos[0], pos[1], pos[2], pos[3])
            for c in cells:
                cell_covered_by_s4[c].append(s4_var)


    # --- Apply Hints from Previous Solution ---
    #
    # TODO...


    # Constraints
    all_cells = set(cell_covered_by_s1.keys())
    if search_surrounds >= 2:
        all_cells.update(cell_covered_by_s2.keys())
    if search_surrounds >= 3:
        all_cells.update(cell_covered_by_s3.keys())
    if search_surrounds >= 4:
        all_cells.update(cell_covered_by_s4.keys())

    # A. Disjountness
    # For every cell, sum(s1) + sum(s2) <= 1
    for c in all_cells:
        s1_cov = cell_covered_by_s1.get(c, [])
        s2_cov = cell_covered_by_s2.get(c, [])
        s3_cov = cell_covered_by_s3.get(c, [])
        s4_cov = cell_covered_by_s4.get(c, [])

        all_cov = s1_cov + s2_cov + s3_cov + s4_cov
        if len(all_cov) > 1:
            model.Add(sum(all_cov) <= 1)

    print("Generated disjountness constraints")

    # B. S1 Surrounds Center
    # All neighbors of Center must be covered by S1
    for p in surroundPyramidsLayer1:
        model.Add(sum(cell_covered_by_s1[p]) == 1)
    
    print("Generated surrounds center constraints.")

    # C. S2 Surrounds S1
    # Logic: If any neighbor of cell c is covered by S1, then c must be covered by (S1 or S2).
    # This forces S2 to fill all gaps around S1.
    if search_surrounds >= 2:
        s1_cover = set(cell_covered_by_s1.keys())
        s1_cover.update(getAllNeighborPyramids(s1_cover))

        for c in s1_cover:
            if c in baseSet: continue

            neighbors = getAllNeighborPyramids({c})

            neighbor_s1_vars = []
            for n in neighbors:
                neighbor_s1_vars.extend(cell_covered_by_s1.get(n, []))

            if not neighbor_s1_vars:
                continue
            
            # Optimization: Use Boolean Logic instead of Linear Arithmetic
            # Logic: If any neighbor is S1, then c must be covered by (S1 or S2).
            # Equivalent to: neighbor_is_s1 IMPLIES (c_is_s1 OR c_is_s2)
            target_literals = cell_covered_by_s1.get(c, []) + cell_covered_by_s2.get(c, [])
            for n_var in neighbor_s1_vars:
                model.AddBoolOr([n_var.Not()] + target_literals)

        
        print("Generated S2 surrounds S1 constraints.")

    # D. S3 Surrounds S2
    if search_surrounds >= 3:
        s2_cover = set(cell_covered_by_s2.keys())
        s2_cover.update(getAllNeighborPyramids(s2_cover))

        for c in s2_cover:
            if c in baseSet: continue

            neighbors = getAllNeighborPyramids({c})

            neighbor_s2_vars = []
            for n in neighbors:
                neighbor_s2_vars.extend(cell_covered_by_s2.get(n, []))

            if not neighbor_s2_vars:
                continue
            
            # Logic: If any neighbor is S2, then c must be covered by (S1 or S2 or S3).
            # Equivalent to: neighbor_is_s2 IMPLIES (c_is_s1 OR c_is_s2 OR c_is_s3)
            target_literals = cell_covered_by_s1.get(c, []) + cell_covered_by_s2.get(c, []) + cell_covered_by_s3.get(c, [])
            for n_var in neighbor_s2_vars:
                model.AddBoolOr([n_var.Not()] + target_literals)
        
        print("Generated S3 surrounds S2 constraints.")

    # E. S4 surrounds S3
    if search_surrounds >= 4:
        s3_cover = set(cell_covered_by_s3.keys())
        s3_cover.update(getAllNeighborPyramids(s3_cover))

        for c in s3_cover:
            if c in baseSet: continue

            neighbors = getAllNeighborPyramids({c})

            neighbor_s3_vars = []
            for n in neighbors:
                neighbor_s3_vars.extend(cell_covered_by_s3.get(n, []))

            if not neighbor_s3_vars:
                continue
            
            # Logic: If any neighbor is S3, then c must be covered by (S1 or S2 or S3 or S4).
            # Equivalent to: neighbor_is_s3 IMPLIES (c_is_s1 OR c_is_S2 OR c_is_s3 OR s_is_S4)
            target_literals = cell_covered_by_s1.get(c, []) + cell_covered_by_s2.get(c, []) + cell_covered_by_s3.get(c, []) + cell_covered_by_s4.get(c, [])
            for n_var in neighbor_s3_vars:
                model.AddBoolOr([n_var.Not()] + target_literals)
        
        print("Generated S4 surrounds S3 constraints.")

    
    print("generated model. Starting solver...")

    # Solve Model
    solver = cp_model.CpSolver()
    solver.parameters.num_search_workers = 8
    solver.parameters.max_time_in_seconds = 3600
    solver.parameters.log_search_progress = False

    print(f"Shape {shapeIndex}")

    print(f"Solving monolithic model for {search_surrounds} corona(s)...")
    status = solver.Solve(model)

    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print(f"Solver status: {solver.StatusName(status)}")
        print("Solution found!")
        s1_tiles = [key for key, value in s1_placements.items() if solver.Value(value)] # key: (x, y, z, basisVectors)
        print(f"S1: {s1_tiles}")
        with open("solutions.txt", "a") as f:
            f.write(f"S1: {s1_tiles}\n")
        for i, t in enumerate(s1_tiles):
            s1_solution_pyramids = set()
            s1_solution_pyramids.update(generatePyramidsFromTransform(basePyramids, t[0], t[1], t[2], t[3]))
            exportPyramids(s1_solution_pyramids, f"obj_solutions/shape{shapeIndex}_s1_solution_{i}")
        if search_surrounds >= 2:
            s2_tiles = [key for key, value in s2_placements.items() if solver.Value(value)]
            print("")
            print(f"S2: {s2_tiles}")
            with open("solutions.txt", "a") as f:
                f.write(f"S2: {s2_tiles}\n")
            for i, t in enumerate(s2_tiles):
                s2_solution_pyramids = set()
                s2_solution_pyramids.update(generatePyramidsFromTransform(basePyramids, t[0], t[1], t[2], t[3]))
                exportPyramids(s2_solution_pyramids, f"obj_solutions/shape{shapeIndex}_s2_solution_{i}")
            if search_surrounds >= 3:
                s3_tiles = [key for key, value in s3_placements.items() if solver.Value(value)]
                print("")
                print(f"S3: {s3_tiles}")
                with open("solutions.txt", "a") as f:
                    f.write(f"S3: {s3_tiles}\n")
                for i, t in enumerate(s3_tiles):
                    s3_solution_pyramids = set()
                    s3_solution_pyramids.update(generatePyramidsFromTransform(basePyramids, t[0], t[1], t[2], t[3]))
                    exportPyramids(s3_solution_pyramids, f"obj_solutions/shape{shapeIndex}_s3_solution_{i}")
                if search_surrounds >= 4:
                    s4_tiles = [key for key, value in s4_placements.items() if solver.Value(value)]
                    print("")
                    print(f"S4: {s4_tiles}")
                    with open("solutions.txt", "a") as f:
                        f.write(f"S4: {s4_tiles}\n")
                    for i, t in enumerate(s4_tiles):
                        s4_solution_pyramids = set()
                        s4_solution_pyramids.update(generatePyramidsFromTransform(basePyramids, t[0], t[1], t[2], t[3]))
                        exportPyramids(s4_solution_pyramids, f"obj_solutions/shape{shapeIndex}_s4_solution_{i}")





    elif status == cp_model.INFEASIBLE:
        print("No solution found: INFEASIBLE. The solver proved no solution exists within the constraints.")

    elif status == cp_model.UNKNOWN:
        print("No solution found: UNKNOWN. The solver reached the time limit (timeout) without finding a solution.")
    
    else:
        print(f"No solution found. Status: {status}")

    return status

if __name__ == '__main__':
    for nrPyramids in range(5, 6):
        polypyramids = generate_polypyramids(nrPyramids)
        for polypyramid in polypyramids:
            print(f"polypyramid: {polypyramid}")

        #pyrCoord_xp = ((0, 0, 0), (1, 0, 0))
        #pyrCoord_yp = ((0, 0, 0), (0, 1, 0))
        #pyrCoord_yn = ((0, 0, 0), (0, -1, 0))
        #pyrCoord_zp = ((0, 0, 0), (0, 0, 1))
        #pyrCoord_1xn = ((1, 0, 0), (-1, 0, 0))
        #pyrCoord_1zp = ((1, 0, 0), (0, 0, 1))
        #pyrCoord_1zn = ((1, 0, 0), (0, 0, -1)) # S1 solution found, S2 infeasible OK 

        #pyrCoord_xp = ((0, 0, 0), (1, 0, 0))
        #pyrCoord_nx = ((0, 0, 0), (-1, 0, 0))
        #pyrCoord_yp = ((0, 0, 0), (0, 1, 0))
        #pyrCoord_yn = ((0, 0, 0), (0, -1, 0))
        #pyrCoord_zp = ((0, 0, 0), (0, 0, 1))
        #pyrCoord_zn = ((0, 0, 0), (0, 0, -1)) # OK
        #pyrCoord_y1_yn = ((0, 1, 0), (0, -1, 0))


        #pyrCoord_xp = ((0, 0, 0), (1, 0, 0))
        #pyrCoord_nx = ((1, 0, 0), (-1, 0, 0)) # OK


        neighborPyramidsCase0 = calculateNeighborPyramidsCase0()
        print(f"Nr neighborPyramidsCase0: {len(neighborPyramidsCase0)}")

        #pyramids = [pyrCoord_xp, pyrCoord_yp, pyrCoord_zp, pyrCoord_yn, pyrCoord_y1_yn]
        #pyramids = [pyrCoord_xp, pyrCoord_yp, pyrCoord_yn, pyrCoord_zp, ((0, 1, 0), (0, -1, 0))]

        for shapeIndex, pyramids in enumerate(polypyramids):
            if shapeIndex < 15:# shape 13, 14 crashes
                continue
            print(f"polypyramid: {pyramids}")

            #pyramids = [pyrCoord_xp, pyrCoord_nx]#, pyrCoord_yp, pyrCoord_yn, pyrCoord_zp, pyrCoord_zn]
            #print(f"pyramids: {pyramids}")

            surroundPyramidsLayer1 = list(getAllNeighborPyramids(pyramids))
            surroundPyramidsLayer2 = list(getAllNeighborPyramids(surroundPyramidsLayer1))
            surroundPyramidsLayer3 = list(getAllNeighborPyramids(surroundPyramidsLayer2))


            #print(f"neighborPyramids: {neighborPyramids}")

            exportPyramids(pyramids, f"obj_solutions/shape{shapeIndex}_pyramids")
            #exportPyramids(neighborPyramids, "neighborPyramids") # OK

            neighborPyramidCoverage = [False] * len(surroundPyramidsLayer1)

            overlapError = False


            #------ S1 ------
            neighborTilePositionsS1 = getAllNeighborTilePositions(pyramids, surroundPyramidsLayer1, 1)

            #------ DEBUG -----
            neighborTilePyramidsS1 = set()
            for n in neighborTilePositionsS1:
                pyrs = generatePyramidsFromTransform(pyramids, n[0], n[1], n[2], n[3])
                if not any(p in surroundPyramidsLayer1 for p in pyrs):
                    print("ERROR: no overlap of tile position and neighborPyrmaids")
                    overlapError = True
                for p in pyrs:
                    if p in surroundPyramidsLayer1:
                        neighborPyramidCoverage[surroundPyramidsLayer1.index(p)] = True
                neighborTilePyramidsS1.update(pyrs)
            print(f"nr neighborTilePyramidsS1: {len(neighborTilePyramidsS1)}")
            for i, c in enumerate(neighborPyramidCoverage):
                if c == False:
                    print(f"ERROR: no overlap of tile position {i} and neighborPyrmaids")
            if not overlapError:
                print("OK: no overlap of any tile position and neighborPyrmaids")
            #-------------------

            pyramidsReachableByS1 = set()
            for pos in neighborTilePositionsS1:
                cells = generatePyramidsFromTransform(pyramids, pos[0], pos[1], pos[2], pos[3])
                pyramidsReachableByS1.update(cells)

            boundaryS1 = list(getAllNeighborPyramids(pyramidsReachableByS1))
            surroundPyramidsS1 = surroundPyramidsLayer1

            forbiddenForS2 = set(pyramids).union(surroundPyramidsLayer1)


            neighborTilePositionsS2 = []
            neighborTilePositionsS3 = []
            neighborTilePositionsS4 = []

            with open("solutions.txt", "a") as f:
                f.write(f"--- Tile Description for Shape {shapeIndex} ---\n")
                f.write(f"pyramids: {pyramids}\n")

            #---- solve monolithic ----
            search_surrounds = 1
            status = solve_monolithic(search_surrounds, pyramids, shapeIndex, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, neighborTilePositionsS4, surroundPyramidsLayer1, surroundPyramidsLayer2, surroundPyramidsLayer3)

            with open("solutions.txt", "a") as f:
                f.write(f"Solve status for S1: {status}\n")

            if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
                with open("solutions.txt", "a") as f:
                    f.write("Solution S1 found!\n")
                print("Solution S1 found, starting seach for S2")
                exportPyramids(surroundPyramidsS1, "obj_solutions/surroundPyramidsS1")

                search_surrounds = 2
                #------ S2 ------
                neighborTilePositionsS2 = getAllNeighborTilePositions(pyramids, boundaryS1, 2, forbiddenForS2)

                pyramidsReachableByS2 = set()
                for pos in neighborTilePositionsS2:
                    cells = generatePyramidsFromTransform(pyramids, pos[0], pos[1], pos[2], pos[3])
                    pyramidsReachableByS2.update(cells)

                boundaryS2 = list(getAllNeighborPyramids(pyramidsReachableByS2))
                surroundPyramidsS2 = surroundPyramidsLayer2

                forbiddenForS3 = set(forbiddenForS2).union(surroundPyramidsS2)

                with open("solutions.txt", "a") as f:
                    f.write(f"--- Tile Description for Shape {shapeIndex} ---\n")
                    f.write(f"pyramids: {pyramids}\n")
                status = solve_monolithic(search_surrounds, pyramids, shapeIndex, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, neighborTilePositionsS4, surroundPyramidsLayer1, surroundPyramidsLayer2, surroundPyramidsLayer3)

                with open("solutions.txt", "a") as f:
                    f.write(f"Solve status for S2: {status}\n")

                if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
                    with open("solutions.txt", "a") as f:
                        f.write("Solution S2 found!\n")
                    print("Solution S2 found, starting seach for S3")

                    exportPyramids(surroundPyramidsS2, "obj_solutions/surroundPyramidsS2")
                    search_surrounds = 3
                    #------ S3 ------
                    boundaryS3 = set(boundaryS2).union(pyramidsReachableByS2)
                    neighborTilePositionsS3 = getAllNeighborTilePositions(pyramids, boundaryS2, 3, forbiddenForS3)

                    pyramidsReachableByS3 = set()
                    for pos in neighborTilePositionsS3:
                        cels = generatePyramidsFromTransform(pyramids, pos[0], pos[1], pos[2], pos[3])
                        pyramidsReachableByS3.update(cels)

                    boundaryS3 = list(getAllNeighborPyramids(pyramidsReachableByS3))
                    surroundPyramidsS3 = surroundPyramidsLayer3

                    forbiddenForS4 = set(forbiddenForS3).union(surroundPyramidsS3)

                    with open("solutions.txt", "a") as f:
                        f.write(f"--- Tile Description for Shape {shapeIndex} ---\n")
                        f.write(f"pyramids: {pyramids}\n")
                    status = solve_monolithic(search_surrounds, pyramids, shapeIndex, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, neighborTilePositionsS4, surroundPyramidsLayer1, surroundPyramidsLayer2, surroundPyramidsLayer3)

                    with open("solutions.txt", "a") as f:
                        f.write(f"Solve status for S3: {status}\n")

                    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
                        with open("solutions.txt", "a") as f:
                            f.write("Solution S3 found!\n")
                        print("Solution S3 found, starting seach for S4")
                        exportPyramids(surroundPyramidsS3, "obj_solutions/surroundPyramidsS3")
                        search_surrounds = 4
                        #------ S4 ------
                        neighborTilePositionsS4 = getAllNeighborTilePositions(pyramids, boundaryS3, 4, forbiddenForS4)

                        with open("solutions.txt", "a") as f:
                            f.write(f"--- Tile Description for Shape {shapeIndex} ---\n")
                            f.write(f"pyramids: {pyramids}\n")
                        status = solve_monolithic(search_surrounds, pyramids, shapeIndex, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, neighborTilePositionsS4, surroundPyramidsLayer1, surroundPyramidsLayer2, surroundPyramidsLayer3)

                        with open("solutions.txt", "a") as f:
                            f.write(f"Solve status for S4: {status}\n")

                        if (status == cp_model.OPTIMAL or status == cp_model.FEASIBLE):
                            with open("solutions.txt", "a") as f:
                                f.write("Solution S4 found!\n")
                            print("Solution S4 found!")
                        else:
                            with open("solutions.txt", "a") as f:
                                f.write("No solution found for S4\n")
                            print("No solution found for S4")
                    else:
                        with open("solutions.txt", "a") as f:
                            f.write("No solution found for S3\n")
                        print("No solution found for S3")
                else:
                    with open("solutions.txt", "a") as f:
                        f.write("No solution found for S2\n")
                    print("No solution found for S2")
            else:
                with open("solutions.txt", "a") as f:
                    f.write("No solution found for S1\n")
                print("No solution found for S1")

                print("No solution found for S1")


            # ----- export solutions -----
            testNeighborTilesPyramidsS1 = []
            for i in range(0, len(neighborTilePositionsS1)):
                testNeighborTilesPyramidsS1.append(generatePyramidsFromTransform(pyramids, neighborTilePositionsS1[i][0], neighborTilePositionsS1[i][1], neighborTilePositionsS1[i] [2],     neighborTilePositionsS1[i][3]))

            for i, t in enumerate(testNeighborTilesPyramidsS1):
               exportPyramids(t, f"neighbors/S1/neighborS1_{i}")

            testNeighborTilesPyramidsS2 = []
            for i in range(0, len(neighborTilePositionsS2)):
                testNeighborTilesPyramidsS2.append(generatePyramidsFromTransform(pyramids, neighborTilePositionsS2[i][0], neighborTilePositionsS2[i][1], neighborTilePositionsS2[i] [2],     neighborTilePositionsS2[i][3]))

            for i, t in enumerate(testNeighborTilesPyramidsS2):
               exportPyramids(t, f"neighbors/S2/neighborS2_{i}")

            testNeighborTilesPyramidsS3 = []
            for i in range(0, len(neighborTilePositionsS3)):
                testNeighborTilesPyramidsS3.append(generatePyramidsFromTransform(pyramids, neighborTilePositionsS3[i][0], neighborTilePositionsS3[i][1], neighborTilePositionsS3[i] [2],     neighborTilePositionsS3[i][3]))

            for i, t in enumerate(testNeighborTilesPyramidsS3):
               exportPyramids(t, f"neighbors/S3/neighborS3_{i}")

    



    
