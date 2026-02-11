
# source .venv/bin/activate
# python3 heeschZ3.py

# -> 3 coronas: timed out on google colab!


from ortools.sat.python import cp_model
import sys
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

def generateTilePyramids(x, y, z, rot, base_pyramids):
    """
    Generates pyramid coordinates for a tile at (x,y,z) with rotation 'rot'.
    """
    # These are the local coordinates of the pyramids for a tile at origin in default orientation
    # tile description
    #base_pyramids = [
    #    (0, 0, -1, 4),
    #
    #    ( 0,  0, 0, 1),
    #    ( 0,  0, 0, 3),
    #    ( 0,  0, 0, 4),
    #    ( 0,  0, 0, 5),
    #    (-1,  0, 0, 0),
    #    ( 0, -1, 0, 2),
    #
    #    ( 0,  0, 1, 1),
    #    ( 0,  0, 1, 3),
    #    ( 0,  0, 1, 4),
    #    ( 0,  0, 1, 5),
    #    (-1,  0, 1, 0),
    #    ( 0, -1, 1, 2),
        
        #( 0,  0, 2, 1),
        #( 0,  0, 2, 3),
        #( 0,  0, 2, 4),
        #( 0,  0, 2, 5),
        #(-1,  0, 2, 0),
        #( 0, -1, 2, 2),
#
        #( 0,  0, 3, 1),
        #( 0,  0, 3, 3),
        #( 0,  0, 3, 4),
        #( 0,  0, 3, 5),
        #(-1,  0, 3, 0),
        #( 0, -1, 3, 2),

    #    (0, 0, 0, 0),
    #    (0, 0, 0, 1),
    #    (0, 0, 0, 2),
    #    (0, 0, 0, 4),
    #    (0, 0, 0, 5),
    #    (0, 1, 0, 3),
    #]


    #base_pyramids = [
    #    (0, 0, 0, 0),
    #    #(0, 0, 0, 1),
    #    #(0, 0, 0, 2),
    #    (0, 0, 0, 3),
    #    (0, 0, 0, 4),
    #    #(0, 0, 0, 5),
    #]
    # base_pyramids = [
    #     (0, 0, 0, 0),
    #     (0, 0, 0, 1),
    #     (0, 0, 0, 2),
    #     (0, 0, 0, 3),
    #     (0, 0, 0, 4),
    #     (0, 0, 0, 5),
    # ]

    #base_pyramids = [
        #(0, 0, 0, 0), 
        #(0, 0, 0, 2),
        #(0, 1, 0, 3),
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

