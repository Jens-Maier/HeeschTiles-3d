This script calculates the surrounds of 3D tiles based on a body centered cubic grid. 
It works in the Unity game enging by adding the script to an empty game object. 
You can switch the draw mode from rendering every step or displaying only the current best cluster. 
You can edit the tile in the tile description in the code. Each pyramid has a coordinate and an orientation 0-5. 
( note: Y is up in unity...)
The program automatically switches to the next surround once the current one is found. 
The tile has (at least) three surrounds (found by hand), the program is able to find the first surround and proceeds to the second. 
The program is currently too slow to find the next surround (at least on my computer).
