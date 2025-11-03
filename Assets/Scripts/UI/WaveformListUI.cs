using UnityEngine;
using System.Collections.Generic;

public class WaveformListUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform container;
    public GameObject waveformSlotPrefab;

    private List<WaveformSlot> slots = new List<WaveformSlot>();

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        RefreshWaveformList();
    }

    private void Update()
    {
        UpdateSelection();
    }

    public void RefreshWaveformList()
    {
        // Clear existing slots
        foreach (var slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slots.Clear();

        if (player == null || waveformSlotPrefab == null || container == null)
            return;

        // Create slot for each unlocked waveform
        for (int i = 0; i < player.unlockedWaveforms.Count; i++)
        {
            GameObject slotObj = Instantiate(waveformSlotPrefab, container);
            WaveformSlot slot = slotObj.GetComponent<WaveformSlot>();

            if (slot != null)
            {
                bool isSelected = i == player.currentWaveformIndex;
                slot.Setup(player.unlockedWaveforms[i], isSelected);
                slots.Add(slot);
            }
        }
    }

    private void UpdateSelection()
    {
        if (player == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetSelected(i == player.currentWaveformIndex);
            }
        }
    }
}