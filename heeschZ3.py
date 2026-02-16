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

def calculate_all_neighbor_tile_positions(surroundPyramids, tilePyramids, forbiddenPyramids=None):
    neighborTilePositions = set()

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
            # diff = sub_int3((p[0], p[1], p[2]), (t[0], t[1], t[2]))
            
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
                if any(tp in tilePyramids for tp in rotatedPyramids):
                    continue

                if forbiddenPyramids and any(tp in forbiddenPyramids for tp in rotatedPyramids):
                    continue


                # Calculate orientation index
                orientation = get_orientation_from_steps(current_steps)
                
                ref_shape = create_rotated_pyramid_coords(orientation, tilePyramids, (0,0,0))
                real_pos = sub_int3(rotatedPyramids[0], ref_shape[0])

                # Append (Position, Orientation)
                # Position is (x, y, z), Orientation is int 0-23
                neighborTilePositions.add((real_pos, orientation))

    return list(neighborTilePositions)
    
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

def get_transformed_pyramids(base_shape, position, orientation):
    rotated = create_rotated_pyramid_coords(orientation, base_shape, (0,0,0))
    transformed = []
    for px, py, pz, pyr in rotated:
        transformed.append((px + position[0], py + position[1], pz + position[2], pyr))
    return transformed

def solve_monolithic(shape_index, search_surrounds, base_shape, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, previous_solution=None):
    
    model = cp_model.CpModel()

    # 1. Setup Grid and Center
    center_cells = set(base_shape)

    center_neighbors_set = calculate_all_neighbor_pyramids(center_cells)
    
    # 2. Generate Tile Variables
    s1_placements = {}
    s2_placements = {}
    s3_placements = {}

    cell_covered_by_s1 = defaultdict(list)
    cell_covered_by_s2 = defaultdict(list)
    cell_covered_by_s3 = defaultdict(list)
    
    # Create S1 variables
    for (pos, rot) in neighborTilePositionsS1:
        cells = get_transformed_pyramids(base_shape, pos, rot)
        s1_var = model.NewBoolVar(f's1_{pos}_{rot}')
        s1_placements[(pos, rot)] = s1_var
        for c in cells:
            cell_covered_by_s1[c].append(s1_var)

    if search_surrounds >= 2:
        # Create S2 variables
        for (pos, rot) in neighborTilePositionsS2:
            cells = get_transformed_pyramids(base_shape, pos, rot)
            s2_var = model.NewBoolVar(f's2_{pos}_{rot}')
            s2_placements[(pos, rot)] = s2_var
            for c in cells:
                cell_covered_by_s2[c].append(s2_var)
    
    if search_surrounds >= 3:
        # Create S3 variables
        for (pos, rot) in neighborTilePositionsS3:
            cells = get_transformed_pyramids(base_shape, pos, rot)
            s3_var = model.NewBoolVar(f's3_{pos}_{rot}')
            s3_placements[(pos, rot)] = s3_var
            for c in cells:
                cell_covered_by_s3[c].append(s3_var)

    


    

    # --- Apply Hints from Previous Solution ---
    if previous_solution:
        print("Applying hints from previous solution...")
        if 's1' in previous_solution:
            for placement_key in previous_solution['s1']:
                if placement_key in s1_placements:
                    model.AddHint(s1_placements[placement_key], 1)
        
        if search_surrounds >= 2 and 's2' in previous_solution:
            for placement_key in previous_solution['s2']:
                if placement_key in s2_placements:
                    model.AddHint(s2_placements[placement_key], 1)


    print("Testing Shape ", end="")
    if shape_index is not None:
        print(f"{shape_index}")
    else:
        print("")
    print(f"Pyramids: {base_shape}")
    print(f"Coronas: {search_surrounds}")
    print(f"S1 positions: {len(s1_placements)}")
    if search_surrounds >= 2:
        print(f"S2 positions: {len(s2_placements)}")
    if search_surrounds == 3:
        print(f"S3 positions: {len(s3_placements)}")


    #5. Constraints
    
    all_cells = set(cell_covered_by_s1.keys())
    if search_surrounds >= 2:
        all_cells.update(cell_covered_by_s2.keys())
    if search_surrounds >= 3:
        all_cells.update(cell_covered_by_s3.keys())

    # A. Disjointness
    # For every cell, sum(s1) + sum(s2) <= 1
    for c in all_cells:
        if c in center_cells: continue
        
        s1_cov = cell_covered_by_s1.get(c, [])
        s2_cov = cell_covered_by_s2.get(c, [])
        s3_cov = cell_covered_by_s3.get(c, [])
        
        all_cov = s1_cov + s2_cov + s3_cov
        if len(all_cov) > 1:
            model.Add(sum(all_cov) <= 1)

    print("Generated disjointness constraints.")

    # B. S1 Surrounds Center
    # All neighbors of Center must be covered by S1
    for c in center_neighbors_set:
        if c in cell_covered_by_s1:
            model.Add(sum(cell_covered_by_s1[c]) == 1)
        else:
            print(f"Warning: Center neighbor {c} cannot be covered by any tile.")
            print("Solver Status: INFEASIBLE (Trivial)")
            print(f"No solution found: Center neighbor {c} cannot be covered (Trivial INFEASIBLE).\n")
            return
    print("Generated surrounds center constraints.")

    # C. S2 Surrounds S1
    # Logic: If any neighbor of cell c is covered by S1, then c must be covered by (S1 or S2).
    # This forces S2 to fill all gaps around S1.
    if search_surrounds >= 2:
        check_set = set(cell_covered_by_s1.keys())
        check_set.update(calculate_all_neighbor_pyramids(check_set))

        for c in check_set:
            if c in center_cells: continue

            neighbors = calculate_all_neighbor_pyramids({c})

            # Collect all S1 variables from all neighbors
            neighbor_s1_vars = []
            for n in neighbors:
                neighbor_s1_vars.extend(cell_covered_by_s1.get(n, []))

            if not neighbor_s1_vars: continue

            # Optimization: Use Boolean Logic instead of Linear Arithmetic
            # Logic: If any neighbor is S1, then c must be covered by (S1 or S2).
            # Equivalent to: neighbor_is_s1 IMPLIES (c_is_s1 OR c_is_s2)
            target_literals = cell_covered_by_s1.get(c, []) + cell_covered_by_s2.get(c, [])
            for n_var in neighbor_s1_vars:
                model.AddBoolOr([n_var.Not()] + target_literals)
        print("Generated S2 surrounds S1 constraints.")

    if search_surrounds == 3:
        # D. S3 Surrounds S2
        check_set = set(cell_covered_by_s2.keys())
        check_set.update(calculate_all_neighbor_pyramids(check_set))

        for c in check_set:
            if c in center_cells: continue
                
            neighbors = calculate_all_neighbor_pyramids({c})
                
            # Collect all S2 variables from all neighbors
            neighbor_s2_vars = []
            for n in neighbors:
                neighbor_s2_vars.extend(cell_covered_by_s2.get(n, []))
                
            if not neighbor_s2_vars: continue
                
            # Logic: If any neighbor is S2, then c must be covered by (S1 or S2 or S3).
            target_literals = cell_covered_by_s1.get(c, []) + cell_covered_by_s2.get(c, []) + cell_covered_by_s3.get(c, [])
            for n_var in neighbor_s2_vars:
                model.AddBoolOr([n_var.Not()] + target_literals)
        print("Generated S3 surrounds S2 constraints.")

    print("generated model. Starting solver...")

    # 6. Solve
    solver = cp_model.CpSolver()
    solver.parameters.num_search_workers = 8
    solver.parameters.max_time_in_seconds = 3600
    solver.parameters.log_search_progress = False
    
    print(f"Solving monolithic model for {search_surrounds} corona(s)...")
    status = solver.Solve(model)

    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print("Solution found!")
        s1_tiles = [key for key, value in s1_placements.items() if solver.Value(value)] # key: ((x, y, z), rot)
        if search_surrounds >= 2:
            s2_tiles = [key for key, value in s2_placements.items() if solver.Value(value)]
            if search_surrounds == 3:
                s3_tiles = [key for key, value in s3_placements.items() if solver.Value(value)]
        
        with open("all_solutions_heesch1_tile.txt", "a") as f:
            f.write("--- Monolithic Solution ---\n")
            f.write("Corona 1:\n")
            for t in s1_tiles:
                #print(f"S1: {t}")
                f.write(f"{t}\n")
            
            if search_surrounds >= 2:
                f.write("Corona 2:\n")
                for t in s2_tiles:
                    #print(f"S2: {t}")
                    f.write(f"{t}\n")

            if search_surrounds == 3:
                f.write("Corona 3:\n")
                for t in s3_tiles:
                    #print(f"S3: {t}")
                    f.write(f"{t}\n")
        
        print(f"Solver Status: {solver.StatusName(status)}")
        print("Solution Found:")
        print(f"S1: {s1_tiles}")
        if search_surrounds >= 2:
            print(f"S2: {s2_tiles}")
        if search_surrounds == 3:
            print(f"S3: {s3_tiles}")
        print("")
        
        # Return the solution dictionary for the next iteration
        solution_data = {'s1': s1_tiles}
        if search_surrounds >= 2:
            solution_data['s2'] = s2_tiles
        if search_surrounds == 3:
            solution_data['s3'] = s3_tiles
        return solution_data

    elif status == cp_model.INFEASIBLE:
        print("No solution found: INFEASIBLE. The solver proved no solution exists within the constraints.")
        print(f"Solver Status: {solver.StatusName(status)}")
        print("No solution found: INFEASIBLE\n")
        return None
    elif status == cp_model.UNKNOWN:
        print("No solution found: UNKNOWN. The solver reached the time limit (timeout) without finding a solution.")
        print(f"Solver Status: {solver.StatusName(status)}")
        print("No solution found: UNKNOWN (Timeout)\n")
        return None
    else:
        print(f"No solution found. Status: {status}")
        print(f"Solver Status: {solver.StatusName(status)}")
        print(f"No solution found. Status: {status}\n")
        return None



