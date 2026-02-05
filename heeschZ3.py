from ortools.sat.python import cp_model

def generateTilePyramids(x, y, z, rot):
    pyramids = []
    # TODO: tile description... rotation...
    # # -> tile description (all rotations...)
    pyramids.append((x, y, z, rot))
    return pyramids

def calculateOverlap(clusterPyramids, x, y, z, rot):
    tilePyramids = generateTilePyramids(x, y, z, rot)
    for pyramid in tilePyramids:
        if pyramid in clusterPyramids:
            return True
    return False



def solve_surround():
    model = cp_model.CpModel()

    clusterPyramids = generateTilePyramids(0, 0, 0, 0)

    grid = {}
    for x in range(-6, 7):
        for y in range(-5, 6):
            for z in range(-5, 6):
                for pyramid in range(6):
                    grid[(x, y, z, pyramid)] = False

    # --- 1. Defnie Variables ---
    # boolean variable for every possible tile position

    placements = {} # -> all possible tile positions of surround
    for x in range(-4, 5):
        for y in range(-3, 4):
            for z in range(-3, 4):
                for rot in range(24):
                    if calculateOverlap(clusterPyramids, x, y, z, rot) == False:
                        placements[(x, y, z, rot)] = model.NewBoolVar(f'x{x}_y{y}_z{z}_r{rot}')

                        # The sum of boolean variables (0 or 1) covering this cell must be exactly 1
                        model.Add(sum(placements) == 1)




    # --- 2. Define Constraints ---
    


    # --- 3. Solve ---


    # --- 4. Output Results ---



    if __name__ == '__main__':
        solve_surround()