
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

    #    (0, 0, 0, 0),
    #    (0, 0, 0, 1),
    #    (0, 0, 0, 2),
    #    (0, 0, 0, 4),
    #    (0, 0, 0, 5),
    #    (0, 1, 0, 3),
    ]


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

def solve_monolithic():
    model = cp_model.CpModel()
    
    # 1. Setup Grid and Center
    center_tile_pos = (0, 0, 0, 0)
    center_cells = set(generateTilePyramids(*center_tile_pos))
    
    # 2. Define Search Space (Bounding Box)
    # A radius of 5 should be sufficient for 2 coronas
    search_radius = 15
    search_cells = set()
    for x in range(-search_radius, search_radius + 1):
        for y in range(-search_radius, search_radius + 1):
            for z in range(-search_radius, search_radius + 1):
                for pyr in range(6):
                    search_cells.add((x, y, z, pyr))
                    
    # 3. Precompute Neighbors for all search cells
    # This is needed for the "S2 surrounds S1" constraint
    cell_neighbors = {}
    for cell in search_cells:
        ns = calculate_all_neighbor_pyramids({cell})
        valid_ns = [n for n in ns if n in search_cells]
        cell_neighbors[cell] = valid_ns

    # 4. Generate Tile Variables
    s1_placements = {}
    s2_placements = {}
    s3_placements = {}
    
    cell_covered_by_s1 = defaultdict(list)
    cell_covered_by_s2 = defaultdict(list)
    cell_covered_by_s3 = defaultdict(list)
    
    # Iterate over potential tile origins
    tile_origin_radius = 8
    for x in range(-tile_origin_radius, tile_origin_radius + 1):
        for y in range(-tile_origin_radius, tile_origin_radius + 1):
            for z in range(-tile_origin_radius, tile_origin_radius + 1):
                for rot in range(24):
                    pos = (x, y, z, rot)
                    cells = generateTilePyramids(*pos)
                    
                    # Optimization: Tile must be within search space
                    if not all(c in search_cells for c in cells):
                        continue
                        
                    # Optimization: Tile must not overlap center
                    if any(c in center_cells for c in cells):
                        continue
                        
                    # Create variables for S1 and S2
                    s1_var = model.NewBoolVar(f's1_{pos}')
                    s2_var = model.NewBoolVar(f's2_{pos}')
                    s3_var = model.NewBoolVar(f's3_{pos}')
                    
                    s1_placements[pos] = s1_var
                    s2_placements[pos] = s2_var
                    s3_placements[pos] = s3_var
                    
                    # Link to cells
                    for c in cells:
                        cell_covered_by_s1[c].append(s1_var)
                        cell_covered_by_s2[c].append(s2_var)
                        cell_covered_by_s3[c].append(s3_var)
                        
                    # Constraint: A tile cannot be in both S1 and S2
                    model.Add(s1_var + s2_var <= 1)
                    model.Add(s1_var + s3_var <= 1)
                    model.Add(s2_var + s3_var <= 1)
                    
    print(f"Generated {len(s1_placements)} potential tile positions.")

    # 5. Constraints
    
    # A. Disjointness
    # For every cell, sum(s1) + sum(s2) <= 1
    for c in search_cells:
        if c in center_cells: continue
        
        s1_cov = cell_covered_by_s1[c]
        s2_cov = cell_covered_by_s2[c]
        s3_cov = cell_covered_by_s3[c]
        
        if s1_cov or s2_cov:
            model.Add(sum(s1_cov) + sum(s2_cov) <= 1)
        if s1_cov or s3_cov:
            model.Add(sum(s1_cov) + sum(s3_cov) <= 1)
        if s2_cov or s3_cov:
            model.Add(sum(s2_cov) + sum(s3_cov) <= 1)


    # B. S1 Surrounds Center
    # All neighbors of Center must be covered by S1
    center_neighbors = calculate_all_neighbor_pyramids(center_cells)
    for c in center_neighbors:
        if c in search_cells:
            if cell_covered_by_s1[c]:
                model.Add(sum(cell_covered_by_s1[c]) == 1)
            else:
                print(f"Warning: Center neighbor {c} cannot be covered by any tile.")
                return

    # C. S2 Surrounds S1
    # Logic: If any neighbor of cell c is covered by S1, then c must be covered by (S1 or S2).
    # This forces S2 to fill all gaps around S1.
    for c in search_cells:
        if c in center_cells: continue
            
        neighbors = cell_neighbors.get(c, [])
        if not neighbors: continue
            
        # Collect all S1 variables from all neighbors
        neighbor_s1_vars = []
        for n in neighbors:
            neighbor_s1_vars.extend(cell_covered_by_s1[n])
            
        if not neighbor_s1_vars: continue
            
        # Constraint: sum(neighbor_s1) <= BigM * (covered_by_s1(c) + covered_by_s2(c))
        # If c is empty, then NO neighbor can be S1.
        current_cell_covered = sum(cell_covered_by_s1[c]) + sum(cell_covered_by_s2[c])
        model.Add(sum(neighbor_s1_vars) <= len(neighbor_s1_vars) * current_cell_covered)

    # D. S3 Surrounds S2
    for c in search_cells:
        if c in center_cells: continue
            
        neighbors = cell_neighbors.get(c, [])
        if not neighbors: continue
            
        # Collect all S2 variables from all neighbors
        neighbor_s2_vars = []
        for n in neighbors:
            neighbor_s2_vars.extend(cell_covered_by_s2[n])
            
        if not neighbor_s2_vars: continue
            
        # Constraint: sum(neighbor_s2) <= BigM * (covered_by_s1(c) + covered_by_s2(c) + covered_by_s3(c))
        current_cell_covered = sum(cell_covered_by_s1[c]) + sum(cell_covered_by_s2[c]) + sum(cell_covered_by_s3[c])
        model.Add(sum(neighbor_s2_vars) <= len(neighbor_s2_vars) * current_cell_covered)
    

    # 6. Solve
    solver = cp_model.CpSolver()
    solver.parameters.num_search_workers = 8
    solver.parameters.max_time_in_seconds = 600
    
    print("Solving monolithic model for 3 coronas...")
    status = solver.Solve(model)

    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print("Solution found!")
        s1_tiles = [p for p, v in s1_placements.items() if solver.Value(v)]
        s2_tiles = [p for p, v in s2_placements.items() if solver.Value(v)]
        s3_tiles = [p for p, v in s3_placements.items() if solver.Value(v)]
        
        with open("all_solutions.txt", "w") as f:
            f.write("--- Monolithic Solution ---\n")
            f.write("Corona 1:\n")
            for t in s1_tiles:
                print(f"S1: {t}")
                f.write(f"{t}\n")
            
            f.write("Corona 2:\n")
            for t in s2_tiles:
                print(f"S2: {t}")
                f.write(f"{t}\n")

            f.write("Corona 3:\n")
            for t in s3_tiles:
                print(f"S3: {t}")
                f.write(f"{t}\n")
    else:
        print("No solution found.")

if __name__ == '__main__':
    solve_monolithic()