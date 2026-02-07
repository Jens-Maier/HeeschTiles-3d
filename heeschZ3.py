
# source .venv/bin/activate
# python3 heeschZ3.py


from ortools.sat.python import cp_model
from collections import defaultdict
from typing import List, Set, Tuple

# Type alias for a pyramid coordinate for better readability
PyramidCoord = Tuple[int, int, int, int]
Position = Tuple[int, int, int]

# Pyramid rotation mappings
ROT_MAP_0_TRUE = {0: 0, 1: 1, 2: 5, 3: 4, 4: 2, 5: 3}
ROT_MAP_0_FALSE = {0: 0, 1: 1, 2: 4, 3: 5, 4: 3, 5: 2}
ROT_MAP_1_TRUE = {0: 4, 1: 5, 2: 2, 3: 3, 4: 1, 5: 0}
ROT_MAP_1_FALSE = {0: 5, 1: 4, 2: 2, 3: 3, 4: 0, 5: 1}
ROT_MAP_2_TRUE = {0: 3, 1: 2, 2: 0, 3: 1, 4: 4, 5: 5}
ROT_MAP_2_FALSE = {0: 2, 1: 3, 2: 1, 3: 0, 4: 4, 5: 5}

def generateTilePyramids(x, y, z, rot):
    """
    Generates pyramid coordinates for a tile at (x,y,z) with rotation 'rot'.
    """
    # These are the local coordinates of the pyramids for a tile at origin in default orientation
    # tile description
    base_pyramids = [
        (0, 0, -1, 4),

        ( 0,  0, 0, 1),
        ( 0,  0, 0, 3),
        ( 0,  0, 0, 4),
        ( 0,  0, 0, 5),
        (-1,  0, 0, 0),
        ( 0, -1, 0, 2),

        ( 0,  0, 1, 1),
        ( 0,  0, 1, 3),
        ( 0,  0, 1, 4),
        ( 0,  0, 1, 5),
        (-1,  0, 1, 0),
        ( 0, -1, 1, 2),
        
        ( 0,  0, 2, 1),
        ( 0,  0, 2, 3),
        ( 0,  0, 2, 4),
        ( 0,  0, 2, 5),
        (-1,  0, 2, 0),
        ( 0, -1, 2, 2),
    ]
    
    # (0, 0, 0, 0),
    # (0, 0, 0, 1),
    # (0, 0, 0, 2),
    # (0, 0, 0, 4),
    # (0, 0, 0, 5),
    # (0, 1, 0, 3),


    #base_pyramids = [
    #    (0, 0, 0, 0),
    #    #(0, 0, 0, 1),
    #    #(0, 0, 0, 2),
    #    (0, 0, 0, 3),
    #    (0, 0, 0, 4),
    #    #(0, 0, 0, 5),
    #]
    #base_pyramids = [
    #    (0, 0, 0, 0),
    #    (0, 0, 0, 3),
    #    (0, 0, 0, 1),
    #    (1, 0, 0, 0),
    #    (1, 0, 0, 3),
    #    (1, 0, 0, 1),
    #]


    # NOTE: The center of rotation for the base tile is assumed to be (0,0,0).
    # This might need to be adjusted depending on the tile's geometry.
    # A potential center based on the coordinates could be (-0.5, -0.5, 0.5).
    center = (0, 0, 0)
    rotated_pyramids = create_rotated_pyramid_coords(rot, base_pyramids, center)

    final_pyramids = []
    for px, py, pz, p_type in rotated_pyramids:
        final_pyramids.append((px + x, py + y, pz + z, p_type))
    return final_pyramids

