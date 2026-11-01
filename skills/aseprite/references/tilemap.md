# Tilemap operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import tilemap
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## create_tilemap_layer

```python
async def create_tilemap_layer(filename: str, layer_name: str, tile_width: int, tile_height: int) -> str:
```

Create a tilemap layer with its own tileset.

Sets the sprite grid to the tile size and adds a tilemap layer.
Tile index 0 is always the empty tile; add real tiles with
draw_on_tile (which creates tiles on demand).

Args:
    filename: Aseprite file to modify
    layer_name: Name for the new tilemap layer
    tile_width: Tile width in pixels
    tile_height: Tile height in pixels

## draw_on_tile

```python
async def draw_on_tile(filename: str, layer_name: str, tile_index: int, pixels: List[Dict[str, Any]]) -> str:
```

Draw pixels onto a tile in a tilemap layer's tileset.

Coordinates are tile-local (0,0 = tile top-left). When tile_index
equals the current tile count, a new tile is appended automatically.

Args:
    filename: Aseprite file to modify
    layer_name: Tilemap layer whose tileset to edit
    tile_index: Tile to draw on (1 = first real tile; 0 is reserved
        for the empty tile and cannot be drawn on)
    pixels: List of {"x": int, "y": int, "color": "#RRGGBB"}

## set_tiles

```python
async def set_tiles(filename: str, layer_name: str, frame_index: int, tiles: List[Dict[str, Any]]) -> str:
```

Place tiles on a tilemap layer by grid position.

Args:
    filename: Aseprite file to modify
    layer_name: Tilemap layer to edit
    frame_index: Frame index starting at 1
    tiles: List of {"col": int, "row": int, "tile_index": int}
        (col/row are grid coordinates; tile_index 0 clears the cell)

## get_tile_at

```python
async def get_tile_at(filename: str, layer_name: str, frame_index: int, col: int, row: int) -> str:
```

Read which tile is placed at a grid position.

Args:
    filename: Aseprite file to read
    layer_name: Tilemap layer to read
    frame_index: Frame index starting at 1
    col: Grid column (0-based)
    row: Grid row (0-based)

Returns:
    JSON with {col, row, tile_index} (0 = empty).

## get_tilemap_info

```python
async def get_tilemap_info(filename: str, layer_name: str) -> str:
```

Get tilemap layer info: tile size, tile count, and map dimensions.

Args:
    filename: Aseprite file to read
    layer_name: Tilemap layer to inspect

Returns:
    JSON with tile_width, tile_height, tile_count (real tiles,
    excluding the empty tile 0), map_cols, map_rows.