def solve_monolithic(search_surrounds, base_shape, previous_solution=None, shape_index=None):

    model = cp_model.CpModel()
    
    # 1. Setup Grid and Center
    center_tile_pos = (0, 0, 0, 0)
    center_cells = set(generateTilePyramids(*center_tile_pos, base_shape))
    
    # 2. Define Search Space (Bounding Box)
    # A radius of 5 should be sufficient for 2 coronas
    search_radius = 6
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
    
    # Precompute neighbors of the center tile to filter S1 candidates
    center_neighbors_set = calculate_all_neighbor_pyramids(center_cells)

    # --- Step 1: Generate all valid geometries in search space ---
    valid_geometries = []
    tile_origin_radius = 5
    for x in range(-tile_origin_radius, tile_origin_radius + 1):
        for y in range(-tile_origin_radius, tile_origin_radius + 1):
            for z in range(-tile_origin_radius, tile_origin_radius + 1):
                for rot in range(24):
                    pos = (x, y, z, rot)
                    cells = generateTilePyramids(*pos, base_shape)
                    
                    # Optimization: Tile must be within search space
                    if not all(c in search_cells for c in cells):
                        continue
                        
                    # Optimization: Tile must not overlap center
                    if any(c in center_cells for c in cells):
                        continue
                    
                    valid_geometries.append((pos, cells))

    # --- Step 2: Identify S1 Candidates ---
    s1_candidates = []
    s1_occupied_cells = set()
    
    for pos, cells in valid_geometries:
        if any(c in center_neighbors_set for c in cells):
            s1_candidates.append((pos, cells))
            s1_occupied_cells.update(cells)
            
    # Create S1 variables
    for pos, cells in s1_candidates:
        s1_var = model.NewBoolVar(f's1_{pos}')
        s1_placements[pos] = s1_var
        for c in cells:
            cell_covered_by_s1[c].append(s1_var)
            
    print(f"Generated {len(s1_placements)} potential tile positions for s1.")

    # --- Step 3: Identify S2 Candidates ---
    if search_surrounds >= 2:
        s1_boundary = calculate_all_neighbor_pyramids(s1_occupied_cells)
        s2_candidates = []
        s2_occupied_cells = set()
        
        s1_pos_set = set(s1_placements.keys())
        
        for pos, cells in valid_geometries:
            if pos in s1_pos_set: continue
            
            # Must touch at least one valid S1 boundary location
            if any(c in s1_boundary for c in cells):
                s2_candidates.append((pos, cells))
                s2_occupied_cells.update(cells)
        
        for pos, cells in s2_candidates:
            s2_var = model.NewBoolVar(f's2_{pos}')
            s2_placements[pos] = s2_var
            for c in cells:
                cell_covered_by_s2[c].append(s2_var)
        
        print(f"Generated {len(s2_placements)} potential tile positions for s2.")

    # --- Step 4: Identify S3 Candidates ---
    if search_surrounds == 3:
        s2_boundary = calculate_all_neighbor_pyramids(s2_occupied_cells)
        s3_candidates = []
        
        s1_pos_set = set(s1_placements.keys())
        
        for pos, cells in valid_geometries:
            if pos in s1_pos_set: continue
            
            # Must touch at least one valid S2 boundary location
            if any(c in s2_boundary for c in cells):
                s3_candidates.append((pos, cells))
        
        for pos, cells in s3_candidates:
            s3_var = model.NewBoolVar(f's3_{pos}')
            s3_placements[pos] = s3_var
            for c in cells:
                cell_covered_by_s3[c].append(s3_var)
            
            if pos in s2_placements:
                model.Add(s2_placements[pos] + s3_var <= 1)
                
        print(f"Generated {len(s3_placements)} potential tile positions for s3.")

    # --- Apply Hints from Previous Solution ---
    if previous_solution:
        print("Applying hints from previous solution...")
        if 's1' in previous_solution:
            for pos in previous_solution['s1']:
                if pos in s1_placements:
                    model.AddHint(s1_placements[pos], 1)
        
        if search_surrounds >= 2 and 's2' in previous_solution:
            for pos in previous_solution['s2']:
                if pos in s2_placements:
                    model.AddHint(s2_placements[pos], 1)
        
        # Note: We don't usually hint s3 because it's the new layer we are searching for,
        # but if we had a partial s3 solution we could hint it here too.

    with open("heesch_solver_summary.log", "a") as f:
        f.write("Testing Shape ")
        if shape_index is not None:
            f.write(f"{shape_index}\n")
        f.write(f"Pyramids: {base_shape}\n")
        f.write(f"Coronas: {search_surrounds}\n")
        f.write(f"S1 positions: {len(s1_placements)}\n")
        if search_surrounds >= 2:
            f.write(f"S2 positions: {len(s2_placements)}\n")
        if search_surrounds == 3:
            f.write(f"S3 positions: {len(s3_placements)}\n")
    

    #5. Constraints
    
    # A. Disjointness
    # For every cell, sum(s1) + sum(s2) <= 1
    for c in search_cells:
        if c in center_cells: continue
        
        s1_cov = cell_covered_by_s1[c]
        if search_surrounds == 2:
            s2_cov = cell_covered_by_s2[c]
        if search_surrounds == 3:
            s2_cov = cell_covered_by_s2[c]
            s3_cov = cell_covered_by_s3[c]
    #    
        if search_surrounds == 2 or search_surrounds == 3:
            if s1_cov or s2_cov:
                model.Add(sum(s1_cov) + sum(s2_cov) <= 1)
            if search_surrounds == 3:
                if s1_cov or s3_cov:
                    model.Add(sum(s1_cov) + sum(s3_cov) <= 1)
                if s2_cov or s3_cov:
                    model.Add(sum(s2_cov) + sum(s3_cov) <= 1)
    print("Generated disjointness constraints.")

    # B. S1 Surrounds Center
    # All neighbors of Center must be covered by S1
    center_neighbors = calculate_all_neighbor_pyramids(center_cells)
    for c in center_neighbors:
        if c in search_cells:
            if cell_covered_by_s1[c]:
                model.Add(sum(cell_covered_by_s1[c]) == 1)
            else:
                print(f"Warning: Center neighbor {c} cannot be covered by any tile.")
                with open("heesch_solver_summary.log", "a") as f:
                    f.write("Solver Status: INFEASIBLE (Trivial)\n")
                    f.write(f"No solution found: Center neighbor {c} cannot be covered (Trivial INFEASIBLE).\n\n")
                return
    print("Generated surrounds center constraints.")

    # C. S2 Surrounds S1
    # Logic: If any neighbor of cell c is covered by S1, then c must be covered by (S1 or S2).
    # This forces S2 to fill all gaps around S1.
    if search_surrounds >= 2:
        for c in search_cells:
            if c in center_cells: continue


            neighbors = cell_neighbors.get(c, [])
            if not neighbors: continue

            # Collect all S1 variables from all neighbors
            neighbor_s1_vars = {}
            for n in neighbors:
                for var in cell_covered_by_s1.get(n, []):
                    neighbor_s1_vars[var.Index()] = var

            if not neighbor_s1_vars: continue

            # Optimization: Use Boolean Logic instead of Linear Arithmetic
            # Logic: If any neighbor is S1, then c must be covered by (S1 or S2).
            # Equivalent to: neighbor_is_s1 IMPLIES (c_is_s1 OR c_is_s2)
            target_literals = cell_covered_by_s1[c] + cell_covered_by_s2[c]
            for n_var in neighbor_s1_vars.values():
                model.AddBoolOr([n_var.Not()] + target_literals)
        print("Generated S2 surrounds S1 constraints.")

    if search_surrounds == 3:
        # D. S3 Surrounds S2
        for c in search_cells:
            if c in center_cells: continue
                
            neighbors = cell_neighbors.get(c, [])
            if not neighbors: continue
                
            # Collect all S2 variables from all neighbors
            neighbor_s2_vars = {}
            for n in neighbors:
                for var in cell_covered_by_s2.get(n, []):
                    neighbor_s2_vars[var.Index()] = var
                
            if not neighbor_s2_vars: continue
                
            # Logic: If any neighbor is S2, then c must be covered by (S1 or S2 or S3).
            target_literals = cell_covered_by_s1[c] + cell_covered_by_s2[c] + cell_covered_by_s3[c]
            for n_var in neighbor_s2_vars.values():
                model.AddBoolOr([n_var.Not()] + target_literals)
        print("Generated S3 surrounds S2 constraints.")

    print("generated model. Starting solver...")

    # 6. Solve
    solver = cp_model.CpSolver()
    solver.parameters.num_search_workers = 8
    solver.parameters.max_time_in_seconds = 3600
    solver.parameters.log_search_progress = True
    
    print(f"Solving monolithic model for {search_surrounds} corona(s)...")
    status = solver.Solve(model)

    # Generated 1464 potential tile positions for s1.
    # Generated 116044 potential tile positions for s2.
    # Generated 116044 potential tile positions for s3.
    # Generated disjointness constraints.
    # Generated surrounds center constraints.
    # Generated S2 surrounds S1 constraints.
    # Generated S3 surrounds S2 constraints.
    # generated model. Starting solver...
    # Solving monolithic model for 3 coronas...
    # No solution found.

    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print("Solution found!")
        s1_tiles = [p for p, v in s1_placements.items() if solver.Value(v)]
        if search_surrounds >= 2:
            s2_tiles = [p for p, v in s2_placements.items() if solver.Value(v)]
            if search_surrounds == 3:
                s3_tiles = [p for p, v in s3_placements.items() if solver.Value(v)]
        
        with open("all_solutions_heesch1_tile.txt", "a") as f:
            f.write("--- Monolithic Solution ---\n")
            f.write("Corona 1:\n")
            for t in s1_tiles:
                print(f"S1: {t}")
                f.write(f"{t}\n")
            
            if search_surrounds >= 2:
                f.write("Corona 2:\n")
                for t in s2_tiles:
                    print(f"S2: {t}")
                    f.write(f"{t}\n")

            if search_surrounds == 3:
                f.write("Corona 3:\n")
                for t in s3_tiles:
                    print(f"S3: {t}")
                    f.write(f"{t}\n")
        
        with open("heesch_solver_summary.log", "a") as f:
            f.write(f"Solver Status: {solver.StatusName(status)}\n")
            f.write("Solution Found:\n")
            f.write(f"S1: {s1_tiles}\n")
            if search_surrounds >= 2:
                f.write(f"S2: {s2_tiles}\n")
            if search_surrounds == 3:
                f.write(f"S3: {s3_tiles}\n")
            f.write("\n")
        
        # Return the solution dictionary for the next iteration
        solution_data = {'s1': s1_tiles}
        if search_surrounds >= 2:
            solution_data['s2'] = s2_tiles
        if search_surrounds == 3:
            solution_data['s3'] = s3_tiles
        return solution_data

    elif status == cp_model.INFEASIBLE:
        print("No solution found: INFEASIBLE. The solver proved no solution exists within the constraints.")
        with open("heesch_solver_summary.log", "a") as f:
            f.write(f"Solver Status: {solver.StatusName(status)}\n")
            f.write("No solution found: INFEASIBLE\n\n")
        return None
    elif status == cp_model.UNKNOWN:
        print("No solution found: UNKNOWN. The solver reached the time limit (timeout) without finding a solution.")
        with open("heesch_solver_summary.log", "a") as f:
            f.write(f"Solver Status: {solver.StatusName(status)}\n")
            f.write("No solution found: UNKNOWN (Timeout)\n\n")
        return None
    else:
        print(f"No solution found. Status: {status}")
        with open("heesch_solver_summary.log", "a") as f:
            f.write(f"Solver Status: {solver.StatusName(status)}\n")
            f.write(f"No solution found. Status: {status}\n\n")
        return None