def rotatePyramids(
    pyr_coords: List[PyramidCoord], 
    axis: int, 
    direction: bool, 
    center: Position
) -> List[PyramidCoord]:
    """
    Rotates a list of pyramid coordinates around a center point.

    Args:
        pyr_coords: A list of pyramid coordinates.
        axis: The axis of rotation (0 for x, 1 for y, 2 for z).
        direction: The direction of rotation (True for positive, False for negative).
        center: The center of rotation as a tuple (x, y, z).

    Returns:
        A new list of rotated pyramid coordinates.
    """
    new_coords = []
    center_x, center_y, center_z = center

    for p_x, p_y, p_z, p_pyr in pyr_coords:
        new_pyr, new_x, new_y, new_z = -1, -1, -1, -1

        if axis == 0:
            if direction:  # x -> x, y -> -z, z -> y
                new_pyr = ROT_MAP_0_TRUE[p_pyr]
                new_x = p_x
                new_y = center_y + p_z - center_z
                new_z = center_z - (p_y - center_y)
            else:  # x -> x, y -> z, z -> -y
                new_pyr = ROT_MAP_0_FALSE[p_pyr]
                new_x = p_x
                new_y = center_y - (p_z - center_z)
                new_z = center_z + (p_y - center_y)
        elif axis == 1:
            if direction:  # x -> z, y -> y, z -> -x
                new_pyr = ROT_MAP_1_TRUE[p_pyr]
                new_x = center_x - (p_z - center_z)
                new_y = p_y
                new_z = center_z + (p_x - center_x)
            else:  # x -> -z, y -> y, z -> x
                new_pyr = ROT_MAP_1_FALSE[p_pyr]
                new_x = center_x + (p_z - center_z)
                new_y = p_y
                new_z = center_z - (p_x - center_x)
        elif axis == 2:
            if direction:  # x -> -y, y -> x, z -> z
                new_pyr = ROT_MAP_2_TRUE[p_pyr]
                new_x = center_x + (p_y - center_y)
                new_y = center_y - (p_x - center_x)
                new_z = p_z
            else:  # x -> y, y -> -x, z -> z
                new_pyr = ROT_MAP_2_FALSE[p_pyr]
                new_x = center_x - (p_y - center_y)
                new_y = center_y + (p_x - center_x)
                new_z = p_z
        
        new_coords.append((new_x, new_y, new_z, new_pyr))

    return new_coords

def calculate_all_neighbor_pyramids(pyramid_cluster: Set[PyramidCoord]) -> Set[PyramidCoord]:
    """
    Calculates all neighbor pyramids for a given cluster of pyramids.

    Args:
        pyramid_cluster: A set of pyramid coordinates.

    Returns:
        A set of neighbor pyramid coordinates.
    """
    pyramid_surround: Set[PyramidCoord] = set()

    base_neighbors_case0 = [
        (0, 1, 0, 0), (0, 1, 0, 3), (0, 1, 0, 4), (0, 1, 0, 5),
        (0, 1, 1, 0), (0, 1, 1, 3), (0, 1, 1, 5),
        (0, 0, 1, 0), (0, 0, 1, 2), (0, 0, 1, 3), (0, 0, 1, 5),
        (0, -1, 1, 0), (0, -1, 1, 2), (0, -1, 1, 5),
        (0, -1, 0, 0), (0, -1, 0, 2), (0, -1, 0, 4), (0, -1, 0, 5),
        (0, -1, -1, 0), (0, -1, -1, 2), (0, -1, -1, 4),
        (0, 0, -1, 0), (0, 0, -1, 2), (0, 0, -1, 3), (0, 0, -1, 4),
        (0, 1, -1, 0), (0, 1, -1, 3), (0, 1, -1, 4),
        (0, 0, 0, 1), (0, 0, 0, 2), (0, 0, 0, 3), (0, 0, 0, 4), (0, 0, 0, 5),
        (1, 1, 0, 1), (1, 1, 0, 3), (1, 1, 0, 4), (1, 1, 0, 5),
        (1, 1, 1, 1), (1, 1, 1, 3), (1, 1, 1, 5),
        (1, 0, 1, 1), (1, 0, 1, 2), (1, 0, 1, 3), (1, 0, 1, 5),
        (1, -1, 1, 1), (1, -1, 1, 2), (1, -1, 1, 5),
        (1, -1, 0, 1), (1, -1, 0, 2), (1, -1, 0, 4), (1, -1, 0, 5),
        (1, -1, -1, 1), (1, -1, -1, 2), (1, -1, -1, 4),
        (1, 0, -1, 1), (1, 0, -1, 2), (1, 0, -1, 3), (1, 0, -1, 4),
        (1, 1, -1, 1), (1, 1, -1, 3), (1, 1, -1, 4),
        (1, 0, 0, 1), (1, 0, 0, 2), (1, 0, 0, 3), (1, 0, 0, 4), (1, 0, 0, 5),
    ]

    for p_x, p_y, p_z, p_pyr in pyramid_cluster:
        p_pos = (p_x, p_y, p_z)
        
        neighbor_pyramids_case0 = [(p_x + x, p_y + y, p_z + z, pyr) for x, y, z, pyr in base_neighbors_case0]

        if p_pyr == 0:
            for pc in neighbor_pyramids_case0:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)
        elif p_pyr == 1:
            neighbor_pyramids_case1 = rotatePyramids(rotatePyramids(neighbor_pyramids_case0, 1, True, p_pos), 1, True, p_pos)
            for pc in neighbor_pyramids_case1:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)
        elif p_pyr == 2:
            neighbor_pyramids_case2 = rotatePyramids(neighbor_pyramids_case0, 2, False, p_pos)
            for pc in neighbor_pyramids_case2:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)
        elif p_pyr == 3:
            neighbor_pyramids_case3 = rotatePyramids(neighbor_pyramids_case0, 2, True, p_pos)
            for pc in neighbor_pyramids_case3:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)
        elif p_pyr == 4:
            neighbor_pyramids_case4 = rotatePyramids(neighbor_pyramids_case0, 1, True, p_pos)
            for pc in neighbor_pyramids_case4:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)
        elif p_pyr == 5:
            neighbor_pyramids_case5 = rotatePyramids(neighbor_pyramids_case0, 1, False, p_pos)
            for pc in neighbor_pyramids_case5:
                if pc not in pyramid_surround and pc not in pyramid_cluster:
                    pyramid_surround.add(pc)

    for p_x, p_y, p_z, p_pyr in pyramid_cluster:
        for i in range(1, 6):
            neighbor_pyramid = (p_x, p_y, p_z, (p_pyr + i) % 6)
            if neighbor_pyramid not in pyramid_cluster:
                pyramid_surround.add(neighbor_pyramid)

    return pyramid_surround

