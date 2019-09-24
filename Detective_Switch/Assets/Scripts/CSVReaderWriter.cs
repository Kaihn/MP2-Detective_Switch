﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;


public class CSVReaderWriter // : MonoBehaviour
{
    [Header("Tip: right-click and 'Copy Path' in project tab")]
    public string CSVReadPath = "Assets/Resources/CSV/AnimData.csv";
    public string CSVWritePath = "Assets/Resources/CSV";
    [Tooltip("Specify the file name without writing .csv - This will be appended automatically")]
    public string CSVFileName = "AnimData";

    // Variable holders from reading CSV files:
    private string[] labels;
    private List<int> frames;
    private List<Vector3> rootPositions;
    private List<Quaternion> rootQuaternions;
    private List<Vector3> leftFootPositions;
    private List<Vector3> rightFootPositions;
    private List<Vector3> leftFootVelocity;
    private List<Vector3> rightFootVelocity;
    private List<Vector3> rootVelocity;
    private AnimationClip animClips;

#if UNITY_EDITOR
    void Awake()
    {
        // ReadCSV();
        // CalculateColoumns(8, 3, 1);
        // CSVWriteTester();
    }
#endif

    public void ReadCSV()
    {
        if (CSVReadPath == null)
        {
            Debug.LogError("CSV Reader/Writer Error: CSV read path empty!");
        }
        else
        {
            ReadCSV(CSVReadPath);
        }

    }

    public void ReadCSV(string path)
    {

        StreamReader strReader = new StreamReader(path);

        bool endOfFile = false;
        bool firstRun = true;

        while (!endOfFile)
        {
            string dataString = strReader.ReadLine();

            if (dataString == null)
            {
                endOfFile = true;
                break;
            }

            if (firstRun)
            {
                string[] tempLabelValues = dataString.Split(',');
                labels = tempLabelValues;

                /// Initialize thelists for quaternions, positions etc:
                InitializeLists();

                firstRun = false;
            }
            else
            {
                string[] tempDataValues = dataString.Split(',');

                float[] dataValues = new float[tempDataValues.Length];
                string stateValues = "";
                for (int i = 1; i < dataValues.Length; i++)
                {
                    if (i == dataValues.Length - 1)
                        stateValues = tempDataValues[i];
                    else
                    {
                        dataValues[i] = float.Parse(tempDataValues[i], CultureInfo.InvariantCulture.NumberFormat);
                    }

                }

                /// Populate the quaternion, position and timestamp arrays/lists:
                QuaternionCreator(dataValues);
                PositionCreator(dataValues);
                FloatIntCreator(dataValues);

            }

        }
        Debug.Log("CSV Reader/Writer: reading completed");

    }