if __name__ == '__main__':
    log_file = open("heesch_solver.txt", "w")
    summary_log = open("heesch_solver_summary.txt", "w")
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
    for nrPyramidsInShape in range(6, 7):
        shapes = generate_polypyramids(nrPyramidsInShape)

        for shape in shapes:
            surroundPyramids = calculate_all_neighbor_pyramids(set(shape))
            print(f"Shape: {list(shape)}")
            print(f"Surround: {list(surroundPyramids)}")

            # ---------------- S1 ----------------
            neighborTilePositionsS1 = calculate_all_neighbor_tile_positions(surroundPyramids, shape)
            print(f"Nr Neighbor Tile Positions S1: {len(neighborTilePositionsS1)}")
            #print(f"Neighbor Tile Positions S1: {neighborTilePositionsS1}")

            neighborTilePyramidsS1 = set()
            for pos, orientation in neighborTilePositionsS1:
                rotated_pyramids = create_rotated_pyramid_coords(orientation, shape, (0, 0, 0))
                for px, py, pz, pyr in rotated_pyramids:
                    transformed_p = (px + pos[0], py + pos[1], pz + pos[2], pyr)
                    neighborTilePyramidsS1.add(transformed_p)

            #print(f"Neighbor Tile Pyramids: {list(neighborTilePyramidsS1)}")

            # ---------------- S2 ----------------
            surroundS1 = calculate_all_neighbor_pyramids(neighborTilePyramidsS1)
            neighborTilePositionsS2 = calculate_all_neighbor_tile_positions(surroundS1, shape, forbiddenPyramids = surroundPyramids)
            print(f"Nr Neighbor Tile Positions S2: {len(neighborTilePositionsS2)}")
            #print(f"Neighbor Tile Positions S2: {neighborTilePositionsS2}")

            neighborTilePyramidsS2 = set()
            for pos, orientation in neighborTilePositionsS2:
                rotated_pyramids = create_rotated_pyramid_coords(orientation, shape, (0, 0, 0))
                for px, py, pz, pyr in rotated_pyramids:
                    transformed_p = (px + pos[0], py + pos[1], pz + pos[2], pyr)
                    neighborTilePyramidsS2.add(transformed_p)

            #print(f"Neighbor Tile Pyramids: {list(neighborTilePyramidsS2)}")

            # ---------------- S3 ----------------
            surroundS2 = calculate_all_neighbor_pyramids(neighborTilePyramidsS2)
            neighborTilePositionsS3 = calculate_all_neighbor_tile_positions(surroundS2, shape, forbiddenPyramids = surroundPyramids)
            print(f"Nr Neighbor Tile Positions S3: {len(neighborTilePositionsS3)}")
            #print(f"Neighbor Tile Positions S3: {neighborTilePositionsS3}")


            # --- find all solutions ---
            print(f"Checking shape {shape_index}...")
            search_surrounds = 1
            solution = solve_monolithic(shape_index, search_surrounds, shape, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3)
            if solution:
                search_surrounds += 1
                solution = solve_monolithic(shape_index, search_surrounds, shape, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, previous_solution=solution)
                if solution:
                    search_surrounds += 1
                    solve_monolithic(shape_index, search_surrounds, shape, neighborTilePositionsS1, neighborTilePositionsS2, neighborTilePositionsS3, previous_solution=solution)
            shape_index += 1
            