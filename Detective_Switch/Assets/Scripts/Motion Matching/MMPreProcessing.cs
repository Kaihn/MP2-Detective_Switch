﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MMPreProcessing : MonoBehaviour
{
    /* The purpose of this script is to process and store the
     * pose and trajectory in a feature list, which must cached
     * such that it can simply be read when the game starts.
     * However, initially we will simply test this by preprocessing
     * on start (or awake).
     *
     * Pose contains relevant joints (feet, neck), relevant velocities,
     * and other information regarding the joints in the rig.
     * Trajectory contains relevant information about the current trajectory
     * when playing a specific animation. It contains the time steps that is
     * being processed, and the amount of time to look in advance.
     */

    // --- References
    private Animator animator;
    [HideInInspector] public List<MMPose> poses;
    [HideInInspector] public List<TrajectoryPoint> trajectoryPoints;
    [HideInInspector] public List<Trajectory> trajectories;

    // --- Inspector
    public List<AnimationClip> clips;
    public List<string> jointNames;
    public bool preprocess = false;
    public int trajectoryPointsToUse = 5;
    public int frameStepSize = 25;

    // --- Not in Inspector
    [HideInInspector]
    public List<string> clipNames;
    [HideInInspector]
    public List<int> clipFrames;

    private List<Vector3> rootPos, lFootPos, rFootPos;
    private List<Quaternion> rootQ;
    // Note: We are having trouble tracking the position of the neck, since it is in muscle space.

    private void Awake()
    {
        InitCollections();
        CSVReaderWriter csvHandler = new CSVReaderWriter();
        if (preprocess)
        {
            foreach (AnimationClip clip in clips)
            {
                // Initialize lists again to avoid reusing previous data
                clipNames = new List<string>();
                clipFrames = new List<int>();
                rootPos = new List<Vector3>();
                lFootPos = new List<Vector3>();
                rFootPos = new List<Vector3>();
                rootQ = new List<Quaternion>();
                for (int i = 0; i < clip.length * clip.frameRate; i++)
                {
                    clipNames.Add(clip.name);
                    clipFrames.Add(i);
                    // Adding root data to list
                    rootPos.Add(GetJointPositionAtFrame(clip, i, jointNames[0]));
                    rootQ.Add(GetJointQuaternionAtFrame(clip, i, jointNames[1]));
                    // Creating a root transform matrix, then multiplying its inverse to transform joints to character space
                    Matrix4x4 rootTrans = Matrix4x4.identity;
                    Quaternion quart = rootQ[i];
                    quart.eulerAngles = new Vector3(rootQ[i].eulerAngles.x, rootQ[i].eulerAngles.y, rootQ[i].eulerAngles.z);
                    rootTrans.SetTRS(rootPos[i], quart, new Vector3(1, 1, 1));
                    rootQ[i] = quart;
                    lFootPos.Add(rootTrans.inverse.MultiplyPoint3x4(GetJointPositionAtFrame(clip, i, jointNames[2])));
                    rFootPos.Add(rootTrans.inverse.MultiplyPoint3x4(GetJointPositionAtFrame(clip, i, jointNames[3])));

                    // Add pose data to list
                    if (i > 0)
                    {
                        poses.Add(new MMPose(clip.name, i,
                            rootPos[i], lFootPos[i], rFootPos[i],
                            CalculateVelocityFromVectors(rootPos[i], rootPos[i - 1]),
                            CalculateVelocityFromVectors(lFootPos[i], lFootPos[i - 1]),
                            CalculateVelocityFromVectors(rFootPos[i], rFootPos[i - 1]),
                            rootQ[i]));
                    }
                    else // There is no previous position for velocity calculation at frame 0
                    {
                        poses.Add(new MMPose(clip.name, i,
                            rootPos[i], lFootPos[i], rFootPos[i],
                            CalculateVelocityFromVectors(rootPos[i], new Vector3(0, 0, 0)),
                            CalculateVelocityFromVectors(lFootPos[i], new Vector3(0, 0, 0)),
                            CalculateVelocityFromVectors(rFootPos[i], new Vector3(0, 0, 0)),
                            rootQ[i]));
                    }
                    trajectoryPoints.Add(new TrajectoryPoint(rootPos[i], rootQ[i] * Vector3.forward));
                }
            }
            csvHandler.WriteCSV(poses, trajectoryPoints);
        }

        InitCollections();
        csvHandler.ReadCSV();
        for (int i = 0; i < csvHandler.GetClipNames().Count; i++)
        {
            clipNames.Add(csvHandler.GetClipNames()[i]);
            clipFrames.Add(csvHandler.GetFrames()[i]);

            // Read pose data and store it in a list of poses
            poses.Add(new MMPose(csvHandler.GetClipNames()[i], csvHandler.GetFrames()[i],
                csvHandler.GetRootPos()[i], csvHandler.GetLeftFootPos()[i], csvHandler.GetRightFootPos()[i],
                csvHandler.GetRootVel()[i], csvHandler.GetLeftFootVel()[i], csvHandler.GetRightFootVel()[i]));

            // To add the trajectory data, first compute the trajectory points for each frame
            TrajectoryPoint[] tempPoints = new TrajectoryPoint[trajectoryPointsToUse];
            if (i + (frameStepSize * trajectoryPointsToUse) < csvHandler.GetClipNames().Count && // Avoid out-of-bounds error
                csvHandler.GetClipNames()[i] == csvHandler.GetClipNames()[i + (frameStepSize * trajectoryPointsToUse)]) // Make sure frames belong to the same clip
            {
                for (int point = 0; point < tempPoints.Length; point++)
                {
                    tempPoints[point] = new TrajectoryPoint(csvHandler.GetTrajectoryPos()[i + (point * frameStepSize)],
                        csvHandler.GetTrajectoryForwards()[i + (point * frameStepSize)]);
                }
                trajectories.Add(new Trajectory(csvHandler.GetClipNames()[i], csvHandler.GetFrames()[i], i, tempPoints));
            }
            else
            {
                for (int j = 0; j < trajectoryPointsToUse; j++)
                    tempPoints[j] = new TrajectoryPoint();
                trajectories.Add(new Trajectory(tempPoints));
            }
        }
    }

    private void InitCollections()
    {
        poses = new List<MMPose>();
        trajectoryPoints = new List<TrajectoryPoint>();
        clipNames = new List<string>();
        clipFrames = new List<int>();
        rootPos = new List<Vector3>();
        lFootPos = new List<Vector3>();
        rFootPos = new List<Vector3>();
        rootQ = new List<Quaternion>();
    }

    public List<Trajectory> GetTrajectories()
    {
        return trajectories;
    }

    public Vector3 GetJointPositionAtFrame(AnimationClip clip, int frame, string jointName)
    {
        // Bindings are inherited from a clip, and the AnimationCurve is inherited from the clip's binding
        float[] vectorValues = new float[3];
        int arrayEnumerator = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.propertyName.Contains(jointName))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                vectorValues[arrayEnumerator] = curve.Evaluate(frame / clip.frameRate);
                arrayEnumerator++;
            }
        }
        return new Vector3(vectorValues[0], vectorValues[1], vectorValues[2]);
    }

    public Quaternion GetJointQuaternionAtFrame(AnimationClip clip, int frame, string jointName)
    {
        /// Bindings are inherited from a clip, and the AnimationCurve is inherited from the clip's binding
        AnimationCurve curve = new AnimationCurve();
        float[] vectorValues = new float[4];
        int arrayEnumerator = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.propertyName.Contains(jointName))
            {
                curve = AnimationUtility.GetEditorCurve(clip, binding);
                vectorValues[arrayEnumerator] = curve.Evaluate(frame / clip.frameRate);
                arrayEnumerator++;
            }
        }
        return new Quaternion(vectorValues[0], vectorValues[1], vectorValues[2], vectorValues[3]);
    }

    public Vector3 CalculateVelocityFromVectors(Vector3 currentPos, Vector3 prevPos)
    {
        return (currentPos - prevPos) / 1 / 30;
    }

    private List<string> GetClipNameFromClip(AnimationClip clip)
    {
        List<string> _clipNames = new List<string>();

        for (int i = 0; i < clip.length * clip.frameRate; i++)
        {
            _clipNames.Add(clip.name);
        }

        return _clipNames;
    }

    private List<int> GetFramesFromClip(AnimationClip clip)
    {
        List<int> _frames = new List<int>();
        int currentFrame = 0;
        int indexer = 0;

        for (int i = 0; i < clip.length * clip.frameRate; i++)
        {
            indexer++;
            if (indexer == clip.frameRate)
            {
                currentFrame++;
                indexer = 0;
            }

            _frames.Add(currentFrame);
        }

        return _frames;
    }
}