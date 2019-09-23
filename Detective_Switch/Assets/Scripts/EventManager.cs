﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EventManager : MonoBehaviour
{

    public ThisEventSystem[] events;

    void Start()
    {
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i].eventName != "" || events[i].eventToFire != null)
            {
                events[i].attachedManager = this.gameObject;
                StartClassCoroutine(i, (int)events[i].function);
            }
            else
            {
                Debug.Log("EventManager Notice: event number " + i + " is not set up correctly!");
            }

        }
        
    }

    void Update()
    {
        // Not used atm.
    }

    public void Fire(string eventName)
    {
        bool isFound = false;

        for (int i = 0; i < events.Length; i++)
        {
            if (eventName == events[i].eventName)
            {
                isFound = true;
                StartCoroutine(events[i].ExternalFire());
            }
        }

        if (isFound == false)
        {
            Debug.LogError("EventManager Error: " + eventName + " not found in events! Please note that event names are case sensitive.");
        }
    }

    void StartClassCoroutine(int eventNum, int funcNum)
    {
        switch (funcNum)
        {
            case 0:
                // ExternalFire selected, no need to run any functions
                break;
            case 1:
                events[eventNum].OnCollision();
                break;
            case 2:
                events[eventNum].OnCollisionWithTag();
                break;
            case 3:
                StartCoroutine(events[eventNum].OnObjectDestroy());
                break;
            case 4:
                StartCoroutine(events[eventNum].TimedEvent());
                break;
            case 5:
                StartCoroutine(events[eventNum].OnObjectMoving());
                break;
            default:
                break;
        }
    }

}

[System.Serializable]
public class ThisEventSystem
{
    [HideInInspector]
    public GameObject attachedManager;

    private bool hasFired = false;
    [HideInInspector]
    public bool activeInInspector = false;

    [HideInInspector]
    public string eventName = "";

    public enum myFuncEnum
    {
        ExternalFire,
        OnCollision,
        OnCollisionWithTag,
        OnObjectDestroy,
        TimedEvent,
        OnObjectMoving
    };

    [HideInInspector]
    public UnityEvent eventToFire;

    [HideInInspector]
    public myFuncEnum function;

    [HideInInspector]
    public float delayForFire = 0f;

    [HideInInspector]
    public GameObject thisGameObject;
    [HideInInspector]
    public string collisionTag = "";
    [HideInInspector]
    public bool isTrigger = true;
    [HideInInspector]
    public float fireCooldown = 0f;
    // public UnityEvent boolEvent;

    public void OnCollisionWithTag()
    {
        if (thisGameObject.GetComponent<ColliderChecker>() == null)
        {
            thisGameObject.AddComponent<ColliderChecker>();
        }

        if (thisGameObject.GetComponent<ColliderChecker>() != null)
        {

            if (hasFired)
            {
                Debug.Log(eventName + " event already fired");
            }
            else
            {
                thisGameObject.GetComponent<ColliderChecker>().SetUpColliderChecker(eventName, fireCooldown, isTrigger, attachedManager, collisionTag);
            }

        }
        else
        {
            Debug.Log(eventName + " error: ColliderChecker script missing!");
        }
    }

    public void OnCollision()
    {
        if (thisGameObject.GetComponent<ColliderChecker>() == null)
        {
            thisGameObject.AddComponent<ColliderChecker>();
        }

        if (thisGameObject.GetComponent<ColliderChecker>() != null)
        {

            if (hasFired)
            {
                Debug.Log(eventName + " event already fired");
            }
            else
            {
                thisGameObject.GetComponent<ColliderChecker>().SetUpColliderChecker(eventName, fireCooldown, isTrigger, attachedManager);
            }

        }
        else
        {
            Debug.Log(eventName + " error: ColliderChecker script missing!");
        }
    }

    public IEnumerator OnObjectDestroy()
    {
        if (hasFired)
        {
            Debug.Log(eventName + " event already fired");
        }
        else
        {
            bool loop = true;
            while (loop)
            {
                yield return new WaitForSeconds(0.1f);

                if (thisGameObject == null)
                {
                    yield return new WaitForSeconds(delayForFire);
                    loop = false;
                    eventToFire.Invoke();
                    Debug.Log(eventName + " event fired!");
                }

            }
        }
    }

    public IEnumerator ExternalFire()
    {
        yield return new WaitForSeconds(delayForFire);

        if (hasFired)
        {
            Debug.Log(eventName + " event already fired");
        } else
        {
            eventToFire.Invoke();
            Debug.Log(eventName + " event fired!");
            hasFired = true;
        }

        if (fireCooldown > 0f)
        {
            yield return new WaitForSeconds(fireCooldown);
            hasFired = false;
        }

    }

    public IEnumerator TimedEvent()
    {

        bool loop = true;
        while (loop)
        {
            yield return new WaitForSeconds(delayForFire);

            eventToFire.Invoke();
            Debug.Log(eventName + " event fired!");

            if (fireCooldown == 0)
            {
                loop = false;
            }

            yield return new WaitForSeconds(fireCooldown);

        }
    }

