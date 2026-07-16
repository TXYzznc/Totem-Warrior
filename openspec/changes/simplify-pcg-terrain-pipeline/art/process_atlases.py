from __future__ import annotations

import argparse
import random
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parent
RAW = ROOT / "raw"
ATLASES = RAW / "atlases"
TERRAIN = RAW / "terrain"
DECORATIONS = RAW / "decorations"

DECORATION_SIZES = [
    (160, 160),
    (256, 96),
    (96, 96),
    (128, 64),
    (192, 112),
    (224, 128),
]


def crop_for_grid(image: Image.Image, columns: int, rows: int, force_square_cells: bool) -> Image.Image:
    width, height = image.size
    if force_square_cells:
        cell = min(width // columns, height // rows)
        target_width = cell * columns
        target_height = cell * rows
    else:
        target_width = width - (width % columns)
        target_height = height - (height % rows)

    left = (width - target_width) // 2
    top = (height - target_height) // 2
    return image.crop((left, top, left + target_width, top + target_height))


def split_cells(image: Image.Image, columns: int, rows: int) -> list[Image.Image]:
    cell_width = image.width // columns
    cell_height = image.height // rows
    return [
        image.crop((column * cell_width, row * cell_height, (column + 1) * cell_width, (row + 1) * cell_height))
        for row in range(rows)
        for column in range(columns)
    ]


def split_terrain(source_name: str, terrain_name: str) -> None:
    source = Image.open(ATLASES / source_name).convert("RGB")
    source = crop_for_grid(source, 4, 2, force_square_cells=True)
    output_dir = TERRAIN / terrain_name
    output_dir.mkdir(parents=True, exist_ok=True)
    for index, cell in enumerate(split_cells(source, 4, 2), start=1):
        final = cell.resize((256, 256), Image.Resampling.BOX)
        final.save(output_dir / f"{terrain_name}_{index:02d}.png")


def split_decorations() -> None:
    source = Image.open(ATLASES / "grass_river_deco_atlas_source.png").convert("RGB")
    source = crop_for_grid(source, 3, 2, force_square_cells=False)
    keyed_dir = DECORATIONS / "keyed"
    keyed_dir.mkdir(parents=True, exist_ok=True)
    for index, cell in enumerate(split_cells(source, 3, 2), start=1):
        cell.save(keyed_dir / f"grass_river_deco_{index:02d}_keyed.png")


def split_keyed_decorations(source_name: str, decoration_name: str) -> None:
    """Split an already chroma-keyed 3x2 decoration atlas without synthesizing art."""
    source = Image.open(ATLASES / source_name).convert("RGBA")
    source = crop_for_grid(source, 3, 2, force_square_cells=False)
    keyed_dir = DECORATIONS / "keyed"
    keyed_dir.mkdir(parents=True, exist_ok=True)
    for index, cell in enumerate(split_cells(source, 3, 2), start=1):
        cell.save(keyed_dir / f"{decoration_name}_deco_{index:02d}_keyed.png")


def finalize_keyed_decorations(decoration_name: str) -> None:
    """Normalize six already chroma-keyed model-drawn decorations to varied target sizes."""
    keyed_dir = DECORATIONS / "keyed"
    for index, target_size in enumerate(DECORATION_SIZES, start=1):
        source_path = keyed_dir / f"{decoration_name}_deco_{index:02d}_keyed.png"
        image = Image.open(source_path).convert("RGBA")
        bbox = image.getchannel("A").getbbox()
        if bbox is None:
            raise ValueError(f"Decoration has no opaque subject: {source_path}")
        subject = image.crop(bbox)
        max_width = max(1, target_size[0] - 8)
        max_height = max(1, target_size[1] - 8)
        scale = min(max_width / subject.width, max_height / subject.height)
        subject = subject.resize(
            (max(1, round(subject.width * scale)), max(1, round(subject.height * scale))),
            Image.Resampling.NEAREST,
        )
        final = Image.new("RGBA", target_size, (0, 0, 0, 0))
        final.alpha_composite(subject, ((target_size[0] - subject.width) // 2, (target_size[1] - subject.height) // 2))
        final.save(DECORATIONS / f"{decoration_name}_deco_{index:02d}.png")


def validate_decoration_set(decoration_name: str) -> list[str]:
    failures: list[str] = []
    for index, expected_size in enumerate(DECORATION_SIZES, start=1):
        path = DECORATIONS / f"{decoration_name}_deco_{index:02d}.png"
        image = Image.open(path).convert("RGBA")
        if image.size != expected_size:
            failures.append(f"{path}: expected {expected_size}, got {image.size}")
            continue
        corners = [
            image.getpixel((0, 0))[3],
            image.getpixel((image.width - 1, 0))[3],
            image.getpixel((0, image.height - 1))[3],
            image.getpixel((image.width - 1, image.height - 1))[3],
        ]
        if any(corner != 0 for corner in corners):
            failures.append(f"{path}: non-transparent corner detected")
    return failures


def finalize_decorations() -> None:
    transparent_dir = DECORATIONS / "transparent_cells"
    DECORATIONS.mkdir(parents=True, exist_ok=True)
    for index, target_size in enumerate(DECORATION_SIZES, start=1):
        source_path = transparent_dir / f"grass_river_deco_{index:02d}.png"
        image = Image.open(source_path).convert("RGBA")
        alpha = image.getchannel("A")
        bbox = alpha.getbbox()
        if bbox is None:
            raise ValueError(f"Decoration has no opaque subject: {source_path}")
        subject = image.crop(bbox)

        max_width = max(1, target_size[0] - 8)
        max_height = max(1, target_size[1] - 8)
        scale = min(max_width / subject.width, max_height / subject.height)
        resized_size = (
            max(1, round(subject.width * scale)),
            max(1, round(subject.height * scale)),
        )
        subject = subject.resize(resized_size, Image.Resampling.NEAREST)

        final = Image.new("RGBA", target_size, (0, 0, 0, 0))
        offset = ((target_size[0] - subject.width) // 2, (target_size[1] - subject.height) // 2)
        final.alpha_composite(subject, offset)
        final.save(DECORATIONS / f"grass_river_deco_{index:02d}.png")


def validate(terrain_names: tuple[str, str] = ("grass", "river"), include_decorations: bool = True) -> None:
    failures: list[str] = []
    for terrain_name in terrain_names:
        for index in range(1, 9):
            path = TERRAIN / terrain_name / f"{terrain_name}_{index:02d}.png"
            image = Image.open(path).convert("RGBA")
            if image.size != (256, 256):
                failures.append(f"{path}: expected 256x256, got {image.size}")
                continue
            alpha = image.getchannel("A")
            border_extrema = [
                alpha.crop((0, 0, 256, 1)).getextrema(),
                alpha.crop((0, 255, 256, 256)).getextrema(),
                alpha.crop((0, 0, 1, 256)).getextrema(),
                alpha.crop((255, 0, 256, 256)).getextrema(),
            ]
            if any(extrema != (255, 255) for extrema in border_extrema):
                failures.append(f"{path}: transparent border pixel detected")

    if include_decorations:
        failures.extend(validate_decoration_set("grass_river"))

    if failures:
        raise SystemExit("\n".join(failures))
    print("PASS: 16 opaque 256x256 terrain tiles and 6 transparent decorations validated.")


def compose_preview(
    grass_name: str = "grass",
    river_name: str = "river",
    preview_name: str = "grass_river_pilot_composite.png",
) -> None:
    columns, rows = 8, 6
    tile_size = 256
    rng = random.Random(20260715)
    canvas = Image.new("RGBA", (columns * tile_size, rows * tile_size), (0, 0, 0, 255))
    river_columns = [
        {3, 4},
        {3, 4},
        {2, 3},
        {2, 3},
        {3, 4},
        {3, 4},
    ]

    for row in range(rows):
        for column in range(columns):
            terrain_name = river_name if column in river_columns[row] else grass_name
            variant = rng.randint(1, 8)
            tile = Image.open(TERRAIN / terrain_name / f"{terrain_name}_{variant:02d}.png").convert("RGBA")
            canvas.alpha_composite(tile, (column * tile_size, row * tile_size))

    placements = [
        (1, 3 * tile_size - 80, 110, False),
        (2, 5 * tile_size - 40, 300, True),
        (3, 3 * tile_size - 48, 500, False),
        (4, 2 * tile_size - 55, 700, True),
        (5, 4 * tile_size - 100, 930, False),
        (6, 3 * tile_size - 120, 1190, False),
        (2, 5 * tile_size - 45, 1290, True),
        (1, 2 * tile_size - 75, 810, False),
    ]
    for decoration_id, x, y, rotate in placements:
        decoration = Image.open(DECORATIONS / f"grass_river_deco_{decoration_id:02d}.png").convert("RGBA")
        if rotate:
            decoration = decoration.rotate(90, expand=True, resample=Image.Resampling.NEAREST)
        canvas.alpha_composite(decoration, (x, y))

    preview_dir = ROOT / "previews"
    preview_dir.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(preview_dir / preview_name)


def compose_swamp_preview() -> None:
    """Create a deterministic review sheet for the four VIRUS_SWAMP terrain types."""
    layout = [
        ["grass_v2", "grass_v2", "river_v2", "river_v2", "swamp_corruption", "swamp_corruption", "grass_v2", "grass_v2"],
        ["grass_v2", "swamp_mud", "swamp_mud", "river_v2", "river_v2", "swamp_corruption", "swamp_corruption", "grass_v2"],
        ["grass_v2", "swamp_mud", "swamp_corruption", "swamp_corruption", "river_v2", "river_v2", "swamp_corruption", "grass_v2"],
        ["grass_v2", "grass_v2", "swamp_mud", "swamp_corruption", "swamp_mud", "river_v2", "river_v2", "grass_v2"],
        ["grass_v2", "swamp_mud", "swamp_mud", "swamp_mud", "swamp_corruption", "swamp_corruption", "river_v2", "grass_v2"],
        ["grass_v2", "grass_v2", "grass_v2", "swamp_mud", "swamp_mud", "river_v2", "river_v2", "grass_v2"],
    ]
    pairs = {
        frozenset(("grass_v2", "river_v2")): "grass_river",
        frozenset(("grass_v2", "swamp_mud")): "swamp_grass_mud_green",
        frozenset(("grass_v2", "swamp_corruption")): "swamp_grass_corruption_green",
        frozenset(("swamp_mud", "river_v2")): "swamp_mud_water_green",
        frozenset(("swamp_mud", "swamp_corruption")): "swamp_mud_corruption_green",
        frozenset(("swamp_corruption", "river_v2")): "swamp_corruption_water_green",
    }
    tile_size = 256
    rng = random.Random(20260715)
    canvas = Image.new("RGBA", (len(layout[0]) * tile_size, len(layout) * tile_size), (0, 0, 0, 255))
    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            variant = rng.randint(1, 8)
            tile = Image.open(TERRAIN / terrain_name / f"{terrain_name}_{variant:02d}.png").convert("RGBA")
            canvas.alpha_composite(tile, (column * tile_size, row * tile_size))

    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            for dx, dy, rotate in ((1, 0, False), (0, 1, True)):
                next_column, next_row = column + dx, row + dy
                if next_row >= len(layout) or next_column >= len(terrain_row):
                    continue
                other = layout[next_row][next_column]
                decoration_name = pairs.get(frozenset((terrain_name, other)))
                if decoration_name is None:
                    continue
                decoration_id = rng.randint(1, 6)
                decoration = Image.open(
                    DECORATIONS / f"{decoration_name}_deco_{decoration_id:02d}.png"
                ).convert("RGBA")
                if rotate:
                    decoration = decoration.rotate(90, expand=True, resample=Image.Resampling.NEAREST)
                center_x = (column + 1) * tile_size if dx else column * tile_size + tile_size // 2
                center_y = row * tile_size + tile_size // 2 if dx else (row + 1) * tile_size
                canvas.alpha_composite(decoration, (center_x - decoration.width // 2, center_y - decoration.height // 2))

    preview_dir = ROOT / "previews"
    preview_dir.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(preview_dir / "virus_swamp_pilot_composite.png")


def compose_ruins_preview() -> None:
    """Create a deterministic review sheet for the four AI_RUINS terrain types."""
    layout = [
        ["ruins_floor", "ruins_floor", "ruins_service_metal", "ruins_service_metal", "ruins_coolant_water", "ruins_coolant_water", "ruins_floor", "ruins_floor"],
        ["ruins_floor", "ruins_reclaimed_growth", "ruins_reclaimed_growth", "ruins_service_metal", "ruins_service_metal", "ruins_coolant_water", "ruins_coolant_water", "ruins_floor"],
        ["ruins_floor", "ruins_reclaimed_growth", "ruins_floor", "ruins_floor", "ruins_coolant_water", "ruins_coolant_water", "ruins_reclaimed_growth", "ruins_floor"],
        ["ruins_floor", "ruins_floor", "ruins_service_metal", "ruins_service_metal", "ruins_floor", "ruins_coolant_water", "ruins_coolant_water", "ruins_floor"],
        ["ruins_floor", "ruins_reclaimed_growth", "ruins_reclaimed_growth", "ruins_floor", "ruins_floor", "ruins_floor", "ruins_coolant_water", "ruins_floor"],
        ["ruins_floor", "ruins_floor", "ruins_floor", "ruins_service_metal", "ruins_service_metal", "ruins_coolant_water", "ruins_coolant_water", "ruins_floor"],
    ]
    pairs = {
        frozenset(("ruins_floor", "ruins_service_metal")): "ruins_floor_metal",
        frozenset(("ruins_floor", "ruins_reclaimed_growth")): "ruins_floor_growth",
        frozenset(("ruins_floor", "ruins_coolant_water")): "ruins_floor_water",
        frozenset(("ruins_service_metal", "ruins_coolant_water")): "ruins_metal_water",
        frozenset(("ruins_reclaimed_growth", "ruins_coolant_water")): "ruins_growth_water",
    }
    tile_size = 256
    rng = random.Random(20260716)
    canvas = Image.new("RGBA", (len(layout[0]) * tile_size, len(layout) * tile_size), (0, 0, 0, 255))
    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            variant = rng.randint(1, 8)
            tile = Image.open(TERRAIN / terrain_name / f"{terrain_name}_{variant:02d}.png").convert("RGBA")
            canvas.alpha_composite(tile, (column * tile_size, row * tile_size))

    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            for dx, dy, rotate in ((1, 0, False), (0, 1, True)):
                next_column, next_row = column + dx, row + dy
                if next_row >= len(layout) or next_column >= len(terrain_row):
                    continue
                decoration_name = pairs.get(frozenset((terrain_name, layout[next_row][next_column])))
                if decoration_name is None:
                    continue
                decoration = Image.open(
                    DECORATIONS / f"{decoration_name}_deco_{rng.randint(1, 6):02d}.png"
                ).convert("RGBA")
                if rotate:
                    decoration = decoration.rotate(90, expand=True, resample=Image.Resampling.NEAREST)
                center_x = (column + 1) * tile_size if dx else column * tile_size + tile_size // 2
                center_y = row * tile_size + tile_size // 2 if dx else (row + 1) * tile_size
                canvas.alpha_composite(decoration, (center_x - decoration.width // 2, center_y - decoration.height // 2))

    preview_dir = ROOT / "previews"
    preview_dir.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(preview_dir / "ai_ruins_pilot_composite.png")


def compose_hive_preview() -> None:
    """Create a deterministic review sheet for the four ALIEN_HIVE terrain types."""
    layout = [
        ["hive_chitin", "hive_chitin", "hive_membrane", "hive_membrane", "hive_acid_pool", "hive_acid_pool", "hive_chitin", "hive_chitin"],
        ["hive_chitin", "hive_resin_crust", "hive_resin_crust", "hive_membrane", "hive_membrane", "hive_acid_pool", "hive_acid_pool", "hive_chitin"],
        ["hive_chitin", "hive_resin_crust", "hive_chitin", "hive_chitin", "hive_acid_pool", "hive_acid_pool", "hive_resin_crust", "hive_chitin"],
        ["hive_chitin", "hive_chitin", "hive_membrane", "hive_membrane", "hive_chitin", "hive_acid_pool", "hive_acid_pool", "hive_chitin"],
        ["hive_chitin", "hive_resin_crust", "hive_resin_crust", "hive_chitin", "hive_chitin", "hive_chitin", "hive_acid_pool", "hive_chitin"],
        ["hive_chitin", "hive_chitin", "hive_chitin", "hive_membrane", "hive_membrane", "hive_acid_pool", "hive_acid_pool", "hive_chitin"],
    ]
    pairs = {
        frozenset(("hive_chitin", "hive_membrane")): "hive_chitin_membrane",
        frozenset(("hive_chitin", "hive_resin_crust")): "hive_chitin_resin",
        frozenset(("hive_chitin", "hive_acid_pool")): "hive_chitin_acid",
        frozenset(("hive_membrane", "hive_acid_pool")): "hive_membrane_acid",
        frozenset(("hive_resin_crust", "hive_acid_pool")): "hive_resin_acid",
    }
    tile_size = 256
    rng = random.Random(20260717)
    canvas = Image.new("RGBA", (len(layout[0]) * tile_size, len(layout) * tile_size), (0, 0, 0, 255))
    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            variant = rng.randint(1, 8)
            tile = Image.open(TERRAIN / terrain_name / f"{terrain_name}_{variant:02d}.png").convert("RGBA")
            canvas.alpha_composite(tile, (column * tile_size, row * tile_size))

    for row, terrain_row in enumerate(layout):
        for column, terrain_name in enumerate(terrain_row):
            for dx, dy, rotate in ((1, 0, False), (0, 1, True)):
                next_column, next_row = column + dx, row + dy
                if next_row >= len(layout) or next_column >= len(terrain_row):
                    continue
                decoration_name = pairs.get(frozenset((terrain_name, layout[next_row][next_column])))
                if decoration_name is None:
                    continue
                decoration = Image.open(
                    DECORATIONS / f"{decoration_name}_deco_{rng.randint(1, 6):02d}.png"
                ).convert("RGBA")
                if rotate:
                    decoration = decoration.rotate(90, expand=True, resample=Image.Resampling.NEAREST)
                center_x = (column + 1) * tile_size if dx else column * tile_size + tile_size // 2
                center_y = row * tile_size + tile_size // 2 if dx else (row + 1) * tile_size
                canvas.alpha_composite(decoration, (center_x - decoration.width // 2, center_y - decoration.height // 2))

    preview_dir = ROOT / "previews"
    preview_dir.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(preview_dir / "alien_hive_pilot_composite.png")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "phase",
        choices=("split", "split-decorations", "finalize", "finalize-keyed", "validate", "preview", "swamp-preview", "ruins-preview", "hive-preview"),
    )
    parser.add_argument("--grass-source", default="grass_atlas_source.png")
    parser.add_argument("--grass-name", default="grass")
    parser.add_argument("--river-source", default="river_atlas_source.png")
    parser.add_argument("--river-name", default="river")
    parser.add_argument("--preview-name", default="grass_river_pilot_composite.png")
    parser.add_argument("--skip-decorations", action="store_true")
    parser.add_argument("--deco-source")
    parser.add_argument("--deco-name")
    parser.add_argument("--validate-decoration", action="append", default=[])
    args = parser.parse_args()
    if args.phase == "split":
        split_terrain(args.grass_source, args.grass_name)
        split_terrain(args.river_source, args.river_name)
        if not args.skip_decorations:
            split_decorations()
    elif args.phase == "split-decorations":
        if not args.deco_source or not args.deco_name:
            raise SystemExit("--deco-source and --deco-name are required for split-decorations")
        split_keyed_decorations(args.deco_source, args.deco_name)
    elif args.phase == "finalize":
        finalize_decorations()
    elif args.phase == "finalize-keyed":
        if not args.deco_name:
            raise SystemExit("--deco-name is required for finalize-keyed")
        finalize_keyed_decorations(args.deco_name)
    elif args.phase == "validate":
        validate((args.grass_name, args.river_name), include_decorations=not args.skip_decorations)
        decoration_failures = []
        for decoration_name in args.validate_decoration:
            decoration_failures.extend(validate_decoration_set(decoration_name))
        if decoration_failures:
            raise SystemExit("\n".join(decoration_failures))
        if args.validate_decoration:
            print(f"PASS: {len(args.validate_decoration)} additional decoration sets validated.")
    elif args.phase == "swamp-preview":
        compose_swamp_preview()
    elif args.phase == "ruins-preview":
        compose_ruins_preview()
    elif args.phase == "hive-preview":
        compose_hive_preview()
    else:
        compose_preview(args.grass_name, args.river_name, args.preview_name)


if __name__ == "__main__":
    main()
