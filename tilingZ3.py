from ortools.sat.python import cp_model

def solve_domino_tiling(rows, cols):
    """
    Solves a tiling problem for a rows x cols grid using 1x2 dominoes.
    """
    model = cp_model.CpModel()

    # --- 1. Define Variables ---
    # We create a boolean variable for every possible position of a domino.
    
    # h[(r, c)] is True if a horizontal domino starts at (r, c) covering (r, c+1)
    h_placements = {}
    for r in range(rows):
        for c in range(cols - 1):
            h_placements[(r, c)] = model.NewBoolVar(f'h_{r}_{c}')

    # v[(r, c)] is True if a vertical domino starts at (r, c) covering (r+1, c)
    v_placements = {}
    for r in range(rows - 1):
        for c in range(cols):
            v_placements[(r, c)] = model.NewBoolVar(f'v_{r}_{c}')

    # --- 2. Define Constraints ---
    # Every single cell (r, c) on the grid must be covered by EXACTLY one domino.
    
    for r in range(rows):
        for c in range(cols):
            potential_covers = []

            # 1. Can be covered by a Horizontal domino starting here
            if (r, c) in h_placements:
                potential_covers.append(h_placements[(r, c)])
            
            # 2. Can be covered by a Horizontal domino starting to the left
            if (r, c - 1) in h_placements:
                potential_covers.append(h_placements[(r, c - 1)])

            # 3. Can be covered by a Vertical domino starting here
            if (r, c) in v_placements:
                potential_covers.append(v_placements[(r, c)])

            # 4. Can be covered by a Vertical domino starting above
            if (r - 1, c) in v_placements:
                potential_covers.append(v_placements[(r - 1, c)])

            # The sum of boolean variables (0 or 1) covering this cell must be exactly 1
            model.Add(sum(potential_covers) == 1)

    # --- 3. Solve ---
    solver = cp_model.CpSolver()
    # Optional: Set number of threads for parallel search
    solver.parameters.num_search_workers = 8 
    
    status = solver.Solve(model)

    # --- 4. Output Results ---
    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        print(f"Solution found for {rows}x{cols} grid:")
        print_grid_solution(rows, cols, solver, h_placements, v_placements)
    else:
        print("No solution found (Grid might have odd number of cells).")

def print_grid_solution(rows, cols, solver, h_placements, v_placements):
    """Visualizes the grid solution using ASCII art."""
    # Create a grid to store characters
    display_grid = [[' ' for _ in range(cols)] for _ in range(rows)]

    for (r, c), var in h_placements.items():
        if solver.Value(var):
            display_grid[r][c] = '<'
            display_grid[r][c+1] = '>'
    
    for (r, c), var in v_placements.items():
        if solver.Value(var):
            display_grid[r][c] = '^'
            display_grid[r+1][c] = 'v'

    for row in display_grid:
        print("".join(row))

if __name__ == '__main__':
    # Try an 8x8 chessboard
    solve_domino_tiling(8, 8)
