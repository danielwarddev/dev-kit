# Font setup

Text uses bundled Python/Pillow rasterization, then composites the resulting image
into an Aseprite cel. See `text.md` for calls. Install Pillow only when needed;
see `execution.md`.

Discover fonts with `text.list_text_fonts()`. A font argument can be a system font
name, a direct `.ttf`/`.otf` path, or a bitmap font directory. The original custom
font location `~/.aseprite-mcp/fonts` is retained for compatibility; using that
directory does **not** require the MCP server.

Bitmap fonts are appropriate for native pixel art: `size` is an integer scale.
For TrueType fonts, `size` is pixel size. Antialiasing defaults off.
Call `measure_text` before drawing when placement depends on exact dimensions.

A bitmap font directory contains `font.json` and its sheet images:

```json
{
  "name": "my-font",
  "letter_gap": 1,
  "space_width": 3,
  "sheets": [{
    "file": "sheet.png",
    "cell_w": 8,
    "cell_h": 8,
    "ascent": 7,
    "chars": ["ABCD", "EFGH"]
  }]
}
```

Each `chars` string represents a sheet row. `ascent` is the baseline measured from
the cell top. Optional `origin: [x, y]` offsets the cell grid.
`ink_rule: "alpha"` uses transparency (default); `"dark"` reads dark pixels on
opaque backgrounds. `advance: "ink"` measures glyph ink (default); `"box"` uses
the cell box. Multiple sheets can have different ascents/cell sizes and share a
baseline; the first sheet containing a codepoint wins.

Optional `overrides` uses decimal codepoint keys:

```json
{
  "86": {"ascent": 3, "rows": ["#.#", "#.#", ".#."]}
}
```

In override rows, `#` is ink and `.` is transparent. Only redistribute font assets
when their licenses permit it; the skill does not bundle third-party fonts.