    private void PositionCreator(float[] data)
    {
        for (int i = 0; i < labels.Length; i++)   // Vector starts from position 1
        {
            if (labels[i].Contains("FootLeftT.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                // Debug.Log(data[i] + " " + data[i + 1] + " " + data[i + 2]);  // For debugging (very performance heavy)
                leftFootPositions.Add(tempPosition);
            }
            if (labels[i].Contains("FootRightT.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                rightFootPositions.Add(tempPosition);
            }
            if (labels[i].Contains("RootT.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                rootPositions.Add(tempPosition);
            }
            if (labels[i].Contains("RootV.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                rootVelocity.Add(tempPosition);
            }
            if (labels[i].Contains("FootLeftV.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                leftFootVelocity.Add(tempPosition);
            }
            if (labels[i].Contains("FootLeftV.x"))
            {
                Vector3 tempPosition = new Vector3(data[i], data[i + 1], data[i + 2]);
                rightFootVelocity.Add(tempPosition);
            }
        }
    }

    private void QuaternionCreator(float[] data)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].Contains("RootQ.x"))
            {
                Quaternion tempQuaternion = new Quaternion(data[i], data[i + 1], data[i + 2], data[i + 3]);
                rootQuaternions.Add(tempQuaternion);
            }
        }
    }

    private void FloatIntCreator(float[] data)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].Contains("Frame"))
            {
                int tempInt = (int)data[i];
                frames.Add(tempInt);
            }
        }
    }

    public void WriteCSV(List<string> _clipName, List<int> _frame, List<Vector3> _rootPos, List<Quaternion> _rootRot, List<Vector3> _footLeft, List<Vector3> _footRight, List<Vector3> _footLeftVel, List<Vector3> _footRightVel, List<Vector3> _rootVel)
    {
        if (CSVWritePath == null || CSVFileName == null)
        {
            Debug.LogError("CSV Reader/Writer Error: CSV write path or file name empty!");
        }
        else if (_frame.Count <= 0 || _frame == null)
        {
            Debug.LogError("CSV Reader/Writer Error: attempt to write CSV file using empty lists!");
        }
        else
        {
            CSVFileName = CSVFileName + ".csv";
            CSVWritePath = CSVWritePath + "/" + CSVFileName;
            Debug.Log("CSV Reader/Writer: writing lists to CSV file " + CSVFileName);

            using (var file = File.CreateText(CSVWritePath))
            {
                labels = new string[24] {"ClipName", "Frame", "RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w",
                "FootLeftT.x","FootLeftT.y","FootLeftT.z", "FootRightT.x","FootRightT.y","FootRightT.z","FootLeftV.x","FootLeftV.y",
                "FootLeftV.z","FootRightV.x","FootRightV.y","FootRightV.z","rootV.x","rootV.x","rootV.x"};

                file.WriteLine(string.Join(",", labels));

                string spec;
                CultureInfo cul;

                spec = "G";
                cul = CultureInfo.CreateSpecificCulture("en-US");

                for (int i = 0; i < _frame.Count; i++)
                {
                    string[] tempLine = new string[24] {_clipName[i], _frame[i].ToString(), _rootPos[i].x.ToString(spec, cul), _rootPos[i].y.ToString(spec, cul), _rootPos[i].z.ToString(spec, cul), _rootRot[i].x.ToString(spec, cul),
                    _rootRot[i].y.ToString(spec, cul), _rootRot[i].z.ToString(spec, cul), _rootRot[i].w.ToString(spec, cul), _footLeft[i].x.ToString(spec, cul), _footLeft[i].y.ToString(spec, cul), _footLeft[i].z.ToString(spec, cul),
                    _footRight[i].x.ToString(spec, cul), _footRight[i].y.ToString(spec, cul), _footRight[i].z.ToString(spec, cul), _footLeftVel[i].x.ToString(spec, cul), _footLeftVel[i].y.ToString(spec, cul), _footLeftVel[i].z.ToString(spec, cul),
                    _footRightVel[i].x.ToString(spec, cul), _footRightVel[i].y.ToString(spec, cul), _footRightVel[i].z.ToString(spec, cul), _rootVel[i].x.ToString(spec, cul), _rootVel[i].y.ToString(spec, cul), _rootVel[i].z.ToString(spec, cul)};

                    file.WriteLine(string.Join(",", tempLine));
                }

            }
        }

    }

    public void WriteCSV(List<string> _clipName, List<int> _frame, List<Vector3> _rootPos, List<Vector3> _footLeft, List<Vector3> _footRight, List<Vector3> _footLeftVel, List<Vector3> _footRightVel, List<Vector3> _rootVel)
    {
        List<Quaternion> _rootRotPlaceholder = new List<Quaternion>();

        for (int i = 0; i < _frame.Count; i++)
        {
            _rootRotPlaceholder.Add(new Quaternion(0, 0, 0, 0));
        }

        WriteCSV(_clipName, _frame, _rootPos, _rootRotPlaceholder, _footLeft, _footRight, _footLeftVel, _footRightVel, _rootVel);
    }


    /// THIS IS FOR DEBUGGING ///
    public void CSVWriteTester(List<Vector3> _rootPos, List<Quaternion> _rootRot, List<Vector3> _footLeft, List<Vector3> _footRight, List<Vector3> _footLeftVel, List<Vector3> _footRightVel, List<Vector3> _rootVel)
    {

        List<string> clipNamePlaceholder = new List<string>();
        List<int> framePlaceholder = new List<int>();

        for (int i = 0; i < _rootPos.Count; i++)
        {
            clipNamePlaceholder.Add("temp");
            framePlaceholder.Add(1);
        }

        WriteCSV(clipNamePlaceholder, framePlaceholder, _rootPos, _rootRot, _footLeft, _footRight, _footLeftVel, _footRightVel, _rootVel);
    } 

    private void InitializeLists()
    {
        rootQuaternions = new List<Quaternion>();
        rootPositions = new List<Vector3>();
        leftFootPositions = new List<Vector3>();
        rightFootPositions = new List<Vector3>();
        leftFootVelocity = new List<Vector3>();
        rightFootVelocity = new List<Vector3>();
        frames = new List<int>();
    }


    private void CalculateColoumns(int _amountColumns, int _amountVectors, int _amountQuaternions)
    {
        int tempColAmount;
        tempColAmount = (_amountColumns + (_amountVectors * 3) + (_amountQuaternions * 4)) - (_amountQuaternions + _amountVectors);
        Debug.Log(tempColAmount);
    }

    public void IndexHelper(int index)
    {
        if (index > -1 && index < labels.Length)
        {
            Debug.Log("CSV index " + index + " is the " + labels[index]);
        }
        else
        {
            Debug.LogError("CSV Reader/Writer Error: index helper out of bounds!");
        }
    }

    public void IndexHelper(string all)
    {
        if (all == "all")
        {
            for (int i = 0; 0 < labels.Length; i++)
            {
                Debug.Log("CSV index " + i + " is the " + labels[i]);
            }

        }
        else
        {
            Debug.Log("CSV Reader/Writer Notice: Index helper can only take specific index numbers or 'all' if you want to see all joints");
        }
    }

    public string GetCSVReadPath()
    {
        return CSVReadPath;
    }

    public string GetCSVFileName()
    {
        return CSVFileName;
    }

    public string GetCSVWritePath()
    {
        return CSVWritePath;
    }

    public string[] GetLabels()
    {
        return labels;
    }

    public List<Vector3> GetLeftFootPos()
    {
        return leftFootPositions;
    }

    public List<Vector3> GetRightFootPos()
    {
        return rightFootPositions;
    }

    public List<Vector3> GetRootPos()
    {
        return rootPositions;
    }

    public List<Quaternion> GetRootQ()
    {
        return rootQuaternions;
    }

    public List<Vector3> GetLeftFootVel()
    {
        return leftFootVelocity;
    }

    public List<Vector3> GetRightFootVel()
    {
        return rightFootVelocity;
    }

    public List<Vector3> GetRootVel()
    {
        return rootVelocity;
    }

    public void SetWritePath(string path)
    {
        CSVWritePath = path;
    }
}
