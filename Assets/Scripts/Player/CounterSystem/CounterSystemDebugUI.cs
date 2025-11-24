using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug UI for displaying Counter System state during testing
/// Attach to a Canvas in your scene
/// </summary>
public class CounterSystemDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CounterSystem counterSystem;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI stackInfoText;
    [SerializeField] private TextMeshProUGUI activeBuffsText;
    [SerializeField] private Image cooldownBar;

    [Header("Settings")]
    [SerializeField] private bool showDebugUI = true;

    private void Start()
    {
        // Auto-find components if not assigned
        if (counterSystem == null)
        {
            counterSystem = FindFirstObjectByType<CounterSystem>();
        }
        if (playerCharacter == null)
        {
            playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        }

        if (counterSystem == null || playerCharacter == null)
        {
            Debug.LogWarning("CounterSystemDebugUI: Missing required components!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!showDebugUI) return;

        UpdateStatusDisplay();
        UpdateStackDisplay();
        UpdateBuffDisplay();
        UpdateCooldownBar();
    }

    private void UpdateStatusDisplay()
    {
        if (statusText == null) return;

        string status = "";

        if (counterSystem.IsCounterActive)
        {
            status = "<color=yellow>COUNTER WINDOW ACTIVE!</color>";
        }
        else if (counterSystem.IsOnCooldown)
        {
            status = $"<color=red>On Cooldown: {counterSystem.CooldownRemaining:F1}s</color>";
        }
        else
        {
            status = "<color=green>Ready to Counter</color>";
        }

        statusText.text = $"Counter Status: {status}";
    }

    private void UpdateStackDisplay()
    {
        if (stackInfoText == null) return;

        string waveformName = playerCharacter.equippedWaveform != null
            ? playerCharacter.equippedWaveform.name
            : "None";

        // This is a simplified version - you'd need to expose stack counts from CounterSystem
        // For now, just show equipped waveform
        stackInfoText.text = $"Equipped: {waveformName}\nPress C to view full stack info";
    }

    private void UpdateBuffDisplay()
    {
        if (activeBuffsText == null) return;

        string buffs = "Active Buffs:\n";
        bool hasAnyBuff = false;

        // Damage Resistance
        float resistance = counterSystem.GetDamageResistance();
        if (resistance > 0)
        {
            buffs += $"<color=blue>• Damage Resistance: {resistance * 100:F0}%</color>\n";
            hasAnyBuff = true;
        }

        // Reflection
        if (counterSystem.IsReflecting())
        {
            buffs += $"<color=cyan>• REFLECTING DAMAGE</color>\n";
            hasAnyBuff = true;
        }

        // Damage Multiplier
        float damageBonus = counterSystem.GetDamageMultiplier();
        if (damageBonus > 1f)
        {
            buffs += $"<color=red>• Next Attack: {damageBonus:F1}x damage</color>\n";
            hasAnyBuff = true;
        }

        // Damage Received
        float damageReceived = counterSystem.GetDamageReceivedMultiplier();
        if (damageReceived > 1f)
        {
            buffs += $"<color=orange>• Taking {damageReceived:F1}x damage</color>\n";
            hasAnyBuff = true;
        }

        // Energy Regen
        float energyBonus = counterSystem.GetEnergyRegenMultiplier();
        if (energyBonus > 1f)
        {
            buffs += $"<color=yellow>• Energy Regen: {energyBonus:F1}x</color>\n";
            hasAnyBuff = true;
        }

        // Unlimited Energy
        if (counterSystem.HasUnlimitedEnergy())
        {
            buffs += $"<color=gold>• UNLIMITED ENERGY</color>\n";
            hasAnyBuff = true;
        }

        // Movement Speed
        float speedBonus = counterSystem.GetMovementSpeedMultiplier();
        if (speedBonus > 1f)
        {
            buffs += $"<color=lime>• Movement Speed: {speedBonus:F1}x</color>\n";
            hasAnyBuff = true;
        }

        // Chain Attack
        if (counterSystem.HasChainAttack())
        {
            buffs += $"<color=red>• CHAIN ATTACK READY</color>\n";
            hasAnyBuff = true;
        }

        if (!hasAnyBuff)
        {
            buffs += "<color=gray>None</color>";
        }

        activeBuffsText.text = buffs;
    }

    private void UpdateCooldownBar()
    {
        if (cooldownBar == null) return;

        if (counterSystem.IsOnCooldown)
        {
            // Assuming cooldown is 2 seconds - you may want to expose this value
            float cooldownDuration = 2f; // Match your CounterSystem.counterCooldown value
            float remaining = counterSystem.CooldownRemaining;
            cooldownBar.fillAmount = 1f - (remaining / cooldownDuration);
            cooldownBar.color = Color.red;
        }
        else if (counterSystem.IsCounterActive)
        {
            cooldownBar.fillAmount = 1f;
            cooldownBar.color = Color.yellow;
        }
        else
        {
            cooldownBar.fillAmount = 1f;
            cooldownBar.color = Color.green;
        }
    }

    // Call this from console or debug button to log full state
    [ContextMenu("Log Full Counter State")]
    public void LogFullCounterState()
    {
        if (counterSystem == null) return;

        Debug.Log("=== COUNTER SYSTEM STATE ===");
        Debug.Log($"Counter Active: {counterSystem.IsCounterActive}");
        Debug.Log($"On Cooldown: {counterSystem.IsOnCooldown}");
        Debug.Log($"Cooldown Remaining: {counterSystem.CooldownRemaining:F2}s");
        Debug.Log($"Damage Resistance: {counterSystem.GetDamageResistance() * 100:F0}%");
        Debug.Log($"Is Reflecting: {counterSystem.IsReflecting()}");
        Debug.Log($"Damage Multiplier: {counterSystem.GetDamageMultiplier():F2}x");
        Debug.Log($"Damage Received Multiplier: {counterSystem.GetDamageReceivedMultiplier():F2}x");
        Debug.Log($"Energy Regen Multiplier: {counterSystem.GetEnergyRegenMultiplier():F2}x");
        Debug.Log($"Has Unlimited Energy: {counterSystem.HasUnlimitedEnergy()}");
        Debug.Log($"Movement Speed Multiplier: {counterSystem.GetMovementSpeedMultiplier():F2}x");
        Debug.Log($"Has Chain Attack: {counterSystem.HasChainAttack()}");
        Debug.Log($"Chain Range: {counterSystem.GetChainRange():F1}m");
        Debug.Log("===========================");
    }
}