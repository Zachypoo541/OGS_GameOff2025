using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window tool for managing spawn points in the scene.
/// Access via: Tools > Wave System > Spawn Point Manager
/// </summary>
#if UNITY_EDITOR
public class SpawnPointManager : EditorWindow
{
    private string newSpawnPointName = "SpawnPoint";
    private Color spawnPointColor = Color.green;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Wave System/Spawn Point Manager")]
    public static void ShowWindow()
    {
        GetWindow<SpawnPointManager>("Spawn Point Manager");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spawn Point Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Create new spawn point section
        EditorGUILayout.LabelField("Create New Spawn Point", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        newSpawnPointName = EditorGUILayout.TextField("Spawn Point Name:", newSpawnPointName);
        spawnPointColor = EditorGUILayout.ColorField("Gizmo Color:", spawnPointColor);

        if (GUILayout.Button("Create Spawn Point at Scene Center"))
        {
            CreateSpawnPoint(Vector3.zero);
        }

        if (GUILayout.Button("Create Spawn Point at Selection"))
        {
            if (Selection.activeTransform != null)
            {
                CreateSpawnPoint(Selection.activeTransform.position);
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select an object in the scene first.", "OK");
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // List existing spawn points
        EditorGUILayout.LabelField("Existing Spawn Points", EditorStyles.boldLabel);

        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        if (spawnPoints.Length == 0)
        {
            EditorGUILayout.HelpBox("No spawn points found in the scene.", MessageType.Info);
        }
        else
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox);

            foreach (SpawnPoint sp in spawnPoints)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(sp.spawnPointID, GUILayout.Width(150));

                GUI.color = sp.gizmoColor;
                if (GUILayout.Button("■", GUILayout.Width(30)))
                {
                    // Color indicator and color picker
                    sp.gizmoColor = EditorGUILayout.ColorField(sp.gizmoColor);
                    EditorUtility.SetDirty(sp);
                }
                GUI.color = Color.white;

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = sp.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    DuplicateSpawnPoint(sp);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Total: {spawnPoints.Length} spawn points");
        }

        EditorGUILayout.Space(10);

        // Utility buttons
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GUILayout.Button("Show All Spawn Points"))
        {
            foreach (SpawnPoint sp in spawnPoints)
            {
                sp.showGizmo = true;
                EditorUtility.SetDirty(sp);
            }
        }

        if (GUILayout.Button("Hide All Spawn Points"))
        {
            foreach (SpawnPoint sp in spawnPoints)
            {
                sp.showGizmo = false;
                EditorUtility.SetDirty(sp);
            }
        }

        if (GUILayout.Button("Create Spawn Point Container"))
        {
            CreateSpawnPointContainer();
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateSpawnPoint(Vector3 position)
    {
        GameObject spawnPointObj = new GameObject(newSpawnPointName);
        spawnPointObj.transform.position = position;

        SpawnPoint spawnPoint = spawnPointObj.AddComponent<SpawnPoint>();
        spawnPoint.spawnPointID = newSpawnPointName;
        spawnPoint.gizmoColor = spawnPointColor;

        // Try to parent to spawn point container if it exists
        GameObject container = GameObject.Find("SpawnPoints");
        if (container != null)
        {
            spawnPointObj.transform.SetParent(container.transform);
        }

        Selection.activeGameObject = spawnPointObj;
        Undo.RegisterCreatedObjectUndo(spawnPointObj, "Create Spawn Point");

        Debug.Log($"Created spawn point: {newSpawnPointName} at {position}");
    }

    private void DuplicateSpawnPoint(SpawnPoint original)
    {
        GameObject duplicate = Instantiate(original.gameObject);
        duplicate.name = original.gameObject.name + "_Copy";
        duplicate.transform.position = original.transform.position + Vector3.right * 2f;

        SpawnPoint duplicatePoint = duplicate.GetComponent<SpawnPoint>();
        duplicatePoint.spawnPointID = original.spawnPointID + "_Copy";

        Selection.activeGameObject = duplicate;
        Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Spawn Point");

        Debug.Log($"Duplicated spawn point: {duplicatePoint.spawnPointID}");
    }

    private void CreateSpawnPointContainer()
    {
        GameObject container = GameObject.Find("SpawnPoints");

        if (container != null)
        {
            EditorUtility.DisplayDialog("Container Exists", "A SpawnPoints container already exists in the scene.", "OK");
            Selection.activeGameObject = container;
            return;
        }

        container = new GameObject("SpawnPoints");
        Undo.RegisterCreatedObjectUndo(container, "Create Spawn Point Container");

        Debug.Log("Created SpawnPoints container. You can organize your spawn points under this object.");
    }
}
#endif