"""Check authored School geometry without Unity. Run from any working directory.

This is a static scene-data check, not a substitute for Unity physics/play testing.
The Unity build callback separately exercises the actual pickup factory and rays.
"""
from functools import lru_cache
from itertools import product
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parents[1]
scene = (ROOT / "Assets/Scene/School.unity").read_text()
parts = re.split(r"^--- !u!(\d+) &(\d+).*\n", scene, flags=re.M)
docs = {int(parts[i + 1]): (int(parts[i]), parts[i + 2])
        for i in range(1, len(parts), 3)}


def field(obj, name):
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", docs[obj][1], re.M)
    assert match, (obj, name)
    return match[1]


def ref(obj, name):
    return int(re.search(r"fileID: (\d+)", field(obj, name))[1])


def vector(text):
    return [float(value) for value in re.findall(r"[xyzw]: ([-+\d.eE]+)", text)]


transforms = {ref(obj, "m_GameObject"): obj for obj, (kind, _) in docs.items() if kind == 4}


@lru_cache(None)
def world(transform):
    x, y, z, w = vector(field(transform, "m_LocalRotation"))
    norm = math.sqrt(x*x + y*y + z*z + w*w)
    x, y, z, w = (value / norm for value in (x, y, z, w))
    rotation = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
    scale = vector(field(transform, "m_LocalScale"))
    position = vector(field(transform, "m_LocalPosition"))
    local = [[rotation[i][j]*scale[j] for j in range(3)] + [position[i]] for i in range(3)]
    local.append([0, 0, 0, 1])
    parent = ref(transform, "m_Father")
    if not parent:
        return local
    matrix = world(parent)
    return [[sum(matrix[i][k]*local[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def point(transform, position):
    matrix = world(transform)
    return [sum(matrix[i][k]*position[k] for k in range(3)) + matrix[i][3] for i in range(3)]


def bounds(transform, center, extent):
    corners = [point(transform, [center[i]+signs[i]*extent[i] for i in range(3)])
               for signs in product((-1, 1), repeat=3)]
    return ([min(p[i] for p in corners) for i in range(3)],
            [max(p[i] for p in corners) for i in range(3)])


def ancestry(game_object):
    result = []
    while game_object:
        result.append(game_object)
        assert field(game_object, "m_IsActive") == "1", result
        parent = ref(transforms[game_object], "m_Father")
        game_object = ref(parent, "m_GameObject") if parent else 0
    return result


rooms = {int(field(obj, "roomId")): obj for obj, (_, body) in docs.items()
         if "guid: 139cbc5b0ec146f9bdcb2e83cade1f70" in body}
assert sorted(rooms) == [1, 2, 3, 4, 5], rooms
assert scene.count("guid: 139cbc5b0ec146f9bdcb2e83cade1f70") == 5
room_go = ref(rooms[3], "m_GameObject")
table = ref(rooms[3], "itemTable")
table_go = ref(table, "m_GameObject")
assert room_go in ancestry(table_go)
assert field(table, "m_IsTrigger") == "0" and field(table, "m_Enabled") == "1"
table_transform = transforms[table_go]
low, high = bounds(table_transform, vector(field(table, "m_Center")),
                   [v/2 for v in vector(field(table, "m_Size"))])

mesh_tops = []
for obj, (kind, _) in docs.items():
    if kind != 33:
        continue
    go = ref(obj, "m_GameObject")
    # Inspect only meshes below the referenced desk.
    cursor = transforms[go]
    while cursor and cursor != table_transform:
        cursor = ref(cursor, "m_Father")
    if not cursor:
        continue
    guid = re.search(r"guid: ([0-9a-f]+)", field(obj, "m_Mesh"))[1]
    meta = next(p for p in (ROOT / "Assets/Mesh").glob("*.meta") if guid in p.read_text())
    mesh = meta.with_suffix("").read_text()
    local_bounds = re.search(r"m_LocalAABB:\n\s+m_Center: (.*)\n\s+m_Extent: (.*)", mesh)
    _, mesh_high = bounds(transforms[go], vector(local_bounds[1]), vector(local_bounds[2]))
    mesh_tops.append(mesh_high[1])
assert mesh_tops, "Referenced desk must have a visible mesh"

camera = next(obj for obj, (kind, _) in docs.items() if kind == 20)
eye_y = point(transforms[ref(camera, "m_GameObject")], [0, 0, 0])[1]
code = (ROOT / "Assets/CursedMod/ShellItem.cs").read_text()
size = float(re.search(r"PickupSize = ([\d.]+)f", code)[1])
clearance = float(re.search(r"TableClearance = ([\d.]+)f", code)[1])
bottom = max(high[1], *mesh_tops) + clearance
top = bottom + size
assert bottom > max(mesh_tops)
assert size < min(high[0]-low[0], high[2]-low[2])
assert bottom + .25 < eye_y < top - .25

# The old sphere was tangent to the horizontal camera ray; the new box has
# an interior horizontal cross-section with room for targeting error.
assert math.isclose(high[1] + 2.5, eye_y, abs_tol=1e-5)
assert size > 0 and top-eye_y > .25 and eye_y-bottom > .25
assert "m_Name: Pickup_Shell\n" not in scene, "Do not bake an extra pickup into the scene"
print("PASS: five unique room IDs; room 3 owns the active desk; no baked duplicate")
print(f"PASS: collision top={high[1]:.2f}, mesh top={max(mesh_tops):.2f}, "
      f"pickup bottom/top={bottom:.2f}/{top:.2f}, eye={eye_y:.2f}")
print("PASS: pickup fits desk; camera ray crosses interaction volume with clearance")