def add_int3(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])

def sub_int3(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])

def get_face_neighbor_candidates(p):
    # Returns list of (x, y, z, pyr)
    candidates = []
    x, y, z, pyr = p
    
    if pyr == 0:
        # 2, 3, 4, 5, x+1: 1
        candidates.append((x, y, z, 2))
        candidates.append((x, y, z, 3))
        candidates.append((x, y, z, 4))
        candidates.append((x, y, z, 5))
        candidates.append((x + 1, y, z, 1))
    elif pyr == 1:
        # 2, 3, 4, 5, x-1: 0
        candidates.append((x, y, z, 2))
        candidates.append((x, y, z, 3))
        candidates.append((x, y, z, 4))
        candidates.append((x, y, z, 5))
        candidates.append((x - 1, y, z, 0))
    elif pyr == 2:
        # 0, 1, 4, 5, y+1: 3
        candidates.append((x, y, z, 0))
        candidates.append((x, y, z, 1))
        candidates.append((x, y, z, 4))
        candidates.append((x, y, z, 5))
        candidates.append((x, y + 1, z, 3))
    elif pyr == 3:
        # 0, 1, 4, 5, y-1: 2
        candidates.append((x, y, z, 0))
        candidates.append((x, y, z, 1))
        candidates.append((x, y, z, 4))
        candidates.append((x, y, z, 5))
        candidates.append((x, y - 1, z, 2))
    elif pyr == 4:
        # 0, 1, 2, 3, z+1: 5
        candidates.append((x, y, z, 0))
        candidates.append((x, y, z, 1))
        candidates.append((x, y, z, 2))
        candidates.append((x, y, z, 3))
        candidates.append((x, y, z + 1, 5))
    elif pyr == 5:
        # 0, 1, 2, 3, z-1: 4
        candidates.append((x, y, z, 0))
        candidates.append((x, y, z, 1))
        candidates.append((x, y, z, 2))
        candidates.append((x, y, z, 3))
        candidates.append((x, y, z - 1, 4))
        
    return candidates

