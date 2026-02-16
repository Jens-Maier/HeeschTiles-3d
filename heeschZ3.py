from ortools.sat.python import cp_model
import sys
from collections import defaultdict
from typing import List, Set, Tuple


PyramidCoord = Tuple[int, int, int, int]
Position = Tuple[int, int, int]

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

def transform_pyramids(pyramids, start_p, end_p, transformSteps = None):
    if transformSteps is None:
        transformSteps = []
    
    diff = sub_int3((end_p[0], end_p[1], end_p[2]), (start_p[0], start_p[1], start_p[2]))
    from_pyr = start_p[3]
    to_pyr = end_p[3]
    from_pos = (start_p[0], start_p[1], start_p[2])
    
    rotated_pyramids = []
    
    # Logic from C# transformPyramids switch
    if from_pyr == 0:
        if to_pyr == 0: 
            rotated_pyramids = list(pyramids)
        elif to_pyr == 1: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
            transformSteps.append((1, True, from_pos))
        elif to_pyr == 2: 
            rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
            transformSteps.append((2, False, from_pos))
        elif to_pyr == 3: 
            rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
            transformSteps.append((2, True, from_pos))
        elif to_pyr == 4: 
            rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
        elif to_pyr == 5: 
            rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
            transformSteps.append((1, False, from_pos))
    elif from_pyr == 1:
        if to_pyr == 0: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
            transformSteps.append((1, True, from_pos))
        elif to_pyr == 1:
            rotated_pyramids = list(pyramids)
        elif to_pyr == 2: 
            rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
            transformSteps.append((2, True, from_pos))
        elif to_pyr == 3: 
            rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
            transformSteps.append((2, False, from_pos))
        elif to_pyr == 4: 
            rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
            transformSteps.append((1, False, from_pos))
        elif to_pyr == 5: 
            rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
    elif from_pyr == 2:
        if to_pyr == 0: 
            rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
            transformSteps.append((2, True, from_pos))
        elif to_pyr == 1: 
            rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
            transformSteps.append((2, False, from_pos))
        elif to_pyr == 2: 
            rotated_pyramids = list(pyramids)
        elif to_pyr == 3: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 0, True, from_pos), 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
            transformSteps.append((0, True, from_pos))
        elif to_pyr == 4: 
            rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
            transformSteps.append((0, False, from_pos))
        elif to_pyr == 5: 
            rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
    elif from_pyr == 3:
        if to_pyr == 0: 
            rotated_pyramids = rotatePyramids(pyramids, 2, False, from_pos)
            transformSteps.append((2, False, from_pos))
        elif to_pyr == 1: 
            rotated_pyramids = rotatePyramids(pyramids, 2, True, from_pos)
            transformSteps.append((2, True, from_pos))
        elif to_pyr == 2: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 2, True, from_pos), 2, True, from_pos)
            transformSteps.append((2, True, from_pos))
            transformSteps.append((2, True, from_pos))
        elif to_pyr == 3: 
            rotated_pyramids = list(pyramids)
        elif to_pyr == 4: 
            rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
        elif to_pyr == 5: 
            rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
            transformSteps.append((0, False, from_pos))
    elif from_pyr == 4:
        if to_pyr == 0: 
            rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
            transformSteps.append((1, False, from_pos))
        elif to_pyr == 1: 
            rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
        elif to_pyr == 2: 
            rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
        elif to_pyr == 3: 
            rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
            transformSteps.append((0, False, from_pos))
        elif to_pyr == 4: rotated_pyramids = list(pyramids)
        elif to_pyr == 5: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 1, True, from_pos), 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
            transformSteps.append((1, True, from_pos))
    elif from_pyr == 5:
        if to_pyr == 0: 
            rotated_pyramids = rotatePyramids(pyramids, 1, True, from_pos)
            transformSteps.append((1, True, from_pos))
        elif to_pyr == 1: 
            rotated_pyramids = rotatePyramids(pyramids, 1, False, from_pos)
            transformSteps.append((1, False, from_pos))
        elif to_pyr == 2: 
            rotated_pyramids = rotatePyramids(pyramids, 0, False, from_pos)
            transformSteps.append((0, False, from_pos))
        elif to_pyr == 3: 
            rotated_pyramids = rotatePyramids(pyramids, 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
        elif to_pyr == 4: 
            rotated_pyramids = rotatePyramids(rotatePyramids(pyramids, 0, True, from_pos), 0, True, from_pos)
            transformSteps.append((0, True, from_pos))
            transformSteps.append((0, True, from_pos))
        elif to_pyr == 5: rotated_pyramids = list(pyramids)

    transformed = []
    for p in rotated_pyramids:
        new_pos = add_int3((p[0], p[1], p[2]), diff)
        transformed.append((new_pos[0], new_pos[1], new_pos[2], p[3]))
    return transformed

def add_int3(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])

