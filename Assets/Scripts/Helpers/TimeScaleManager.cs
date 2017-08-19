using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// A singleton that manages multiple assignments to Time.timescale
/// </summary>
public class TimeScaleManager : MonoBehaviour
{
    private static TimeScaleManager _instance;
    public static TimeScaleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<TimeScaleManager>();

                if (_instance == null) Debug.LogError("Missing TimeScaleManager");
                else
                {
                    DontDestroyOnLoad(_instance);
                }
            }
            return _instance;
        }
    }

    public float base_timescale = 1;
    public bool maintain_fixedtimestep_ratio = true;
    public float fixed_timestep = 0.016f;

    private FloatProduct product;


    // PUBLIC MODIFIERS

    public static void SetFactor(float factor, UID id)
    {
        TimeScaleManager I = Instance;
        I.product.SetFactor(factor, id);
        I.UpdateTimeScale();
    }
    public static float GetFactor(UID id)
    {
        return Instance.product.GetFactor(id);
    }


    // PRIVATE MODIFIERS

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
            {
                // save new inspector parameters
                //_instance.base_timescale = this.base_timescale;
                //_instance.maintain_fixedtimestep_ratio = this.maintain_fixedtimestep_ratio;
                //_instance.fixed_timestep = this.fixed_timestep;

                Destroy(this.gameObject);
            }

        }
    }
    private void Initialize()
    {
        SceneManager.sceneLoaded += OnLoadScene;
    }
    private void OnLoadScene(Scene scene, LoadSceneMode mode)
    {
        if (this != _instance) return;

        product = new FloatProduct(base_timescale);
        Instance.UpdateTimeScale();
    }
    private void Update()
    {
        if (Time.timeScale != product.Value)
        {
            Debug.LogWarning("Time.timeScale was set by something other than TimeScaleManager");

            // insure that time scale is controlled by this manager
            Time.timeScale = product.Value;
        }
    }
    private void UpdateTimeScale()
    {
        Time.timeScale = product.Value;
        if (maintain_fixedtimestep_ratio)
        {
            Time.fixedDeltaTime = fixed_timestep * Time.timeScale;
        }
    }
}