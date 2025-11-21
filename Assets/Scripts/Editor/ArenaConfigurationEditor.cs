using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for ArenaConfiguration to display helpful information.
/// Place this file in an "Editor" folder.
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(ArenaConfiguration))]
public class ArenaConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ArenaConfiguration arena = (ArenaConfiguration)target;

        // Draw default inspector
        DrawDefaultInspector();

        // Add visual separator
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);

        // Display arena statistics
        EditorGUILayout.LabelField("Arena Overview", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int waveCount = arena.GetWaveCount();
        int totalEnemies = 0;

        // Safely get total enemies with null check
        try
        {
            totalEnemies = arena.GetTotalEnemyCount();
        }
        catch (System.Exception)
        {
            totalEnemies = 0;
        }

        EditorGUILayout.LabelField($"Total Waves: {waveCount}");
        EditorGUILayout.LabelField($"Total Enemies: {totalEnemies}");

        if (arena.nextArena != null)
        {
            EditorGUILayout.LabelField($"Next Arena: {arena.nextArena.arenaName}");
        }
        else
        {
            EditorGUILayout.LabelField("Next Arena: None (Final Arena)");
        }

        EditorGUILayout.EndVertical();

        // Wave-by-wave breakdown
        if (arena.waves != null && arena.waves.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Wave Breakdown:", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < arena.waves.Length; i++)
            {
                WaveConfiguration wave = arena.waves[i];

                if (wave != null)
                {
                    int enemyCount = wave.GetTotalEnemyCount();
                    int groupCount = wave.spawnGroups != null ? wave.spawnGroups.Length : 0;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Wave {i + 1}:", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{enemyCount} enemies, {groupCount} groups", EditorStyles.miniLabel);

                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        Selection.activeObject = wave;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox($"Wave {i + 1} is not assigned!", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // Add quick action buttons
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create New Wave"))
        {
            CreateNewWave(arena);
        }

        if (GUILayout.Button("Validate Arena"))
        {
            ValidateArena(arena);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewWave(ArenaConfiguration arena)
    {
        // Create new wave asset in the same folder as the arena
        string arenaPath = AssetDatabase.GetAssetPath(arena);
        string folderPath = System.IO.Path.GetDirectoryName(arenaPath);

        WaveConfiguration newWave = ScriptableObject.CreateInstance<WaveConfiguration>();
        newWave.waveNumber = arena.GetWaveCount() + 1;

        string wavePath = AssetDatabase.GenerateUniqueAssetPath(
            $"{folderPath}/Wave_{arena.arenaNumber}_{newWave.waveNumber}.asset");

        AssetDatabase.CreateAsset(newWave, wavePath);
        AssetDatabase.SaveAssets();

        // Add to arena's wave list
        System.Collections.Generic.List<WaveConfiguration> waves =
            new System.Collections.Generic.List<WaveConfiguration>(arena.waves ?? new WaveConfiguration[0]);
        waves.Add(newWave);
        arena.waves = waves.ToArray();

        EditorUtility.SetDirty(arena);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newWave;

        Debug.Log($"Created new wave: {wavePath}");
    }

    private void ValidateArena(ArenaConfiguration arena)
    {
        int issueCount = 0;

        if (arena.waves == null || arena.waves.Length == 0)
        {
            Debug.LogWarning($"Arena '{arena.arenaName}' has no waves assigned!");
            issueCount++;
        }
        else
        {
            for (int i = 0; i < arena.waves.Length; i++)
            {
                if (arena.waves[i] == null)
                {
                    Debug.LogWarning($"Arena '{arena.arenaName}' wave slot {i + 1} is empty!");
                    issueCount++;
                }
                else
                {
                    WaveConfiguration wave = arena.waves[i];

                    if (wave.spawnGroups == null || wave.spawnGroups.Length == 0)
                    {
                        Debug.LogWarning($"Arena '{arena.arenaName}' Wave {i + 1} has no spawn groups!");
                        issueCount++;
                    }
                    else
                    {
                        foreach (var group in wave.spawnGroups)
                        {
                            if (group.spawnEntries != null)
                            {
                                foreach (var entry in group.spawnEntries)
                                {
                                    if (entry.enemyPrefab == null)
                                    {
                                        Debug.LogWarning($"Arena '{arena.arenaName}' Wave {i + 1} has spawn entry with no enemy prefab!");
                                        issueCount++;
                                    }

                                    if (string.IsNullOrEmpty(entry.spawnPointID))
                                    {
                                        Debug.LogWarning($"Arena '{arena.arenaName}' Wave {i + 1} has spawn entry with no spawn point ID!");
                                        issueCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (issueCount == 0)
        {
            Debug.Log($"Arena '{arena.arenaName}' validation complete. No issues found!");
        }
        else
        {
            Debug.LogWarning($"Arena '{arena.arenaName}' validation found {issueCount} issue(s). Check console for details.");
        }
    }
}
#endif