def sub_int3(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


# Pyramid rotation mappings
ROT_MAP_0_TRUE = {0: 0, 1: 1, 2: 5, 3: 4, 4: 2, 5: 3}
ROT_MAP_0_FALSE = {0: 0, 1: 1, 2: 4, 3: 5, 4: 3, 5: 2}
ROT_MAP_1_TRUE = {0: 4, 1: 5, 2: 2, 3: 3, 4: 1, 5: 0}
ROT_MAP_1_FALSE = {0: 5, 1: 4, 2: 2, 3: 3, 4: 0, 5: 1}
ROT_MAP_2_TRUE = {0: 3, 1: 2, 2: 0, 3: 1, 4: 4, 5: 5}
ROT_MAP_2_FALSE = {0: 2, 1: 3, 2: 1, 3: 0, 4: 4, 5: 5}

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

def get_pyramid_vertices(x, y, z, p):
    vertices = set()
    vertices.add((x, y, z))
    if p == 0: # +x
        base_x = x + 0.5
        for dy in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                vertices.add((base_x, y + dy, z + dz))
    elif p == 1: # -x
        base_x = x - 0.5
        for dy in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                vertices.add((base_x, y + dy, z + dz))
    elif p == 2: # +y
        base_y = y + 0.5
        for dx in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                vertices.add((x + dx, base_y, z + dz))
    elif p == 3: # -y
        base_y = y - 0.5
        for dx in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                vertices.add((x + dx, base_y, z + dz))
    elif p == 4: # +z
        base_z = z + 0.5
        for dx in (-0.5, 0.5):
            for dy in (-0.5, 0.5):
                vertices.add((x + dx, y + dy, base_z))
    elif p == 5: # -z
        base_z = z - 0.5
        for dx in (-0.5, 0.5):
            for dy in (-0.5, 0.5):
                vertices.add((x + dx, y + dy, base_z))
    return vertices


def get_base_neighbors_case0():
    target = get_pyramid_vertices(0, 0, 0, 0)
    neighbors = []
    for x in range(2):
        for y in range(-1, 2):
            for z in range(-1, 2):
                for p in range(6):
                    if x == 0 and y == 0 and z == 0 and p == 0:
                        continue
                    if not target.isdisjoint(get_pyramid_vertices(x, y, z, p)):
                        neighbors.append((x, y, z, p))

    # print("Base Neighbors:")
    # for p in neighbors:
    #     print(p)
    return neighbors

BASE_NEIGHBORS_CASE0 = get_base_neighbors_case0()

def calculate_all_neighbor_pyramids(pyramid_cluster: Set[PyramidCoord]) -> Set[PyramidCoord]:
    """
    Calculates all neighbor pyramids for a given cluster of pyramids.

    Args:
        pyramid_cluster: A set of pyramid coordinates.

    Returns:
        A set of neighbor pyramid coordinates.
    """
    pyramid_surround: Set[PyramidCoord] = set()

    base_neighbors_case0 = BASE_NEIGHBORS_CASE0
    


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

def get_basis_rotation(axis, direction):
    """Returns a function that rotates a vector (x,y,z) according to the axis/direction."""
    # Logic matches rotatePyramids implementation
    if axis == 0:
        if direction: return lambda v: (v[0], v[2], -v[1]) # x->x, y->z, z->-y (Code logic)
        else: return lambda v: (v[0], -v[2], v[1])        # x->x, y->-z, z->y
    elif axis == 1:
        if direction: return lambda v: (-v[2], v[1], v[0]) # x->-z, y->y, z->x
        else: return lambda v: (v[2], v[1], -v[0])         # x->z, y->y, z->-x
    elif axis == 2:
        if direction: return lambda v: (v[1], -v[0], v[2]) # x->y, y->-x, z->z
        else: return lambda v: (-v[1], v[0], v[2])         # x->-y, y->x, z->z
    return lambda v: v

CANONICAL_BASES = {}

def init_canonical_bases():
    """Precomputes the basis vectors for all 24 orientations."""
    if CANONICAL_BASES: return

    def sim_rot(basis, axis, direction):
        rot = get_basis_rotation(axis, direction)
        return (rot(basis[0]), rot(basis[1]))

    for r in range(24):
        # Start with Identity Basis: X=(1,0,0), Y=(0,1,0)
        basis = ((1, 0, 0), (0, 1, 0))
        
        # Apply rotations corresponding to create_rotated_pyramid_coords logic
        if r == 0: pass
        elif r == 1: basis = sim_rot(basis, 1, True)
        elif r == 2: basis = sim_rot(sim_rot(basis, 1, True), 1, True)
        elif r == 3: basis = sim_rot(basis, 1, False)
        
        elif r == 4: basis = sim_rot(basis, 2, False)
        elif r == 5: basis = sim_rot(sim_rot(basis, 2, False), 1, True)
        elif r == 6: basis = sim_rot(sim_rot(sim_rot(basis, 2, False), 1, True), 1, True)
        elif r == 7: basis = sim_rot(sim_rot(basis, 2, False), 1, False)
        
        elif r == 8: basis = sim_rot(sim_rot(basis, 0, True), 0, True)
        elif r == 9: basis = sim_rot(sim_rot(sim_rot(basis, 0, True), 0, True), 1, True)
        elif r == 10: basis = sim_rot(sim_rot(sim_rot(sim_rot(basis, 0, True), 0, True), 1, True), 1, True)
        elif r == 11: basis = sim_rot(sim_rot(sim_rot(basis, 0, True), 0, True), 1, False)
        
        elif r == 12: basis = sim_rot(basis, 2, True)
        elif r == 13: basis = sim_rot(sim_rot(basis, 2, True), 1, True)
        elif r == 14: basis = sim_rot(sim_rot(sim_rot(basis, 2, True), 1, True), 1, True)
        elif r == 15: basis = sim_rot(sim_rot(basis, 2, True), 1, False)
        
        elif r == 16: basis = sim_rot(basis, 0, True)
        elif r == 17: basis = sim_rot(sim_rot(basis, 0, True), 1, True)
        elif r == 18: basis = sim_rot(sim_rot(sim_rot(basis, 0, True), 1, True), 1, True)
        elif r == 19: basis = sim_rot(sim_rot(basis, 0, True), 1, False)
        
        elif r == 20: basis = sim_rot(basis, 0, False)
        elif r == 21: basis = sim_rot(sim_rot(basis, 0, False), 1, True)
        elif r == 22: basis = sim_rot(sim_rot(sim_rot(basis, 0, False), 1, True), 1, True)
        elif r == 23: basis = sim_rot(sim_rot(basis, 0, False), 1, False)
        
        CANONICAL_BASES[basis] = r

def get_orientation_from_steps(steps):
    """Calculates the orientation index (0-23) from a list of rotation steps."""
    if not CANONICAL_BASES: init_canonical_bases()
    
    # Start with Identity
    x_vec = (1, 0, 0)
    y_vec = (0, 1, 0)
    
    for axis, direction, _ in steps:
        rot = get_basis_rotation(axis, direction)
        x_vec = rot(x_vec)
        y_vec = rot(y_vec)
    
    return CANONICAL_BASES.get((x_vec, y_vec), -1)

def calculate_all_neighbor_tile_positions(surroundPyramids, tilePyramids):
    neighborTilePositions = []

    # Ensure canonical bases are initialized
    init_canonical_bases()

    for p in surroundPyramids:
        for t in tilePyramids:
            # rotate every tile pyramid to every position in surroundPyramids
            transformSteps = []
            # Pass transformSteps to capture the rotations
            transformedPyramids = transform_pyramids(tilePyramids, t, p, transformSteps)
            
            # Determine the axis for the secondary rotation loop
            axis = 0
            if p[3] == 0 or p[3] == 1: axis = 0
            elif p[3] == 2 or p[3] == 3: axis = 1
            elif p[3] == 4 or p[3] == 5: axis = 2
            
            # Calculate the position of the new tile (relative to 0,0,0)
            # diff was calculated in transform_pyramids as end_p - start_p
            # Since start_p is from the tile at origin, diff is the new center.
            diff = sub_int3((p[0], p[1], p[2]), (t[0], t[1], t[2]))
            
            for rot in range(4):
                # Create a copy of steps for this specific rotation
                current_steps = list(transformSteps)
                
                rotatedPyramids = []
                if rot == 0:
                    rotatedPyramids = transformedPyramids
                elif rot == 1:
                    rotatedPyramids = rotatePyramids(transformedPyramids, axis, True, (p[0], p[1], p[2]))
                    current_steps.append((axis, True, None))
                elif rot == 2:
                    rotatedPyramids = rotatePyramids(rotatePyramids(transformedPyramids, axis, True, (p[0], p[1], p[2])), axis, True, (p[0], p[1], p[2]))
                    current_steps.append((axis, True, None))
                    current_steps.append((axis, True, None))
                elif rot == 3:
                    rotatedPyramids = rotatePyramids(transformedPyramids, axis, False, (p[0], p[1], p[2]))
                    current_steps.append((axis, False, None))

                # Check if this placement overlaps with the original tile
                if not any(tp in tilePyramids for tp in rotatedPyramids):
                    # Calculate orientation index
                    orientation = get_orientation_from_steps(current_steps)
                    # Append (Position, Orientation)
                    # Position is (x, y, z), Orientation is int 0-23
                    neighborTilePositions.append((diff, orientation))
    return neighborTilePositions
    
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
    log_file = open("heesch_solver.log", "w")
    summary_log = open("heesch_solver_summary.log", "w")
    summary_log.write("--- Heesch Solver Summary ---\n")
    summary_log.flush()
    
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
    sys.stdout = Tee(sys.stdout, log_file, summary_log)
    sys.stderr = sys.stdout



    shape_index = 0
    for nrPyramidsInShape in range(1, 3):
        shapes = generate_polypyramids(nrPyramidsInShape)

        for shape in shapes:
            surroundPyramids = calculate_all_neighbor_pyramids(set(shape))
            print(f"Shape: {list(shape)}")
            print(f"Surround: {list(surroundPyramids)}")

            neighborTilePositions = calculate_all_neighbor_tile_positions(surroundPyramids, shape)
            print(f"Nr Neighbor Tile Positions: {len(neighborTilePositions)}")
            print(f"Neighbor Tile Positions: {neighborTilePositions}")

            neighborTilePyramids = set()
            for pos, orientation in neighborTilePositions:
                rotated_pyramids = create_rotated_pyramid_coords(orientation, shape, (0, 0, 0))
                for px, py, pz, pyr in rotated_pyramids:
                    transformed_p = (px + pos[0], py + pos[1], pz + pos[2], pyr)
                    neighborTilePyramids.add(transformed_p)

            print(f"Neighbor Tile Pyramids: {list(neighborTilePyramids)}")