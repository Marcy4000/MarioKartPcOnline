using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceCounter : MonoBehaviour
{
    public static PlaceCounter instance { get; private set; }
    [SerializeField] private Animator animator;
    public KartLap[] karts;
    private LapCheckPoint[] checkPoints;
    private Transform finishLine;

    // Animation names for positions (for backward compatibility with existing animations)
    private string[] positionAnimations = { 
        "first", "second", "third", "fourth", "fifth", "sixth", 
        "seventh", "eighth", "nineth", "tenth", "eleventh", "twelveth" 
    };

    private void Start()
    {
        instance = this;
        StartCoroutine(GetKartsAndCheckpoints());
    }

    IEnumerator GetKartsAndCheckpoints()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        yield return new WaitForSeconds(0.2f);

        karts = FindObjectsOfType<KartLap>();
        checkPoints = FindObjectsOfType<LapCheckPoint>();
        finishLine = FindObjectOfType<LapHandle>().transform;
    }

    public int GetCurrentPlace(KartLap targetKart)
    {
        // This method is now only used as fallback when RaceStateManager is not available
        // The centralized system handles position calculation
        if (RaceStateManager.instance != null)
        {
            // Return current position from centralized system
            return targetKart.racePlace;
        }

        // Fallback to old local calculation
        int currPlace = 1; // Start at 1st place

        foreach (var kart in karts)
        {
            if (kart == targetKart)
            {
                continue;
            }

            // Check if this kart is ahead of the target kart
            bool isAhead = false;

            if (kart.lapNumber > targetKart.lapNumber)
            {
                isAhead = true;
            }
            else if (kart.lapNumber == targetKart.lapNumber)
            {
                if (kart.CheckpointIndex > targetKart.CheckpointIndex)
                {
                    isAhead = true;
                }
                else if (kart.CheckpointIndex == targetKart.CheckpointIndex)
                {
                    // Same checkpoint, check distance to next checkpoint
                    float targetDistance = Vector3.Distance(targetKart.transform.position, GetCheckpointTransformByIndex(targetKart.CheckpointIndex + 1).position);
                    float kartDistance = Vector3.Distance(kart.transform.position, GetCheckpointTransformByIndex(kart.CheckpointIndex + 1).position);
                    
                    if (kartDistance < targetDistance)
                    {
                        isAhead = true;
                    }
                }
            }

            if (isAhead)
            {
                currPlace++;
            }
        }

        return currPlace;
    }

    public LapCheckPoint GetCheckpointByIndex(int index)
    {
        foreach (var checkpoint in checkPoints)
        {
            if (checkpoint.Index == index)
            {
                return checkpoint;
            }
        }
        return checkPoints[0];
    }

    public Transform GetCheckpointTransformByIndex(int index)
    {
        foreach (var checkpoint in checkPoints)
        {
            if (checkpoint.Index == index)
            {
                return checkpoint.transform;
            }
        }
        return finishLine;
    }

    public void ChangePosition(int position)
    {
        // Convert 1-based position to 0-based array index
        int animIndex = position - 1;

        // Ensure we don't go out of bounds
        if (animIndex >= 0 && animIndex < positionAnimations.Length)
        {
            animator.Play(positionAnimations[animIndex]);
        }
        else
        {
            // For positions beyond our predefined animations, use the last one
            animator.Play(positionAnimations[positionAnimations.Length - 1]);
            Debug.LogWarning($"Position {position} exceeds predefined animations. Using last animation instead.");
        }
    }
}
