using UnityEngine;

namespace EnemySystem
{
    [CreateAssetMenu(fileName = "EnemyVisualSettings", menuName = "Enemy/Visual Settings", order = 1)]
    public class EnemyVisualSettings : ScriptableObject
    {
        [Header("Enemy Identity")]
        public string enemyName = "Enemy";
        public Color enemyColor = Color.green;

        [Header("Color Tint Settings")]
        [Tooltip("Color to tint towards when darkening")]
        public Color darkTintColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [Tooltip("How much to darken when idle (0 = normal, 1 = fully dark)")]
        [Range(0f, 1f)]
        public float idleTintStrength = 0.7f;
        [Tooltip("How much to darken when detected (0 = normal, 1 = fully dark)")]
        [Range(0f, 1f)]
        public float detectedTintStrength = 0.0f;
        [Tooltip("How much to darken when attacking (0 = normal, 1 = fully dark)")]
        [Range(0f, 1f)]
        public float attackingTintStrength = 0.5f;
        [Tooltip("Speed of color pulsing when attacking")]
        public float attackingPulseSpeed = 4.0f;

        [Header("Transition Settings")]
        [Tooltip("Speed of transitions between states")]
        public float transitionSpeed = 3f;
    }
}