def transform_pyramids(pyramids, start_p, end_p):
    diff = sub_int3((end_p[0], end_p[1], end_p[2]), (start_p[0], start_p[1], start_p[2]))
    from_pyr = start_p[3]
    to_pyr = end_p[3]
    from_pos = (start_p[0], start_p[1], start_p[2])
    
    rotated_pyramids = []
    
    # Logic from C# transformPyramids switch
    if from_pyr == 0:
        if to_pyr == 0: rotated_pyramids = list(pyramids)
        elif to_pyr == 1: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
        elif to_pyr == 2: rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
        elif to_pyr == 3: rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
        elif to_pyr == 4: rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
        elif to_pyr == 5: rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
    elif from_pyr == 1:
        if to_pyr == 0: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
        elif to_pyr == 1: rotated_pyramids = list(pyramids)
        elif to_pyr == 2: rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
        elif to_pyr == 3: rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
        elif to_pyr == 4: rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
        elif to_pyr == 5: rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
    elif from_pyr == 2:
        if to_pyr == 0: rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
        elif to_pyr == 1: rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
        elif to_pyr == 2: rotated_pyramids = list(pyramids)
        elif to_pyr == 3: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 0, True, from_pos), 0, True, from_pos)
        elif to_pyr == 4: rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
        elif to_pyr == 5: rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
    elif from_pyr == 3:
        if to_pyr == 0: rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
        elif to_pyr == 1: rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
        elif to_pyr == 2: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 2, True, from_pos), 2, True, from_pos)
        elif to_pyr == 3: rotated_pyramids = list(pyramids)
        elif to_pyr == 4: rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
        elif to_pyr == 5: rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
    elif from_pyr == 4:
        if to_pyr == 0: rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
        elif to_pyr == 1: rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
        elif to_pyr == 2: rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
        elif to_pyr == 3: rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
        elif to_pyr == 4: rotated_pyramids = list(pyramids) # C# code had a comment about error/test here, but loop used original pyramids
        elif to_pyr == 5: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
    elif from_pyr == 5:
        if to_pyr == 0: rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
        elif to_pyr == 1: rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
        elif to_pyr == 2: rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
        elif to_pyr == 3: rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
        elif to_pyr == 4: rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 0, True, from_pos), 0, True, from_pos)
        elif to_pyr == 5: rotated_pyramids = list(pyramids)

    transformed = []
    for p in rotated_pyramids:
        new_pos = add_int3((p[0], p[1], p[2]), diff)
        transformed.append((new_pos[0], new_pos[1], new_pos[2], p[3]))
    return transformed