    public IEnumerator OnObjectMoving()
    {
        Vector3 tempPos = thisGameObject.transform.position;
        bool loop = true;
        while (loop)
        {
            if (thisGameObject.transform.position != tempPos)
            {
                yield return new WaitForSeconds(delayForFire);

                eventToFire.Invoke();
                Debug.Log(eventName + " event fired!");

                if (fireCooldown == 0)
                {
                    loop = false;
                }

                yield return new WaitForSeconds(fireCooldown);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EventManager))]
public class EventManager_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("IMPORTANT! - The ColliderChecker.cs script must also be located in the script folder for this event system to work", MessageType.None);
        DrawDefaultInspector(); // for other non-HideInInspector fields

        var script = target as EventManager;

        for (int i = 0; i < script.events.Length; i++)
        {
            script.events[i].activeInInspector = EditorGUILayout.Foldout(script.events[i].activeInInspector, script.events[i].eventName);

            EditorGUI.indentLevel = EditorGUI.indentLevel + 1;

            if (script.events[i].activeInInspector)
            {
                script.events[i].eventName = EditorGUILayout.TextField("Event Name:", script.events[i].eventName);

                SerializedProperty functionProp = serializedObject.FindProperty("events.Array.data[" + i + "].function");
                EditorGUILayout.PropertyField(functionProp);

                script.events[i].delayForFire = EditorGUILayout.FloatField("Fire Delay", script.events[i].delayForFire);


                if ((int)script.events[i].function == 0)
                {
                    // External fire //
                    EditorGUILayout.LabelField("Fire cooldown, if 0 then can only be fired once:");
                    script.events[i].fireCooldown = EditorGUILayout.FloatField("Fire Cooldown", script.events[i].fireCooldown);
                    EditorGUILayout.HelpBox("This is an event you need to call from a script.\n" +
                        "You simply do that by calling the ExternalFire function in the EventManager script and give the specific event name as argument. " +
                        "If more events have the same name, they will also be fired. NOTICE, the names are case sensitive!", MessageType.Info);
                }
                if ((int)script.events[i].function == 1) // if bool is true, show other fields
                {
                    // Collision // 
                    EditorGUILayout.LabelField("Fire cooldown, if 0 then can only be fired once:");
                    script.events[i].fireCooldown = EditorGUILayout.FloatField("Fire Cooldown", script.events[i].fireCooldown);
                    script.events[i].isTrigger = EditorGUILayout.Toggle("Is Trigger?", script.events[i].isTrigger);
                    script.events[i].thisGameObject = EditorGUILayout.ObjectField("Collider Object", script.events[i].thisGameObject, typeof(GameObject), true) as GameObject;
                    EditorGUILayout.HelpBox("This fires an event when the selected object collides with anything. Please use 'is trigger' depending on the nature of the collision. Not fully tested yet.", MessageType.Warning);
                }
                if ((int)script.events[i].function == 2)
                {
                    // Collision with tag //
                    EditorGUILayout.LabelField("Fire cooldown, if 0 then can only be fired once:");
                    script.events[i].fireCooldown = EditorGUILayout.FloatField("Fire Cooldown", script.events[i].fireCooldown);
                    script.events[i].collisionTag = EditorGUILayout.TextField("Collision Tag Name", script.events[i].collisionTag);
                    script.events[i].isTrigger = EditorGUILayout.Toggle("Is Trigger?", script.events[i].isTrigger);
                    script.events[i].thisGameObject = EditorGUILayout.ObjectField("Collider Object", script.events[i].thisGameObject, typeof(GameObject), true) as GameObject;
                    EditorGUILayout.HelpBox("This fires an event when the selected object collides with a specific tag. NOTICE, the names are case sensitive!. Please use 'is trigger' depending on the nature of the collision. Not fully tested yet.", MessageType.Warning);
                }
                if ((int)script.events[i].function == 3)
                {
                    // Check if object destroyed //
                    script.events[i].thisGameObject = EditorGUILayout.ObjectField("GameObject", script.events[i].thisGameObject, typeof(GameObject), true) as GameObject;
                    EditorGUILayout.HelpBox("Attach the GameObject you want to listen to. When this specific object is destroyed, the event will fire.", MessageType.Info);
                }

                if ((int)script.events[i].function == 4)
                {
                    // Timed event //
                    EditorGUILayout.LabelField("Fire cooldown, if 0 then can only be fired once:");
                    script.events[i].fireCooldown = EditorGUILayout.FloatField("Fire Cooldown", script.events[i].fireCooldown);
                    EditorGUILayout.HelpBox("Specify the time interval for when the event should be fired", MessageType.Info);
                }
                if ((int)script.events[i].function == 5)
                {
                    // Object moving // 
                    script.events[i].fireCooldown = EditorGUILayout.FloatField("Fire Cooldown", script.events[i].fireCooldown);
                    script.events[i].thisGameObject = EditorGUILayout.ObjectField("GameObject", script.events[i].thisGameObject, typeof(GameObject), true) as GameObject;
                    EditorGUILayout.HelpBox("Fires an event when the selected gameobject is moving. Not fully tested yet", MessageType.Warning);
                }

                SerializedProperty fireProp = serializedObject.FindProperty("events.Array.data[" + i + "].eventToFire");
                EditorGUILayout.PropertyField(fireProp);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
        }
    }
}
#endif