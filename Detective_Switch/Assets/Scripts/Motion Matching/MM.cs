﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEditor.Animations;

public class MM : MonoBehaviour
{
    /* Main script for handling Motion Matching.
     * Load the feature list from the preprocesser,
     * and the desired trajectory from the TrajectoryCalculator,
     * and applies motion matching to the character.
     */

    // TODO: Take the player trajectory from player class, create a method for finding candidates (5 best?!) and comparing it to the player
    // trajectory, then whichever candidates pass the trajectory match, will be compared with posematching, where best becomes new (based on ID?)
    
    // --- References
    private MMPreProcessing preprocces;
    private TrajectoryTest movement;
    private TrajectoryPoint trajectoryPoint;
    private Trajectory trajectory;
    private List<Trajectory> trajectories;
    private MMPose pose;
    private Animator animator;

    // --- Public variables
    public AnimationClip currentClip;
    public int currentClipNum;
    public int currentFrame;

    // --- Private variables
    private AnimationClip[] allClips;
    private float normalizedFrameTime;

    void Start()
    {
        // Data is loaded from the MMPreProcessor
        preprocces = GetComponent<MMPreProcessing>();
        movement = GetComponent<TrajectoryTest>();

        // Animator is initialized
        animator = GetComponent<Animator>();
        allClips = animator.runtimeAnimatorController.animationClips;

        // Populate trajectory
        trajectories = new List<Trajectory>();
        //string firstClipName = "";  Can be updated at break to only use csv data
        int iterator = 0;
        for (int i = 0; i < allClips.Length; i++)
        {
            List<TrajectoryPoint> tempTrajPoints = new List<TrajectoryPoint>();
            for (int j =  0; j < preprocces.trajectoryPoints.Count; j++)
            {
                if (preprocces.clipNames[j] == allClips[i].name || iterator < preprocces.trajectoryPoints.Count)
                {
                    tempTrajPoints.Add(preprocces.trajectoryPoints[j]);
                    iterator++;
                }
                else // New clip - don't save trajectory points from previous clip
                    break;
            }
            trajectories.Add(new Trajectory(allClips[i].name, (int)(allClips[i].length * allClips[i].frameRate), i, tempTrajPoints.ToArray()));
        }

        // Play the default animation and update the reference
        currentClip = allClips[1];
        currentFrame = 20;
        PlayAnimationAtFrame(currentClip.name, currentFrame / currentClip.frameRate);
        Debug.Log("Framerate: " + currentClip.frameRate + ". Length: " + currentClip.length);
        Debug.Log("Current frame is: " + currentFrame + ". CurrentClip has " + currentClip.length * currentClip.frameRate + " frames." +
            "result of input is: " + currentFrame / currentClip.frameRate);
        //animator.Play(allClips[1].name, 0, allClips[1].length * );

        Debug.Log("testing " + trajectories[currentClipNum].GetTrajectoryPoints().Length);

        //for (int i = 0; i < trajectories[currentClipNum].GetTrajectoryPoints().Length; i++)
        //{
        //}
        //Gizmos.DrawWireSphere(transform.position + trajectories[currentClipNum].GetTrajectoryPoints()[currentFrame + i].position * movement.trajPoints[i]); // Pos
    }

    // Update is called once per frame
    void LateUpdate()
    {

    }

    /// <summary>
    /// First find the best matches for curve matching, then find the best pose in these curve animations
    /// Implement a culling of recently played animation frames.
    /// </summary>
    
    void ComputeCost()
    {
        // Weights
    }

    void TrajectoryMatching()
    {

    }

    void PoseMatching()
    {

    }

    void PlayAnimationAtFrame(string animation, float normalizedTime)
    {
        animator.Play(animation, 0, normalizedTime);
        UpdateCurrentClip(animation, normalizedTime);
    }

    void UpdateCurrentClip(string nameOfNewClip, float time)
    {
        for (int i = 0; i < allClips.Length; i++)
        {
            if (allClips[i].name == nameOfNewClip)
            {
                Debug.Log("Current clip has changed from " + currentClip.name + " to " + allClips[i]);
                currentClip = allClips[i];
                currentClipNum = i;
                Debug.Log("Clip num is now: " + currentClipNum);
                currentFrame = (int)(time * currentClip.frameRate);
                return;
            }
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < movement.trajPoints.Length; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + trajectories[currentClipNum].GetTrajectoryPoints()[currentFrame + i].position * movement.trajPoints[i] / movement.gizmoSphereSpacing, movement.gizmoSphereSize); // Pos
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + trajectories[currentClipNum].GetTrajectoryPoints()[currentFrame + i].forward * movement.trajPoints[i], movement.gizmoSphereSize); // Forward
        }
    }
}

//AnimatorController controller = GetComponent<Animator>().runtimeAnimatorController;

//AnimatorStateInfo currentAnimatorStateInfo;
//float playbackTime = currentAnimatorStateInfo.normalizedTime * currentAnimatorStateInfo.length;

//AnimationClip[] manyClips = controller.animationClips;
//List<AnimationClip> allClips = new List<AnimationClip>();

// NEED A SMARTER WAY TO ITERATE THROUGHT THE AMOUNT OF CLIPS (STATES) IN AN ANIMATION
//for (int i = 0; i < manyClips.Length; i++)
//{
//Debug.Log(manyClips[i].);
//allClips = controller.animationClips;
//currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(i);
//Debug.Log("Name of animation " + i + ": " + currentAnimatorStateInfo.nameHash);
//Debug.Log("Length of animation " + i +  ": " + currentAnimatorStateInfo.length);
//}

//preprocces = new MMPreProcessing(animator.GetCurrentAnimatorClipInfo)