def create_rotated_pyramid_coords(r, temp_pyramid_coords, center):
    
    #Creates rotated pyramid coordinates based on rotation index r.
    #
    #Args:
    #    r: Rotation index (0-23).
    #    temp_pyramid_coords: List of tuples (x, y, z, pyramid).
    #    center: Tuple (cx, cy, cz).
    #    
    #Returns:
    #    List of rotated pyramid coordinates (x, y, z, pyramid).
    
    # NOTE: This function is very repetitive. It could be refactored to generate
    # the rotations programmatically, for example by defining the 24 rotations
    # of a cube and applying them sequentially.
    return_pyramid_coords = []
    
    if r == 0:
        return_pyramid_coords = list(temp_pyramid_coords) # +y up 0
    elif r == 1:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 1, True, center) # +y up 1
    elif r == 2:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 1, True, center), 1, True, center) # +y up 2
    elif r == 3:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 1, False, center) # +y up 3
        
    elif r == 4:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 2, False, center) # +x up 0
    elif r == 5:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, False, center), 1, True, center) # +x up 1
    elif r == 6:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, False, center), 1, True, center), 1, True, center) # +x up 2
    elif r == 7:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, False, center), 1, False, center) # +x up 3
        
    elif r == 8:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 0, True, center) # -y up 0
    elif r == 9:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 0, True, center), 1, True, center) # -y up 1
    elif r == 10:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 0, True, center), 1, True, center), 1, True, center) # -y up 2
    elif r == 11:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 0, True, center), 1, False, center) # -y up 3
        
    elif r == 12:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 2, True, center) # -x up 0
    elif r == 13:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, True, center), 1, True, center) # -x up 1
    elif r == 14:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, True, center), 1, True, center), 1, True, center) # -x up 2
    elif r == 15:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 2, True, center), 1, False, center) # -x up 3
        
    elif r == 16:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 0, True, center) # +z up 0
    elif r == 17:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 1, True, center) # +z up 1
    elif r == 18:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 1, True, center), 1, True, center) # +z up 2
    elif r == 19:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, True, center), 1, False, center) # +z up 3
        
    elif r == 20:
        return_pyramid_coords = rotatePyramids(temp_pyramid_coords, 0, False, center) # -z up 0
    elif r == 21:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, False, center), 1, True, center) # -z up 1
    elif r == 22:
        return_pyramid_coords = rotatePyramids(rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, False, center), 1, True, center), 1, True, center) # -z up 2
    elif r == 23:
        return_pyramid_coords = rotatePyramids(rotatePyramids(temp_pyramid_coords, 0, False, center), 1, False, center) # -z up 3
        
    else:
        return_pyramid_coords = list(temp_pyramid_coords)
        
    return return_pyramid_coords

