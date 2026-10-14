using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Menu: Tools > P06 > Build Scene
// Membangun seluruh scene P06_BehaviorTree: layer, environment, NavMesh, Player, Enemy,
// waypoint, SafePoint, kamera, dan mengisi semua reference di Inspector.
// Aman dijalankan ulang: object lama dengan nama yang sama dihapus dulu.
public static class P06SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/P06_BehaviorTree.unity";
    private const string NavMeshAssetPath = "Assets/Scenes/P06_BehaviorTree_NavMesh.asset";

    private const int ObstacleLayer = 6;
    private const int PlayerLayer = 7;
    private const int EnemyLayer = 8;

    [MenuItem("Tools/P06/Build Scene")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SetupLayers();

        foreach (string name in new[] { "Environment", "Player", "Enemy", "PatrolPoints", "SafePoint", "DebugHUD" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Ground.mat");
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Wall.mat");
        Material playerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Player.mat");
        Material enemyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Enemy.mat");

        // ---------------- Environment ----------------
        GameObject environment = new GameObject("Environment");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(environment.transform);
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        SetMaterial(ground, groundMat);

        // Tinggi wall 4 agar Raycast dari "mata" Enemy (y = 2.5) benar-benar terhalang.
        CreateWall("Wall01", environment.transform, new Vector3(0f, 2f, 3f), new Vector3(6f, 4f, 1f), wallMat);
        CreateWall("Wall02", environment.transform, new Vector3(-4f, 2f, -2f), new Vector3(1f, 4f, 5f), wallMat);
        CreateWall("Wall03", environment.transform, new Vector3(4f, 2f, -3f), new Vector3(4f, 4f, 1f), wallMat);

        // ---------------- Player ----------------
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = PlayerLayer;
        player.transform.position = new Vector3(0f, 1f, 0f);
        SetMaterial(player, playerMat);
        player.AddComponent<SimplePlayerController>();
        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();

        // ---------------- Waypoints & SafePoint ----------------
        GameObject patrolPoints = new GameObject("PatrolPoints");
        Vector3[] waypointPositions =
        {
            new Vector3(-7f, 0f, -7f),
            new Vector3(-7f, 0f, 7f),
            new Vector3(7f, 0f, 7f),
            new Vector3(7f, 0f, -7f)
        };

        Transform[] waypoints = new Transform[waypointPositions.Length];
        for (int i = 0; i < waypointPositions.Length; i++)
        {
            GameObject waypoint = new GameObject("Waypoint0" + (i + 1));
            waypoint.transform.SetParent(patrolPoints.transform);
            waypoint.transform.position = waypointPositions[i];
            CreateMarker(waypoint.transform, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0f, 0.2f, 0f));
            waypoints[i] = waypoint.transform;
        }

        GameObject safePoint = new GameObject("SafePoint");
        safePoint.transform.position = new Vector3(-12f, 0f, -12f);
        CreateMarker(safePoint.transform, PrimitiveType.Cylinder, new Vector3(2f, 0.02f, 2f), new Vector3(0f, 0.02f, 0f));

        // ---------------- NavMesh ----------------
        GameObject navigation = new GameObject("NavMeshSurface");
        navigation.transform.SetParent(environment.transform);
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        // Player & Enemy tidak ikut di-bake supaya tidak membuat lubang di NavMesh.
        surface.layerMask = ~((1 << PlayerLayer) | (1 << EnemyLayer));
        surface.BuildNavMesh();

        AssetDatabase.DeleteAsset(NavMeshAssetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);

        // ---------------- Enemy ----------------
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Enemy";
        enemy.layer = EnemyLayer;
        enemy.transform.position = new Vector3(6f, 1f, 5f);
        enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        SetMaterial(enemy, enemyMat);

        // "Hidung" untuk menunjukkan arah hadap (penting untuk FOV).
        GameObject nose = CreateMarker(enemy.transform, PrimitiveType.Cube, new Vector3(0.3f, 0.3f, 0.5f), new Vector3(0f, 0.5f, 0.5f));
        nose.name = "Nose";
        SetMaterial(nose, wallMat);

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.baseOffset = 1f;
        agent.height = 2f;
        agent.radius = 0.5f;
        agent.speed = 3f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;

        EnemyBTController controller = enemy.AddComponent<EnemyBTController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("player").objectReferenceValue = player.transform;
        so.FindProperty("safePoint").objectReferenceValue = safePoint.transform;
        so.FindProperty("obstacleMask").intValue = 1 << ObstacleLayer;
        SerializedProperty points = so.FindProperty("patrolPoints");
        points.arraySize = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
            points.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        UtilityAIEvaluator utility = enemy.AddComponent<UtilityAIEvaluator>();

        // ---------------- Debug HUD ----------------
        GameObject hudObject = new GameObject("DebugHUD");
        EnemyDebugHUD hud = hudObject.AddComponent<EnemyDebugHUD>();
        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("enemy").objectReferenceValue = controller;
        hudSo.FindProperty("utility").objectReferenceValue = utility;
        hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        // ---------------- Camera ----------------
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 24f, -18f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(enemy.scene);
        EditorSceneManager.SaveScene(enemy.scene);
        Selection.activeGameObject = enemy;

        Debug.Log("[P06] Scene P06_BehaviorTree berhasil dibangun. Tekan Play untuk mencoba.");
    }

    private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.layer = ObstacleLayer;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        SetMaterial(wall, material);
    }

    // Penanda visual tanpa collider, supaya tidak memengaruhi NavMesh maupun Raycast.
    private static GameObject CreateMarker(Transform parent, PrimitiveType type, Vector3 scale, Vector3 localPosition)
    {
        GameObject marker = GameObject.CreatePrimitive(type);
        marker.name = "Marker";
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localScale = scale;
        return marker;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        if (material != null)
            target.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        SetLayer(layers, ObstacleLayer, "Obstacle");
        SetLayer(layers, PlayerLayer, "Player");
        SetLayer(layers, EnemyLayer, "Enemy");

        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayer(SerializedProperty layers, int index, string name)
    {
        SerializedProperty layer = layers.GetArrayElementAtIndex(index);

        if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != name)
            Debug.LogWarning($"[P06] Layer {index} sudah dipakai '{layer.stringValue}', diganti menjadi '{name}'.");

        layer.stringValue = name;
    }
}