class GenNode:
    def __init__(self, placement, existing_pyramids=None):
        self.placement = placement
        if existing_pyramids:
            self.pyramids = list(existing_pyramids)
            if placement not in self.pyramids:
                self.pyramids.append(placement)
        else:
            self.pyramids = [placement]
        self.face_neighbors = []

    def calculate_face_neighbors(self):
        candidates = []
        for p in self.pyramids:
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

def cluster_is_new(nodes_to_compare, new_pyramids):
    # Test if new cluster is rotation of previous cluster
    for node_to_compare in nodes_to_compare:
        if len(node_to_compare.pyramids) != len(new_pyramids):
            print("ERROR different count!!!")
            continue
            
        # Try to map every pyramid in new_pyramids to the first pyramid of node_to_compare
        anchor = node_to_compare.pyramids[0]
        anchor_pos = (anchor[0], anchor[1], anchor[2])
        
        for t in new_pyramids:
            transformed_new = transform_pyramids(new_pyramids, t, anchor)
            
            # 4 rotations around pyramid axis
            axis = 0
            if anchor[3] == 0 or anchor[3] == 1: axis = 0
            elif anchor[3] == 2 or anchor[3] == 3: axis = 1
            elif anchor[3] == 4 or anchor[3] == 5: axis = 2
            
            for r in range(4):
                check_pyramids = transformed_new
                if r == 1:
                    check_pyramids = rotatePyramids(transformed_new, axis, True, anchor_pos)
                elif r == 2:
                    check_pyramids = rotatePyramids(rotatePyramids(transformed_new, axis, True, anchor_pos), axis, True, anchor_pos)
                elif r == 3:
                    check_pyramids = rotatePyramids(transformed_new, axis, False, anchor_pos)
                
                # Check equality
                difference = False
                compare_set = set(node_to_compare.pyramids)
                for pt in check_pyramids:
                    if pt not in compare_set:
                        difference = True
                        break
                
                if not difference:
                    return False # Found a match, so it is NOT new
                    
    return True

