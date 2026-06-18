using UnityEditor;
using UnityEngine;

/// <summary>
/// Quick play-testing helper: select any object(s) in the Hierarchy, hover the
/// Scene view over a surface, and press G — the selection teleports to the point
/// under the cursor, resting on whatever it hit.
///
/// • Works in edit mode (placing assets) AND while the game is playing
///   (repositioning your character to test different spots). Keep a Scene view
///   visible alongside the Game view while playing.
/// • The selected object's lowest point is dropped onto the surface, so it sits
///   on the ground instead of being half-buried.
/// • A CharacterController (Pippaloski) is briefly toggled off during the move,
///   otherwise it would snap the player straight back.
/// • Multi-select moves every selected object together, keeping their layout.
/// • Edit-mode moves are undoable (Ctrl+Z).
///
/// Change TeleportKey below if G clashes with one of your shortcuts.
/// </summary>
[InitializeOnLoad]
public static class CursorTeleport
{
    private const KeyCode TeleportKey = KeyCode.G;

    static CursorTeleport()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView view)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown || e.keyCode != TeleportKey) return;
        if (e.alt || e.control || e.command) return;   // leave modified combos to Unity

        var active = Selection.activeGameObject;
        if (active == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!TryRaycastIgnoringSelection(ray, out RaycastHit hit)) return;

        // Rest the active object's lowest point on the surface, then move the
        // whole selection by that same delta so their arrangement is preserved.
        Vector3 resting = RestPosition(active, hit.point);
        Vector3 delta   = resting - active.transform.position;

        foreach (var t in Selection.transforms)
            MoveTransform(t, t.position + delta);

        e.Use();   // swallow the key so Unity doesn't also run a default shortcut
    }

    // Raycast but skip any collider that belongs to a selected object, so the
    // cursor ray passing through the character doesn't land it on itself.
    private static bool TryRaycastIgnoringSelection(Ray ray, out RaycastHit best)
    {
        best = default;
        float bestDist = float.MaxValue;
        bool found = false;

        foreach (var h in Physics.RaycastAll(ray, 10000f))
        {
            if (h.distance >= bestDist) continue;
            if (IsSelected(h.collider.transform)) continue;
            best = h; bestDist = h.distance; found = true;
        }
        return found;
    }

    private static bool IsSelected(Transform t)
    {
        foreach (var sel in Selection.transforms)
            if (t == sel || t.IsChildOf(sel)) return true;
        return false;
    }

    private static Vector3 RestPosition(GameObject go, Vector3 surfacePoint)
    {
        if (TryGetBounds(go, out Bounds b))
        {
            float bottomToPivot = go.transform.position.y - b.min.y;
            return new Vector3(surfacePoint.x, surfacePoint.y + bottomToPivot, surfacePoint.z);
        }
        return surfacePoint;
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool has = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    private static void MoveTransform(Transform t, Vector3 worldPos)
    {
        // A CharacterController fights direct transform writes — toggle it off
        // for the move (only matters in play mode).
        var cc = t.GetComponent<CharacterController>();
        if (Application.isPlaying && cc != null && cc.enabled)
        {
            cc.enabled = false;
            t.position = worldPos;
            cc.enabled = true;
        }
        else
        {
            if (!Application.isPlaying)
                Undo.RecordObject(t, "Cursor Teleport");
            t.position = worldPos;
        }
    }
}
