using UnityEngine;
using UnityEditor;

/// PathManager 커스텀 인스펙터 — 경로 타일을 방향 버튼 클릭만으로 빠르게 이어 붙이는 도구.
/// 항상 "마지막 타일 기준으로 2유닛 격자에 맞춰 다음 타일을 추가"하는 방식이라 간격이 어긋나거나
/// 대각선으로 놓일 일이 구조적으로 없다(배치 규칙을 도구 자체가 강제함).
/// 새 스테이지를 만들 때 Stage_Template을 복제한 씬에서도 Path 오브젝트를 선택하면 바로 쓸 수 있다
/// (특정 씬에 종속된 도구가 아니라, PathManager가 있는 모든 씬의 인스펙터에 자동으로 나타남).
[CustomEditor(typeof(PathManager))]
public class PathManagerEditor : Editor
{
    private const float TileSpacing = 2f;
    private const string PathTileMaterialPath = "Assets/Materials/M_PathTile.mat";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PathManager pathManager = (PathManager)target;
        Transform tileRoot = GetTileRoot(pathManager);
        if (tileRoot == null)
        {
            EditorGUILayout.HelpBox("타일 루트(Tiles)를 찾을 수 없습니다. Tile Root 필드를 먼저 지정하세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("경로 타일 빠르게 잇기", EditorStyles.boldLabel);

        if (tileRoot.childCount == 0)
        {
            EditorGUILayout.HelpBox("타일이 하나도 없습니다. Tile_00은 직접 만들어주세요(시작 지점).", MessageType.Info);
            return;
        }

        Transform lastTile = tileRoot.GetChild(tileRoot.childCount - 1);
        EditorGUILayout.LabelField($"마지막 타일: {lastTile.name}  (타일 {tileRoot.childCount}개)");

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↑ 북(+Z)", GUILayout.Width(80))) AddTile(tileRoot, new Vector3(0f, 0f, TileSpacing));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("← 서(-X)", GUILayout.Width(80))) AddTile(tileRoot, new Vector3(-TileSpacing, 0f, 0f));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("→ 동(+X)", GUILayout.Width(80))) AddTile(tileRoot, new Vector3(TileSpacing, 0f, 0f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↓ 남(-Z)", GUILayout.Width(80))) AddTile(tileRoot, new Vector3(0f, 0f, -TileSpacing));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        GUI.enabled = tileRoot.childCount > 1; // Tile_00(시작 지점)은 못 지우게 막는다
        if (GUILayout.Button("마지막 타일 되돌리기")) RemoveLastTile(tileRoot);
        GUI.enabled = true;
    }

    // PathManager.tileRoot는 private 필드라 SerializedProperty로 읽는다. 비어있으면(기본값) PathManager
    // 자신의 Transform이 곧 타일 루트라는 뜻(BuildTilesFromChildren의 기본 동작과 동일하게 맞춤).
    private Transform GetTileRoot(PathManager pathManager)
    {
        SerializedObject so = new SerializedObject(pathManager);
        SerializedProperty prop = so.FindProperty("tileRoot");
        Transform tileRoot = prop != null ? prop.objectReferenceValue as Transform : null;
        return tileRoot != null ? tileRoot : pathManager.transform;
    }

    private void AddTile(Transform tileRoot, Vector3 offset)
    {
        Transform lastTile = tileRoot.GetChild(tileRoot.childCount - 1);
        Vector3 newPos = lastTile.position + offset;
        int index = tileRoot.childCount;

        GameObject go = new GameObject($"Tile_{index:00}");
        Undo.RegisterCreatedObjectUndo(go, "경로 타일 추가");
        go.transform.SetParent(tileRoot, false);
        go.transform.position = newPos;

        // 로직용 마커만 만들면 화면에 아무것도 안 보여서 어디 놓였는지 확인하기 어렵다 — 최종 결과물과
        // 같은 방식(PathTilesVisual 아래 Plane, Y 0.02, M_PathTile)으로 길 바닥을 바로 같이 깔아준다.
        CreateVisualTile(tileRoot, index, newPos);

        // 새로 만든 타일로 선택을 옮기지 않는다 — Path(이 버튼들이 있는 오브젝트) 선택이 계속 유지돼야
        // 버튼을 연달아 눌러서 타일을 빠르게 이어붙일 수 있다(선택이 바뀌면 매번 Path를 다시 클릭해야 함).
        EditorUtility.SetDirty(tileRoot.gameObject);
    }

    private void RemoveLastTile(Transform tileRoot)
    {
        Transform lastTile = tileRoot.GetChild(tileRoot.childCount - 1);
        int index = tileRoot.childCount - 1;
        RemoveVisualTile(tileRoot, index);
        Undo.DestroyObjectImmediate(lastTile.gameObject);
        EditorUtility.SetDirty(tileRoot.gameObject);
    }

    // Path/Tiles와 나란히 있는 Path/PathTilesVisual 아래에 길 바닥 하나를 만든다. 컨테이너가 없는 씬(아직
    // 3-3 절차를 한 번도 안 거친 씬 등)에서는 건너뛴다 — 로직(Tile_XX) 생성 자체는 항상 되게 하기 위함.
    private void CreateVisualTile(Transform tileRoot, int index, Vector3 pos)
    {
        Transform visualRoot = FindVisualRoot(tileRoot);
        if (visualRoot == null) return;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Collider col = visual.GetComponent<Collider>();
        if (col != null) Undo.DestroyObjectImmediate(col);

        Undo.RegisterCreatedObjectUndo(visual, "경로 타일 추가");
        visual.name = $"PathTile_{index:00}";
        visual.transform.SetParent(visualRoot, false);
        visual.transform.position = pos + new Vector3(0f, 0.02f, 0f);
        visual.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(PathTileMaterialPath);
        if (mat != null) visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private void RemoveVisualTile(Transform tileRoot, int index)
    {
        Transform visualRoot = FindVisualRoot(tileRoot);
        if (visualRoot == null) return;

        Transform visual = visualRoot.Find($"PathTile_{index:00}");
        if (visual != null) Undo.DestroyObjectImmediate(visual.gameObject);
    }

    // Tiles와 PathTilesVisual은 항상 같은 부모(Path) 아래의 형제 컨테이너라는 기존 규칙을 그대로 따른다.
    private Transform FindVisualRoot(Transform tileRoot)
    {
        Transform parent = tileRoot.parent;
        return parent != null ? parent.Find("PathTilesVisual") : null;
    }
}
