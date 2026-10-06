using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Elevation: two buildings get working fire escapes you can climb, flight by flight, to the roof.
//   A  North Avenue (x -10..-5, facing north): the tallest on the block, ten flights to a 30 m roof.
//   B  South Avenue (x -10..-5, facing south): four flights to a 12 m roof.
// The pack's fire-escape piece (a landing with a steep stair up to the next landing) is visual only; each piece gets
// simple invisible floors, rails and a ramp under the stair, so climbing is smooth for the character controller.
// Named anchors mark landings, roofs and the climbing route (used by the Flux hunt and the climb test).
public static partial class CityMap {
    static readonly Dictionary<(int x, int z), (string id, int stacks)> ClimbTowers = new Dictionary<(int, int), (string, int)> {
        { (-10, 35), ("A", 3) }, { (-10, -40), ("B", 1) },
    };
    static bool ClimbTower(float x0, float z0, out string id, out int stacks) {
        if (ClimbTowers.TryGetValue(((int)x0, (int)z0), out var c)) { id = c.id; stacks = c.stacks; return true; }
        id = null; stacks = 0; return false;
    }
    const float FireBase = .05f;   // the ground landing sits just above the sidewalk; every piece above is 3 m higher

    static void ClimbableFireEscape(Transform b, float x0, float z0, int facing, int stacks, string id) {
        int top = stacks * 3; float roofY = 3 + 9 * stacks, roofTop = roofY + .5f;
        var cols = new GameObject("Fire escape colliders " + id).transform; cols.SetParent(b, false);
        var path = new GameObject("Climb " + id + " path").transform; path.SetParent(root, false);
        int n = 0; void Way(Vector3 p) { var w = new GameObject("Way " + (n++).ToString("00")).transform; w.SetParent(path, false); w.position = p; }
        Transform first = null;
        for (int i = 0; i <= top; i++) {
            float y = FireBase + 3 * i;
            var piece = Put("Buildings/SM_Bld_FireEscape_02", OnFace(x0, z0, facing, 1.7f, .45f, y), 90 * facing, b);   // clear of the shopfront, which juts .43 m if (!piece) return;
            StripColliders(piece.transform); var t = piece.transform; if (!first) first = t;
            // Floor: the landing, minus the hole the flight below comes up through (local x -2.6..0, z .78..1.64).
            LocalBox(cols, t, "Landing", new Vector3(-1.67f, .05f, .39f), new Vector3(3.18f, .1f, .78f));
            LocalBox(cols, t, "Landing end", new Vector3(-2.93f, .05f, 1.21f), new Vector3(.66f, .1f, .86f));
            // Rails: the outer edge and both ends (the ground landing is open to the sidewalk).
            LocalBox(cols, t, "Rail", new Vector3(-1.67f, .65f, 1.66f), new Vector3(3.2f, 1.2f, .06f));
            LocalBox(cols, t, "Rail end", new Vector3(-3.29f, .65f, .82f), new Vector3(.06f, 1.2f, 1.7f));
            if (i > 0) LocalBox(cols, t, "Rail end", new Vector3(-.05f, .65f, .82f), new Vector3(.06f, 1.2f, 1.7f));
            // The flight up: a ramp under the steps. The last one lands level with the roof.
            var low = t.TransformPoint(new Vector3(-2.95f, .12f, 1.19f));
            var high = i < top ? t.TransformPoint(new Vector3(-.55f, 3.1f, 1.19f)) : t.TransformPoint(new Vector3(-.3f, roofTop + .05f - y, 1.19f));
            Ramp(cols, low, high, .7f);
            if (i == top) LocalBox(cols, t, "Roof step", new Vector3(-.5f, roofTop - y, .52f), new Vector3(1f, .1f, .56f));   // beside the stair top, never over it
            var landing = new GameObject("Climb " + id + " landing " + i).transform; landing.SetParent(root, false);
            landing.SetPositionAndRotation(t.TransformPoint(new Vector3(-1.7f, .1f, -.05f)), Quaternion.Euler(0, 90 * facing + 180, 0));
            // Route for the climb test: along the landing, onto the flight, up, step off onto the next landing.
            if (i == 0) { Way(t.TransformPoint(new Vector3(.8f, 0, .45f))); Way(t.TransformPoint(new Vector3(-.4f, .1f, .45f))); }   // step on at the open end
            Way(t.TransformPoint(new Vector3(-2.95f, .1f, .45f))); Way(t.TransformPoint(new Vector3(-2.95f, .2f, 1.19f)));
            if (i < top) { Way(t.TransformPoint(new Vector3(-.6f, 3.1f, 1.19f))); Way(t.TransformPoint(new Vector3(-.6f, 3.1f, .45f))); }
            else { Way(high); Way(t.TransformPoint(new Vector3(-.45f, roofTop - y, .5f))); Way(t.TransformPoint(new Vector3(-1.2f, roofTop - y, -1.6f))); }
        }
        var roofMark = new GameObject("Climb " + id + " roof").transform; roofMark.SetParent(root, false);
        roofMark.position = OnFace(x0, z0, facing, 0, -2.5f, roofTop);
    }
    // A box collider given in a fire-escape piece's local frame.
    static void LocalBox(Transform parent, Transform frame, string name, Vector3 centre, Vector3 size) {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(frame.TransformPoint(centre), frame.rotation);
        go.AddComponent<BoxCollider>().size = size;
    }
    // A thin walkable slab whose top surface runs from `low` to `high`.
    static void Ramp(Transform parent, Vector3 low, Vector3 high, float width) {
        var go = new GameObject("Stair ramp"); go.transform.SetParent(parent, false);
        var rot = Quaternion.LookRotation(high - low); const float thick = .16f;
        go.transform.SetPositionAndRotation((low + high) / 2 - rot * Vector3.up * thick / 2, rot);
        go.AddComponent<BoxCollider>().size = new Vector3(width, thick, (high - low).magnitude + .1f);
    }
}
