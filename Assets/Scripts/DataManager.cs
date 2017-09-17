using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<DataManager>();

                if (_instance == null) Debug.LogError("Missing DataManager");
                else
                {
                    DontDestroyOnLoad(_instance);
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }

    // Debug
    public bool debug_powers = false;
    public bool aiDoNothing = false;

    // Players
    public const int numPlayers = 2;
    public int numAI = 0;
    public int NumHumans { get { return numPlayers - numAI; } }
    public bool firstPlayerIsTop = true;

    public Color[] playerColors;


    // PUBLIC ACCESSORS



    // PUBLIC MODIFIERS



    // PRIVATE / PROTECTED MODIFIERS

    private void Awake()
    {
        // if this is the first instance, make this the singleton
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(_instance);
            Initialize();
        }
        else
        {
            // destroy other instances that are not the already existing singleton
            if (this != _instance)
                Destroy(this.gameObject);
        }
    }
    private void Initialize()
    {   
    }

}
