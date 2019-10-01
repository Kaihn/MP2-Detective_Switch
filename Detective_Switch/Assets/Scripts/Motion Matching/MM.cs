﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEditor.Animations;
using UnityEngine.UIElements;

public class MM : MonoBehaviour
{
    /* Main script for handling Motion Matching.
     * Load the feature list from the preprocessor,
     * and the desired trajectory from the TrajectoryCalculator,
     * and applies motion matching to the character.
     */

    // TODO: First find the best matches for curve matching, then find the best pose in these curve animations

    // TODO: Implement a culling of recently played animation frames.

    // TODO: Take the player trajectory from player class (DONE), create a method for finding candidates (5 best?!) and comparing it to the player
    // TODO: trajectory, then whichever candidates pass the trajectory match, will be compared with posematching, where best becomes new (based on ID?)

    // --- References
    private MMPreProcessing preprocess;
    private TrajectoryTest movement;
    private Trajectory trajectory;
    private List<Trajectory> animTrajectories;
    private Trajectory movementTrajectory;
    private MMPose pose;
    private Animator animator;

    // --- Public variables
    public AnimationClip currentClip;
    public int currentFrame;
    public int currentAnimId;
    public float queryRate;
    public float comparisonThreshold = 50;

    // --- Private variables
    [SerializeField] bool isMMRunning;
    [SerializeField] int framesToCull = 10;
    private AnimationClip[] allClips;
    private float normalizedFrameTime;
    private int candidateId;
    private List<Trajectory> candidates;


    void Start()
    {
        // Data is loaded from the MMPreProcessor
        preprocess = GetComponent<MMPreProcessing>();
        movement = GetComponent<TrajectoryTest>();


        // Animator is initialized
        animator = GetComponent<Animator>();
        allClips = animator.runtimeAnimatorController.animationClips;

        // Populate trajectory
        animTrajectories = preprocess.GetTrajectories();
        movementTrajectory = new Trajectory(movement.GetMovementTrajectoryPoints());


        // Play the default animation and update the reference
        currentClip = allClips[0];
        currentFrame = 0;
        currentAnimId = 0;
        PlayAnimationAtFrame(currentClip.name, currentFrame / currentClip.frameRate, 0);

        // TODO: Make coroutines for starting mm, idle, and walking, as well as stopping mm
        StartCoroutine(StartMM());
    }

    List<Trajectory> TrajectoryMatching(Trajectory movement, float threshold)
    {
	    List<Trajectory> trajectoryCandidates = new List<Trajectory>();
		// Transform animation trajectories to character space
		//for (int i = 0; i < animTrajectories.Count; i++)
		//{
	 //       TrajectoryPoint[] tempPoints = new TrajectoryPoint[preprocess.trajectoryPointsToUse];
	 //       for (int j = 0; j < tempPoints.Length; j++)
	 //       {
		//        tempPoints[j] = new TrajectoryPoint(); // Initialize
	 //       }
  //          for (int j = 0; j < preprocess.trajectoryPointsToUse; j++)
	 //       {
		//		tempPoints[j].position = transform.worldToLocalMatrix.MultiplyPoint3x4(animTrajectories[i].GetTrajectoryPoints()[j].position);
		//		tempPoints[j].forward = transform.worldToLocalMatrix.MultiplyVector(animTrajectories[i].GetTrajectoryPoints()[j].forward);
  //          }
  //          trajectoryCandidates.Add(new Trajectory(animTrajectories[i].GetClipName(), animTrajectories[i].GetFrame(), animTrajectories[i].GetTrajectoryId(), tempPoints));
  //      }
        for (int i = 0; i < trajectoryCandidates.Count; i++)
        {
            if (trajectoryCandidates[i].GetTrajectoryId() >= 0 && trajectoryCandidates[i].GetTrajectoryId() != currentAnimId)
            {
                if (trajectoryCandidates[i].CompareTrajectoryPoints(movement) +
                    trajectoryCandidates[i].CompareTrajectoryForwards(movement) < threshold)
                {
                    trajectoryCandidates.Add(trajectoryCandidates[i]);
                }
            }
        }
        return trajectoryCandidates;
    }

    private int PoseMatching(List<Trajectory> candidates)
    {
        float poseDistance = float.MaxValue;
        int newId = currentAnimId;
        foreach (var candidate in candidates)
        {
            float candidateDistance = preprocess.poses[candidate.GetTrajectoryId()].ComparePose(new MMPose(
                movement.root.position, movement.lFoot.position, movement.rFoot.position,
                movement.rootVel, movement.lFootVel, movement.rFootVel));
            if (candidateDistance < poseDistance)
            {
                poseDistance = candidateDistance;
                newId = candidate.GetTrajectoryId();
            }
        }

        return newId;
    }

    void PlayAnimationAtFrame(string animName, float normalizedTime, int animId)
    {
        animator.CrossFadeInFixedTime(animName, 0.3f, 0, normalizedTime); // 0.3f was recommended by Magnus
        UpdateCurrentClip(animName, normalizedTime, animId);
    }

    void UpdateCurrentClip(string nameOfNewClip, float time, int clipId)
    {
        for (int i = 0; i < allClips.Length; i++)
        {
            if (allClips[i].name == nameOfNewClip)
            {
                Debug.Log("Current clip has changed from " + currentClip.name + " to " + allClips[i].name); // Keep
                currentClip = allClips[i];
                currentFrame = (int)(time * currentClip.frameRate);
                currentAnimId = clipId;
                return;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < animTrajectories[currentAnimId].GetTrajectoryPoints().Length; i++)
            {
                Gizmos.DrawWireSphere(animTrajectories[currentAnimId].GetTrajectoryPoints()[i].position, movement.gizmoSphereSize);
                Gizmos.DrawLine(animTrajectories[currentAnimId].GetTrajectoryPoints()[i].position, animTrajectories[currentAnimId].GetTrajectoryPoints()[i].position + animTrajectories[currentAnimId].GetTrajectoryPoints()[i].forward);
            }
        }
    }

    private void CandidatesToCull() 
    {
        
    }

    private IEnumerator StartMM()
    {
        // TODO: Add a banlist - discard x previous frames
        isMMRunning = true;
        while (true)
        {
            if (movement.rootVel.Equals(Vector3.zero))
                StartCoroutine(StartIdle());
            movementTrajectory = new Trajectory(movement.GetMovementTrajectoryPoints());
            candidates = TrajectoryMatching(movementTrajectory, comparisonThreshold);
            candidateId = PoseMatching(candidates);
            PlayAnimationAtFrame(preprocess.poses[candidateId].GetClipName(), (float)preprocess.poses[candidateId].GetFrame() / 1 / 30, candidateId);
            yield return new WaitForSeconds(queryRate);
        }
    }

    private IEnumerator StopMM()
    {
        yield return new WaitForSeconds(queryRate);
    }
    private IEnumerator StartIdle()
    {
        Debug.Log("Idling");
        PlayAnimationAtFrame(preprocess.poses[0].GetClipName(), (float) preprocess.poses[0].GetFrame() / 1 / 30, 0);
        yield return new WaitForSeconds(1);
    }
    private IEnumerator StartMovement()
    {
        yield return new WaitForSeconds(queryRate);
    }
}