using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Player Combat Settings")]
    public List<WaveformData> unlockedWaveforms = new List<WaveformData>();
    public int currentWaveformIndex = 0;

    [Header("Aiming Settings")]
    [SerializeField] private float maxAimDistance = 1000f;
    [SerializeField] private LayerMask aimRaycastMask = -1;

    private Transform _cameraTransform;
    private Reticle _reticle;
    private CounterSystem _counterSystem;
    private CombatEntity _combatEntity;
    private Transform _projectileSpawnPoint;
    private HandAnimationController _handAnimationController;

    public void Initialize(Transform cameraTransform, Reticle reticle, CounterSystem counterSystem, CombatEntity combatEntity, Transform projectileSpawnPoint, HandAnimationController handAnimationController)
    {
        _cameraTransform = cameraTransform;
        _reticle = reticle;
        _counterSystem = counterSystem;
        _combatEntity = combatEntity;
        _projectileSpawnPoint = projectileSpawnPoint;
        _handAnimationController = handAnimationController;

        if (_handAnimationController != null)
        {
            _handAnimationController.Initialize();
        }

        // Equip the first waveform (this will trigger the initial enter animation)
        if (unlockedWaveforms.Count > 0)
        {
            EquipWaveform(0);
        }

        // Update reticle to match starting waveform
        if (_reticle != null && _combatEntity.equippedWaveform != null)
        {
            _reticle.UpdateReticleForWaveform(_combatEntity.equippedWaveform);
        }
    }

    public void UpdateCombatInput(CombatInput input)
    {
        // Handle counter input
        if (input.Counter && _counterSystem != null)
        {
            _counterSystem.AttemptCounter();

            // Play counter animation on left hand
            if (_handAnimationController != null)
                _handAnimationController.PlayCounterAction();
        }

        // Direct waveform switching (1-4 keys)
        if (input.Wave1)
        {
            EquipWaveformByName("Sine");
        }
        if (input.Wave2)
        {
            EquipWaveformByName("Square");
        }
        if (input.Wave3)
        {
            EquipWaveformByName("Saw");
        }
        if (input.Wave4)
        {
            EquipWaveformByName("Triangle");
        }

        // Cycle waveforms (Q/E keys)
        if (input.NextWaveform)
        {
            CycleWaveform(1);
        }
        if (input.PrevWaveform)
        {
            CycleWaveform(-1);
        }

        // Fire projectile with raycast-based aiming
        if (input.Fire && _cameraTransform != null)
        {
            Vector3 aimDir = GetAimDirection();
            FireProjectile(aimDir);
        }
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
        Vector3 targetPoint;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimRaycastMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxAimDistance;
        }

        Vector3 direction = (targetPoint - _projectileSpawnPoint.position).normalized;
        return direction;
    }

    public void FireProjectile(Vector3 direction)
    {
        if (!_combatEntity.CanUseAttack()) return;

        // Play fire animation on right hand FIRST
        if (_handAnimationController != null)
        {
            _handAnimationController.PlayFireAnimation();
        }

        float damage = _combatEntity.CalculateDamage();
        if (_counterSystem != null)
            damage *= _counterSystem.GetDamageMultiplier();

        _combatEntity.ConsumeAttackResources();

        if (_combatEntity.equippedWaveform.projectilePrefab != null && _projectileSpawnPoint != null)
        {
            GameObject proj = Instantiate(_combatEntity.equippedWaveform.projectilePrefab,
                _projectileSpawnPoint.position,
                Quaternion.LookRotation(direction));

            // Handle hitscan
            if (_combatEntity.equippedWaveform.isHitscan)
            {
                HitscanProjectile hitscan = proj.GetComponent<HitscanProjectile>();
                if (hitscan != null)
                    hitscan.Initialize(damage, _combatEntity.equippedWaveform, _combatEntity, direction, _combatEntity.equippedWaveform.hitscanRange, _reticle);
            }
            // Handle regular projectiles
            else
            {
                WaveformProjectile projectile = proj.GetComponent<WaveformProjectile>();
                if (projectile != null)
                {
                    bool enableChain = _counterSystem != null && _counterSystem.HasChainAttack();
                    float chainRange = enableChain ? _counterSystem.GetChainRange() : 0f;

                    projectile.Initialize(damage, _combatEntity.equippedWaveform, _combatEntity, direction, _reticle, enableChain, chainRange);

                    if (enableChain)
                        _counterSystem.ConsumeChainAttack();
                }
            }

            if (_reticle != null)
                _reticle.OnFire();
        }
    }

    public void CycleWaveform(int direction)
    {
        if (unlockedWaveforms.Count == 0) return;

        currentWaveformIndex = (currentWaveformIndex + direction) % unlockedWaveforms.Count;
        if (currentWaveformIndex < 0) currentWaveformIndex = unlockedWaveforms.Count - 1;

        EquipWaveform(currentWaveformIndex);
        Debug.Log($"Switched to waveform: {_combatEntity.equippedWaveform.name}");
    }

    public void EquipWaveform(int index)
    {
        if (index >= 0 && index < unlockedWaveforms.Count)
        {
            WaveformData newWaveform = unlockedWaveforms[index];
            bool isInitial = (_combatEntity.equippedWaveform == null);

            // Check if this waveform is already equipped
            if (_combatEntity.equippedWaveform == newWaveform && !isInitial)
            {
                Debug.Log($"[PlayerCombat] {newWaveform.name} is already equipped, skipping switch.");
                return;
            }

            Debug.Log($"[PlayerCombat] EquipWaveform: {newWaveform.name}, IsInitial: {isInitial}");
            Debug.Log($"[PlayerCombat] HandAnimations is null? {newWaveform.handAnimations == null}");

            _combatEntity.equippedWaveform = newWaveform;
            currentWaveformIndex = index;

            _combatEntity.immuneToWaveforms.Clear();

            if (_reticle != null)
            {
                _reticle.UpdateReticleForWaveform(_combatEntity.equippedWaveform);
            }

            // Update right hand animations for the new waveform
            if (_handAnimationController != null && newWaveform.handAnimations != null)
            {
                if (isInitial)
                {
                    Debug.Log($"[PlayerCombat] Calling SetInitialWaveform for {newWaveform.name}");
                    _handAnimationController.SetInitialWaveform(newWaveform.handAnimations);
                }
                else
                {
                    Debug.Log($"[PlayerCombat] Calling SwitchWaveform to {newWaveform.name}");
                    _handAnimationController.SwitchWaveform(newWaveform.handAnimations);
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerCombat] Cannot update animations - HandAnimController: {_handAnimationController != null}, HandAnimations: {newWaveform.handAnimations != null}");
            }

            // Notify PlayerCharacter about waveform change for self-cast system
            if (_combatEntity is PlayerCharacter playerCharacter)
            {
                playerCharacter.OnWaveformSwitched(newWaveform);
            }
        }
    }

    public void EquipWaveformByName(string waveformName)
    {
        for (int i = 0; i < unlockedWaveforms.Count; i++)
        {
            if (unlockedWaveforms[i].waveformName.Equals(waveformName, System.StringComparison.OrdinalIgnoreCase))
            {
                EquipWaveform(i);
                return;
            }
        }

        Debug.LogWarning($"[PlayerCombat] Waveform '{waveformName}' not found or not unlocked.");
    }

    public void UnlockWaveform(WaveformData waveform)
    {
        if (!unlockedWaveforms.Contains(waveform))
        {
            unlockedWaveforms.Add(waveform);
        }
    }
}