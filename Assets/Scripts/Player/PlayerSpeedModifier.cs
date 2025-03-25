using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles speed modifications for the PlayerController
/// </summary>
public class PlayerSpeedModifier : MonoBehaviour
{
    private PlayerController playerController;
    private float originalMoveSpeed;
    private float originalSprintMultiplier;
    private Dictionary<string, SpeedModifier> activeModifiers = new Dictionary<string, SpeedModifier>();

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerSpeedModifier requires a PlayerController component");
            enabled = false;
            return;
        }

        // Store original speed values
        originalMoveSpeed = playerController.MoveSpeed;
        originalSprintMultiplier = playerController.SprintMultiplier;
    }

    /// <summary>
    /// Apply a speed modifier to the player
    /// </summary>
    /// <param name="modifierValue">Value between 0-1 (0.5 = 50% normal speed)</param>
    /// <param name="duration">How long the modifier lasts in seconds</param>
    /// <param name="id">Unique ID for this modifier (for stacking control)</param>
    public void ApplySpeedModifier(float modifierValue, float duration = 2f, string id = "default")
    {
        // Cancel existing modifier with same ID if it exists
        if (activeModifiers.ContainsKey(id))
        {
            if (activeModifiers[id].coroutine != null)
            {
                StopCoroutine(activeModifiers[id].coroutine);
            }
        }

        // Create new modifier
        SpeedModifier modifier = new SpeedModifier
        {
            value = modifierValue,
            duration = duration
        };

        // Store modifier
        activeModifiers[id] = modifier;

        // Start duration coroutine
        modifier.coroutine = StartCoroutine(RemoveModifierAfterDuration(id, duration));

        // Recalculate speed
        CalculateSpeed();
    }

    /// <summary>
    /// Remove a specific speed modifier by ID
    /// </summary>
    public void RemoveSpeedModifier(string id = "default")
    {
        if (activeModifiers.ContainsKey(id))
        {
            if (activeModifiers[id].coroutine != null)
            {
                StopCoroutine(activeModifiers[id].coroutine);
            }

            activeModifiers.Remove(id);
            CalculateSpeed();
        }
    }

    /// <summary>
    /// Remove all speed modifiers
    /// </summary>
    public void ClearAllSpeedModifiers()
    {
        foreach (var modifier in activeModifiers.Values)
        {
            if (modifier.coroutine != null)
            {
                StopCoroutine(modifier.coroutine);
            }
        }

        activeModifiers.Clear();
        ResetSpeed();
    }

    /// <summary>
    /// Reset speed to original values
    /// </summary>
    public void ResetSpeed()
    {
        if (playerController != null)
        {
            playerController.MoveSpeed = originalMoveSpeed;
            playerController.SprintMultiplier = originalSprintMultiplier;
        }
    }

    private IEnumerator RemoveModifierAfterDuration(string id, float duration)
    {
        yield return new WaitForSeconds(duration);

        RemoveSpeedModifier(id);
    }

    private void CalculateSpeed()
    {
        if (playerController == null) return;

        // Start with original speed
        float finalSpeedMultiplier = 1f;

        // Apply all modifiers (multiplicative)
        foreach (var modifier in activeModifiers.Values)
        {
            finalSpeedMultiplier *= modifier.value;
        }

        // Apply to player controller
        playerController.MoveSpeed = originalMoveSpeed * finalSpeedMultiplier;

        // Optionally also modify sprint speed
        playerController.SprintMultiplier = originalSprintMultiplier * finalSpeedMultiplier;
    }

    // Structure to hold modifier data
    private class SpeedModifier
    {
        public float value;
        public float duration;
        public Coroutine coroutine;
    }
}