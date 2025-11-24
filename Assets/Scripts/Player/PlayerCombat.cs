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

    public void Initialize(Transform cameraTransform, Reticle reticle, CounterSystem counterSystem, CombatEntity combatEntity, Transform projectileSpawnPoint)
    {
        _cameraTransform = cameraTransform;
        _reticle = reticle;
        _counterSystem = counterSystem;
        _combatEntity = combatEntity;
        _projectileSpawnPoint = projectileSpawnPoint;

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
        }

        // Switch waveforms
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

        // Self modifier (includes dash)
        if (input.SelfModifier)
        {
            _combatEntity.ApplySelfModifier();
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
            _combatEntity.equippedWaveform = unlockedWaveforms[index];
            currentWaveformIndex = index;

            _combatEntity.immuneToWaveforms.Clear();

            if (_reticle != null)
            {
                _reticle.UpdateReticleForWaveform(_combatEntity.equippedWaveform);
            }
        }
    }

    public void UnlockWaveform(WaveformData waveform)
    {
        if (!unlockedWaveforms.Contains(waveform))
        {
            unlockedWaveforms.Add(waveform);
        }
    }
}