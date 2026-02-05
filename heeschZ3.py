from ortools.sat.python import cp_model
from collections import defaultdict

def generateTilePyramids(x, y, z, rot):
    """
    Generates pyramid coordinates for a tile at (x,y,z) with rotation 'rot'.
    """
    # These are the local coordinates of the pyramids for a tile at origin in default orientation
    base_pyramids = [
        (0, 0, -1, 4),
        (0, 0, 0, 1),
        (0, 0, 0, 3),
        (0, 0, 0, 4),
        (0, 0, 0, 5),
        (-1, 0, 0, 0),
        (0, -1, 0, 2),
        (0, 0, 1, 1),
        (0, 0, 1, 3),
        (0, 0, 1, 4),
        (0, 0, 1, 5),
        (-1, 0, 1, 0),
        (0, -1, 1, 2),
        (0, 0, 2, 1),
        (0, 0, 2, 3),
        (0, 0, 2, 4),
        (0, 0, 2, 5),
        (-1, 0, 2, 0),
        (0, -1, 2, 2),
    ]

    # NOTE: The center of rotation for the base tile is assumed to be (0,0,0).
    # This might need to be adjusted depending on the tile's geometry.
    # A potential center based on the coordinates could be (-0.5, -0.5, 0.5).
    center = (0, 0, 0)
    rotated_pyramids = create_rotated_pyramid_coords(rot, base_pyramids, center)

    final_pyramids = []
    for px, py, pz, p_type in rotated_pyramids:
        final_pyramids.append((px + x, py + y, pz + z, p_type))
    return final_pyramids

def rotatePyramids(pyr_coords, axisxyz, direction, center):
    
    #Rotates a list of pyramid coordinates around a center point.
    #
    #Args:
    #    pyr_coords: List of tuples (x, y, z, pyramid).
    #    axisxyz: 0 for X-axis, 1 for Y-axis, 2 for Z-axis.
    #    direction: Boolean, True for one direction, False for the other.
    #    center: Tuple (cx, cy, cz).
    #    
    #Returns:
    #    List of rotated pyramid coordinates (x, y, z, pyramid).
    
    # NOTE: The rotation logic is intentionally designed for a left-handed
    # coordinate system (e.g., for visualization in Unity). The transformations
    # may appear unusual for a right-handed system but are correct for their
    # intended purpose.
    new_coords = []
    cx, cy, cz = center

    if axisxyz == 0:  # X-axis
        if direction:
            # x -> x
            # y -> -z
            # z -> y
            pyr_map = {0: 0, 1: 1, 2: 5, 3: 4, 4: 2, 5: 3}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((x, cy + (z - cz), cz - (y - cy), new_pyr))
        else:
            # x -> x
            # y -> z
            # z -> -y
            pyr_map = {0: 0, 1: 1, 2: 4, 3: 5, 4: 3, 5: 2}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((x, cy - (z - cz), cz + (y - cy), new_pyr))

    elif axisxyz == 1:  # Y-axis
        if direction:
            # x -> z
            # y -> y
            # z -> -x
            pyr_map = {0: 4, 1: 5, 2: 2, 3: 3, 4: 1, 5: 0}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((cx - (z - cz), y, cz + (x - cx), new_pyr))
        else:
            # x -> -z
            # y -> y
            # z -> x
            pyr_map = {0: 5, 1: 4, 2: 2, 3: 3, 4: 0, 5: 1}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((cx + (z - cz), y, cz - (x - cx), new_pyr))

    elif axisxyz == 2:  # Z-axis
        if direction:
            # x -> -y
            # y -> x
            # z -> z
            pyr_map = {0: 3, 1: 2, 2: 0, 3: 1, 4: 4, 5: 5}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((cx + (y - cy), cy - (x - cx), z, new_pyr))
        else:
            # x -> y
            # y -> -x
            # z -> z
            pyr_map = {0: 2, 1: 3, 2: 1, 3: 0, 4: 4, 5: 5}
            for x, y, z, p in pyr_coords:
                new_pyr = pyr_map.get(p, -1)
                new_coords.append((cx - (y - cy), cy + (x - cx), z, new_pyr))

    return new_coords

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

    # Define the "corona" - the cells that need to be covered around the central tile
    def get_opposite_cell(cell):
        x, y, z, p = cell
        # Pyramid indices assumed as: 0:+X, 1:-X, 2:+Y, 3:-Y, 4:+Z, 5:-Z
        if p == 0: return (x + 1, y, z, 1)
        if p == 1: return (x - 1, y, z, 0)
        if p == 2: return (x, y + 1, z, 3)
        if p == 3: return (x, y - 1, z, 2)
        if p == 4: return (x, y, z + 1, 5)
        if p == 5: return (x, y, z - 1, 4)
        return None # Should not happen

    corona_cells = {get_opposite_cell(c) for c in clusterPyramids}

    # --- 1. Define Variables ---
    # boolean variable for every possible tile position

    placements = {} # -> all possible tile positions of surround
    cellCoverage = defaultdict(list)

    # Search space for surrounding tiles
    for x in range(-4, 5):
        for y in range(-3, 4):
            for z in range(-3, 4):
                for rot in range(24):
                    tile_pyramids = generateTilePyramids(x, y, z, rot)
                    # Check for overlap with the central tile
                    if not any(p in clusterPyramidsSet for p in tile_pyramids):
                        # Create variable for this valid placement
                        var = model.NewBoolVar(f'x{x}_y{y}_z{z}_r{rot}')
                        placements[(x, y, z, rot)] = var

                        # Map variable to the cells it covers
                        for cell in tile_pyramids:
                            cellCoverage[cell].append(var)

    # --- 2. Define Constraints ---
    # Each cell in the grid can be covered by at most one tile (no overlaps between surrounding tiles)
    for cell, potentialVars in cellCoverage.items():
        model.Add(sum(potentialVars) <= 1)

    # The corona of the central tile must be completely covered.
    for cell in corona_cells:
        # This enforces that one of the placements covering this corona cell must be chosen.
        model.Add(sum(cellCoverage.get(cell, [])) == 1)

    # --- 3. Solve ---
    solver = cp_model.CpSolver()
    solver.parameters.log_search_progress = True
    status = solver.Solve(model)

    # --- 4. Output Results ---
    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print("Solution found!")
        # You can add code here to visualize or process the solution
        # For example, print the coordinates of placed tiles:
        for pos, var in placements.items():
            if solver.Value(var):
                print(f"Placed tile at {pos}")
    else:
        print(f"No solution found. Status: {solver.StatusName(status)}")

if __name__ == '__main__':
    solve_surround()