def generate_polypyramids(n):
    """Generates poly-pyramids of size n using BFS."""
    print(f"Generating poly-pyramids of size {n}...")
    nodes = []
    
    # Layer 0 (Size 1)
    layer0 = []
    root = GenNode((0,0,0,0))
    root.calculate_face_neighbors()
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
                    new_node = GenNode(p, node.pyramids)
                    new_node.calculate_face_neighbors()
                    current_layer_nodes.append(new_node)
                else:
                    if cluster_is_new(current_layer_nodes, new_pyramids):
                        new_node = GenNode(p, node.pyramids)
                        new_node.calculate_face_neighbors()
                        if not new_node.detect_holes():
                            current_layer_nodes.append(new_node)
                            
        nodes.append(current_layer_nodes)
        print(f"Layer {layer} (Size {layer+1}) generated {len(current_layer_nodes)} shapes.")
        
    return [n.pyramids for n in nodes[n-1]]

if __name__ == '__main__':
    # Redirect stdout and stderr to a file while keeping console output
    #log_file = open("/content/drive/MyDrive/ColabNotebooks/heesch_solver.log", "w")
    log_file = open("heesch_solver.log", "w")
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
    sys.stderr = sys.stdout
    
    #with open("/content/drive/MyDrive/ColabNotebooks/heesch_solver_summary.log", "w") as f:
    with open("heesch_solver_summary.log", "w") as f:
        f.write("--- Heesch Solver Summary ---\n")
    
    shape_index = 0
    for nrPyramidsInShape in range(5, 7):
        shapes = generate_polypyramids(nrPyramidsInShape)
        for shape in shapes:
            print(f"Checking shape {shape_index}...")
            search_surrounds = 1
            solution = solve_monolithic(search_surrounds, shape, shape_index=shape_index)
            if solution:
                search_surrounds += 1
                solution = solve_monolithic(search_surrounds, shape, previous_solution=solution, shape_index=shape_index)
                if solution:
                    search_surrounds += 1
                    solve_monolithic(search_surrounds, shape, previous_solution=solution, shape_index=shape_index)
            shape_index += 1