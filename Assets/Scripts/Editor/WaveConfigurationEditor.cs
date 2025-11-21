using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for WaveConfiguration to display helpful information.
/// Place this file in an "Editor" folder.
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(WaveConfiguration))]
public class WaveConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WaveConfiguration wave = (WaveConfiguration)target;

        // Draw default inspector
        DrawDefaultInspector();

        // Add visual separator
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);

        // Display wave statistics
        EditorGUILayout.LabelField("Wave Statistics", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int totalEnemies = wave.GetTotalEnemyCount();
        float totalDuration = wave.GetTotalSpawnDuration();
        int groupCount = wave.spawnGroups != null ? wave.spawnGroups.Length : 0;

        EditorGUILayout.LabelField($"Total Spawn Groups: {groupCount}");
        EditorGUILayout.LabelField($"Total Enemies: {totalEnemies}");
        EditorGUILayout.LabelField($"Spawn Duration: {totalDuration:F1} seconds");

        // List enemies by type
        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Enemy Breakdown:", EditorStyles.miniBoldLabel);

            System.Collections.Generic.Dictionary<string, int> enemyCounts =
                new System.Collections.Generic.Dictionary<string, int>();

            foreach (var group in wave.spawnGroups)
            {
                if (group.spawnEntries != null)
                {
                    foreach (var entry in group.spawnEntries)
                    {
                        if (entry.enemyPrefab != null)
                        {
                            string enemyName = entry.enemyPrefab.name;
                            if (!enemyCounts.ContainsKey(enemyName))
                            {
                                enemyCounts[enemyName] = 0;
                            }
                            enemyCounts[enemyName] += entry.count;
                        }
                    }
                }
            }

            foreach (var kvp in enemyCounts)
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }

        EditorGUILayout.EndVertical();

        // Add validation warnings
        EditorGUILayout.Space(5);
        ValidateWave(wave);
    }

    private void ValidateWave(WaveConfiguration wave)
    {
        bool hasWarnings = false;

        if (wave.spawnGroups == null || wave.spawnGroups.Length == 0)
        {
            EditorGUILayout.HelpBox("No spawn groups defined. Add at least one spawn group.", MessageType.Warning);
            hasWarnings = true;
        }
        else
        {
            for (int i = 0; i < wave.spawnGroups.Length; i++)
            {
                SpawnGroup group = wave.spawnGroups[i];

                if (group.spawnEntries == null || group.spawnEntries.Length == 0)
                {
                    EditorGUILayout.HelpBox($"Spawn Group {i + 1} has no spawn entries.", MessageType.Warning);
                    hasWarnings = true;
                }
                else
                {
                    foreach (var entry in group.spawnEntries)
                    {
                        if (entry.enemyPrefab == null)
                        {
                            EditorGUILayout.HelpBox($"Spawn Group {i + 1} has an entry with no enemy prefab.", MessageType.Warning);
                            hasWarnings = true;
                        }

                        if (string.IsNullOrEmpty(entry.spawnPointID))
                        {
                            EditorGUILayout.HelpBox($"Spawn Group {i + 1} has an entry with no spawn point ID.", MessageType.Warning);
                            hasWarnings = true;
                        }
                    }
                }
            }
        }

        if (!hasWarnings)
        {
            EditorGUILayout.HelpBox("Wave configuration looks good!", MessageType.Info);
        }
    }
}
#endif