def solve_surround():
    model = cp_model.CpModel()

    # Define the central tile
    clusterPyramids = generateTilePyramids(0, 0, 0, 0)
    clusterPyramidsSet = set(clusterPyramids)

    print(f"clusterPyramids count: {len(clusterPyramidsSet)}") # 19 OK
    #...# source .venv/bin/activate
        # python3 heeschZ3.py



    # Define the "corona" - the cells that need to be covered around the central tile
    corona_cells = calculate_all_neighbor_pyramids(clusterPyramids)
    print(f"coronaCells count: {len(corona_cells)}") # 180 OK

    # --- 1. Define Variables ---
    # boolean variable for every possible tile position

    placements = {} # -> all possible tile positions of surround
    cellCoverage = defaultdict(list)

    # Search space for surrounding tiles
    neighborCubes = []
    for p in clusterPyramids:
        for i in range(-4, 5):
            for j in range(-4, 5):
                for k in range(-4, 5):
                    newCubePos = (p[0] + i, p[1] + j, p[2] + k)
                    if newCubePos not in neighborCubes:
                        neighborCubes.append(newCubePos)

    print(f"neighborCubes count: {len(neighborCubes)}") # 270 OK

    nrPlacements = 0
    nrPlacementsWithOverlap = 0
    for neighborCubePos in neighborCubes:
        testTilePyramidCoords = []
        for rot in range(24):
            testTilePyramidCoords = generateTilePyramids(neighborCubePos[0], neighborCubePos[1], neighborCubePos[2], rot)

            if not any(p in clusterPyramidsSet for p in testTilePyramidCoords):
                placement_key = (neighborCubePos[0], neighborCubePos[1], neighborCubePos[2], rot)
                if placement_key not in placements:
                    # Create variable for this valid placement
                    var = model.NewBoolVar(f'x{neighborCubePos[0]}_y{neighborCubePos[1]}_z{neighborCubePos[2]}_r{rot}')
                    placements[placement_key] = var
                    nrPlacements += 1

                    # Map variable to the cells it covers
                    for cell in testTilePyramidCoords:
                        cellCoverage[cell].append(var)
                else:
                    nrPlacementsWithOverlap += 1

    print(f"Nr pyramids in cluster: {len(clusterPyramidsSet)}")
    for p in clusterPyramidsSet:
        print(p)
    print(f"Nr placements counter: {nrPlacements}")
    print(f"Nr placements with overlap: {nrPlacementsWithOverlap}")
    print(f"Nr possible placements: {len(placements)}") # soll: 1304 ist: 6076
    

    # for x in range(-2, 3):
    #     for y in range(-2, 3):
    #         for z in range(-2, 3):
    #             for rot in range(24):
    #                 tile_pyramids = generateTilePyramids(x, y, z, rot)
    #                 # Check for overlap with the central tile
    #                 if not any(p in clusterPyramidsSet for p in tile_pyramids):
    #                     # Create variable for this valid placement
    #                     var = model.NewBoolVar(f'x{x}_y{y}_z{z}_r{rot}')
    #                     placements[(x, y, z, rot)] = var
    # 
    #                     # Map variable to the cells it covers
    #                     for cell in tile_pyramids:
    #                         cellCoverage[cell].append(var)


    # Check if any corona cells are uncovered
    uncovered_corona = [c for c in corona_cells if not cellCoverage[c]]
    covered_corona = [c for c in corona_cells if cellCoverage[c]]
    if uncovered_corona:
        print(f"INFEASIBLE: {len(uncovered_corona)} corona cells are not covered by any valid tile placement.")
        print("Uncovered cells:")
        print(*uncovered_corona, sep="\n")
        
    if covered_corona:
        print(f"FEASIBLE: {len(covered_corona)} corona cells are covered by at least one valid tile placement.")
        print(f"Covered cells: {covered_corona}")
    else:
        print("No cells in covered_corona.")
    #return

    # --- 2. Define Constraints ---
    # Constraint 1: Tiles cannot overlap.
    # This means any given cell in space can be covered by at most one tile.
    for cell, potentialVars in cellCoverage.items():
        model.Add(sum(potentialVars) <= 1)

    # Constraint 2: The corona must be fully covered.
    # This means every cell in the corona must be covered by at least one tile.
    for cell in corona_cells:
        model.Add(sum(cellCoverage.get(cell, [])) >= 1)

    # --- 3. Solve ---
    solver = cp_model.CpSolver()
    #solver.parameters.log_search_progress = True
    status = solver.Solve(model)

    # --- 4. Output Results ---
    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print("Solution found!")
        # You can add code here to visualize or process the solution
        # For example, print the coordinates of placed tiles:
        nrTilesPlaced = 0
        i = 0
        with open("solution.txt", "w") as f:
            for pos, var in placements.items():
                if solver.Value(var):
                    nrTilesPlaced += 1
                    print(f"{pos}")
                    f.write(f"{pos}\n")


        print(f"Total tiles placed: {nrTilesPlaced}")
    else:
        print(f"No solution found. Status: {solver.StatusName(status)}")
        nrTilesPlaced = 0
        with open("solutionAttempt.txt", "w") as f:
            for pos, var in placements.items():
                if solver.Value(var):
                    nrTilesPlaced += 1
                    f.write(f"{pos}\n")
                    #print(f"Placed tile at {pos}")
        print(f"Total tiles placed: {nrTilesPlaced}")

    # print corona cells
    nrCoronaCells = 0
    filePath = "corona_cells.txt"
    with open(filePath, "w") as f:
        for pos in corona_cells:
            nrCoronaCells += 1
            f.write(f"{pos}\n")
            #print(f"{pos}")
    print(f"Total corona cells: {nrCoronaCells}")
    print(f"Corona cells written to {filePath}")


        # TODO: calculate overlap of each tile with the root tile and pairwise overlap!
        #...
        

if __name__ == '__main__':
    solve_surround()