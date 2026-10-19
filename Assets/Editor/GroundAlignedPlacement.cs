using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Editor-only: prefab behaviour and runtime movement are left to their components.
public class GroundAlignedPlacement : EditorWindow
{
    [SerializeField] private Tilemap ground;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool replaceTiles;
    private bool painting;

    [MenuItem("Tools/床をグリッド配置")]
    private static void Open() => GetWindow<GroundAlignedPlacement>("床をグリッド配置");

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        if (prefab == null) SetPrefab("CrumblingBlock");
    }

    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void SetPrefab(string name)
    {
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("地面の Tilemap を指定し、配置モードで Scene をクリックします。床の左端と上面をマスの境界にそろえます。Esc で終了。", MessageType.Info);
        ground = (Tilemap)EditorGUILayout.ObjectField("基準の地面", ground, typeof(Tilemap), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("崩れる床")) SetPrefab("CrumblingBlock");
            if (GUILayout.Button("動く床")) SetPrefab("MovingPlatform");
            if (GUILayout.Button("すり抜け床")) SetPrefab("OneWayPlatform");
        }
        prefab = (GameObject)EditorGUILayout.ObjectField("配置する Prefab", prefab, typeof(GameObject), false);
        replaceTiles = EditorGUILayout.Toggle("重なる地面タイルを置換", replaceTiles);
        EditorGUILayout.HelpBox(replaceTiles
            ? "配置する床に重なる、指定した Tilemap のタイルを消します。Ctrl+Z で床の配置と一緒に戻せます。"
            : "地面タイルや別の床に重なる場所には配置しません。地面の一部を崩れる床にする場合は「置換」を ON にしてください。", MessageType.Info);
        bool valid = ValidGround() && ValidBlock(prefab) && PrefabUtility.IsPartOfPrefabAsset(prefab);
        if (!valid)
            EditorGUILayout.HelpBox("シーン内の XY 矩形 Tilemap と、回転していない BoxCollider2D 付き Prefab を指定してください。", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlaying))
        {
            painting = GUILayout.Toggle(painting, "配置モード（Scene をクリック）", "Button");
        }
        using (new EditorGUI.DisabledScope(!ValidGround() || EditorApplication.isPlaying))
        {
            if (GUILayout.Button("選択した床を整列")) SnapSelection();
        }
        EditorGUILayout.LabelField("幅・厚み・崩れるまでの時間は維持します。", EditorStyles.wordWrappedLabel);
    }

    private bool ValidGround()
    {
        if (ground == null || !ground.gameObject.scene.IsValid() || ground.layoutGrid == null ||
            ground.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle) return false;
        Vector3 x = ground.CellToWorld(Vector3Int.right) - ground.CellToWorld(Vector3Int.zero);
        Vector3 y = ground.CellToWorld(Vector3Int.up) - ground.CellToWorld(Vector3Int.zero);
        return x.x > 0 && y.y > 0 && Mathf.Abs(x.y) + Mathf.Abs(x.z) + Mathf.Abs(y.x) + Mathf.Abs(y.z) < 0.0001f;
    }

    private static bool ValidBlock(GameObject block)
    {
        return block != null && block.GetComponent<BoxCollider2D>() != null &&
            Quaternion.Angle(block.transform.rotation, Quaternion.identity) < 0.01f;
    }

    internal static Bounds BlockBounds(GameObject block, Vector3 position)
    {
        var box = block.GetComponent<BoxCollider2D>();
        Vector3 scale = block.transform.lossyScale;
        Vector3 offset = Vector3.Scale(box.offset, scale);
        Vector3 size = Vector3.Scale(box.size, scale);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0);
        return new Bounds(position + offset, size);
    }

    internal static Vector3 AlignedPosition(Tilemap map, GameObject block, Vector3Int cell)
    {
        Bounds bounds = BlockBounds(block, Vector3.zero);
        Vector3 corner = map.CellToWorld(cell);
        float top = map.CellToWorld(cell + Vector3Int.up).y;
        return new Vector3(corner.x - bounds.min.x, top - bounds.max.y, corner.z);
    }

    private BoundsInt CoveredCells(Bounds bounds)
    {
        Vector3 inset = new Vector3(0.0001f, 0.0001f, 0);
        Vector3Int min = ground.WorldToCell(bounds.min + inset);
        Vector3Int max = ground.WorldToCell(bounds.max - inset);
        min.z = max.z = 0;
        return new BoundsInt(min, max - min + Vector3Int.one);
    }

    private bool CanPlace(GameObject block, Vector3 position, out BoundsInt cells)
    {
        Bounds bounds = BlockBounds(block, position);
        cells = CoveredCells(bounds);
        if (!replaceTiles)
            foreach (Vector3Int cell in cells.allPositionsWithin)
                if (ground.HasTile(cell)) return false;
        foreach (var other in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (other.gameObject == block || other.isTrigger || !other.enabled ||
                other.gameObject.scene != ground.gameObject.scene) continue;
            Bounds occupied = other.bounds;
            if (bounds.min.x < occupied.max.x - 0.001f && bounds.max.x > occupied.min.x + 0.001f &&
                bounds.min.y < occupied.max.y - 0.001f && bounds.max.y > occupied.min.y + 0.001f)
                return false;
        }
        return true;
    }

    private void ClearTiles(BoundsInt cells)
    {
        if (!replaceTiles) return;
        Undo.RegisterCompleteObjectUndo(ground, "床をグリッド配置");
        foreach (Vector3Int cell in cells.allPositionsWithin) ground.SetTile(cell, null);
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!painting || !ValidGround() || !ValidBlock(prefab) ||
            !PrefabUtility.IsPartOfPrefabAsset(prefab) || EditorApplication.isPlaying) return;
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            painting = false;
            e.Use();
            Repaint();
            return;
        }
        if (e.alt) return;
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var plane = new Plane(Vector3.forward, ground.CellToWorld(Vector3Int.zero));
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3Int cell = ground.WorldToCell(ray.GetPoint(distance));
        cell.z = 0;
        Vector3 position = AlignedPosition(ground, prefab, cell);
        bool available = CanPlace(prefab, position, out BoundsInt cells);
        Bounds bounds = BlockBounds(prefab, position);
        Handles.color = available ? Color.green : Color.red;
        Handles.DrawWireCube(bounds.center, bounds.size);
        Handles.Label(bounds.max + Vector3.up * 0.15f, available ? prefab.name : "地面・床と重なっています");
        if (e.type == EventType.MouseMove) view.Repaint();
        if (e.type != EventType.MouseDown || e.button != 0) return;
        e.Use();
        if (!available) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("床をグリッド配置");
        ClearTiles(cells);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ground.gameObject.scene);
        instance.transform.position = position;
        Undo.RegisterCreatedObjectUndo(instance, "床をグリッド配置");
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        Undo.CollapseUndoOperations(group);
    }

    private void SnapSelection()
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("床を整列");
        Vector3 origin = ground.CellToWorld(Vector3Int.zero);
        Vector3 stepX = ground.CellToWorld(Vector3Int.right) - origin;
        Vector3 stepY = ground.CellToWorld(Vector3Int.up) - origin;
        int skipped = 0;
        foreach (GameObject block in Selection.gameObjects)
        {
            if (!ValidBlock(block) || EditorUtility.IsPersistent(block) || block.scene != ground.gameObject.scene)
            {
                skipped++;
                continue;
            }
            Bounds bounds = BlockBounds(block, block.transform.position);
            var cell = new Vector3Int(Mathf.RoundToInt((bounds.min.x - origin.x) / stepX.x),
                Mathf.RoundToInt((bounds.max.y - origin.y) / stepY.y) - 1, 0);
            Vector3 position = AlignedPosition(ground, block, cell);
            position.z = block.transform.position.z;
            if (!CanPlace(block, position, out BoundsInt cells)) { skipped++; continue; }
            ClearTiles(cells);
            Undo.RecordObject(block.transform, "床を整列");
            block.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(block.transform);
            Physics2D.SyncTransforms();
        }
        Undo.CollapseUndoOperations(group);
        if (skipped > 0) ShowNotification(new GUIContent(skipped + " 個をスキップ（重なり／対象外）"));
        SceneView.RepaintAll();
    }
}
