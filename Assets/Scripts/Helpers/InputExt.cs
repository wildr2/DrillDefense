using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InputExt : MonoBehaviour
{
    private static InputExt _instance;
    private static InputExt I
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InputExt>();

                if (_instance == null) Debug.LogError("Missing InputExt");
                else
                {
                    DontDestroyOnLoad(_instance);
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }

    // [ControlScheme][ControlType] => InputExt.Entry
    private Dictionary<IConvertible, Dictionary<IConvertible, List<Entry>>> controls;
    private Dictionary<IConvertible, Dictionary<IConvertible, string>> control_names;
    private static Action on_lateupdate;

    // [player_id] => ControlScheme
    private IConvertible[] player_control_schemes;

    // Parameters
    public static bool DebugMode { get; set; }
    public static float RepeatDelay { get; set; }


    // PUBLIC MODIFIERS
    
    public static void ClearControls(IConvertible control_scheme, IConvertible control)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries != null) entries.Clear();
        I.SetControlName(control_scheme, control, "");
    }
    public static void AddAxis(IConvertible control_scheme, IConvertible control, KeyCode neg, KeyCode pos, string control_name = "")
    {
        AxisEntry entry = new AxisEntry(
            () => (Input.GetKey(neg) ? -1 : 0) + (Input.GetKey(pos) ? 1 : 0));
        I.GetOrAddEntryList(control_scheme, control).Add(entry);
        if (control_name != "") I.SetControlName(control_scheme, control, control_name);
    }
    public static void AddAxis(IConvertible control_scheme, IConvertible control, Func<bool> neg, Func<bool> pos, string control_name = "")
    {
        AxisEntry entry = new AxisEntry(
            () => (neg() ? -1 : 0) + (pos() ? 1 : 0));
        I.GetOrAddEntryList(control_scheme, control).Add(entry);
        if (control_name != "") I.SetControlName(control_scheme, control, control_name);
    }
    public static void AddAxis(IConvertible control_scheme, IConvertible control, string im_name, string control_name = "")
    {
        AxisEntry entry = new AxisEntry(() => Input.GetAxis(im_name));
        I.GetOrAddEntryList(control_scheme, control).Add(entry);
        if (control_name != "") I.SetControlName(control_scheme, control, control_name);
    }
    public static void AddKey(IConvertible control_scheme, IConvertible control, KeyCode keycode, string control_name="")
    {
        I.GetOrAddEntryList(control_scheme, control).Add(new KeyCodeEntry(keycode));
        if (control_name != "") I.SetControlName(control_scheme, control, control_name);
    }
    public static void AddKey(IConvertible control_scheme, IConvertible control, Func<bool> is_down, string control_name = "")
    {
        I.GetOrAddEntryList(control_scheme, control).Add(new PseudoKeyEntry(is_down));
        if (control_name != "") I.SetControlName(control_scheme, control, control_name);
    }

    public static void RegisterPlayers(int num_players, IConvertible default_scheme)
    {
        I.player_control_schemes = new IConvertible[num_players];
        for (int i = 0; i < num_players; ++i)
        {
            I.player_control_schemes[i] = default_scheme;
        }
    }
    public static void SetPlayerControlScheme(int id, IConvertible scheme)
    {
        if (!I.CheckPlayerIdValid(id)) return;
        I.player_control_schemes[id] = scheme;
    }


    // PUBLIC ACCESSORS

    public static bool GetKey(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return false;
        return GetKeyCS(I.player_control_schemes[id], control);
    }
    public static int GetKeyInt(int id, IConvertible control)
    {
        return GetKey(id, control) ? 1 : 0;
    }
    public static bool GetKeyDown(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return false;
        return GetKeyDownCS(I.player_control_schemes[id], control);
    }
    public static bool GetKeyUp(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return false;
        return GetKeyUpCS(I.player_control_schemes[id], control);
    }
    public static float GetAxis(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return 0;
        return GetAxisCS(I.player_control_schemes[id], control);
    }
    public static int GetAxisInt(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return 0;
        return GetAxisIntCS(I.player_control_schemes[id], control);
    }
    public static int GetAxisOnce(int id, IConvertible control, bool repeat = false)
    {
        if (!I.CheckPlayerIdValid(id)) return 0;
        return GetAxisOnceCS(I.player_control_schemes[id], control, repeat);
    }

    public static bool GetKeyCS(IConvertible control_scheme, IConvertible control)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries == null) return false;

        bool answer = false;
        foreach (Entry e in entries)
        {
            KeyEntry ke = e as KeyEntry;
            if (ke != null)
            {
                answer = answer || ke.GetKey();
            }
        }
        return answer;
    }
    public static bool GetKeyDownCS(IConvertible control_scheme, IConvertible control)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries == null) return false;

        bool answer = false;
        foreach (Entry e in entries)
        {
            KeyEntry ke = e as KeyEntry;
            if (ke != null)
            {
                answer = answer || ke.GetKeyDown();
            }
        }
        return answer;
    }
    public static bool GetKeyUpCS(IConvertible control_scheme, IConvertible control)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries == null) return false;

        bool answer = false;
        foreach (Entry e in entries)
        {
            KeyEntry ke = e as KeyEntry;
            if (ke != null)
            {
                answer = answer || ke.GetKeyUp();
            }
        }
        return answer;
    }
    public static float GetAxisCS(IConvertible control_scheme, IConvertible control)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries == null) return 0;

        float answer = 0;
        foreach (Entry e in entries)
        {
            AxisEntry ae = e as AxisEntry;
            if (ae != null)
            {
                answer += ae.GetAxis();
            }
        }
        return Mathf.Clamp(answer, -1, 1);
    }
    public static int GetAxisIntCS(IConvertible control_scheme, IConvertible control)
    {
        float f = GetAxisCS(control_scheme, control);
        return f < 0 ? -1 : f > 0 ? 1 : 0;
    }
    public static int GetAxisOnceCS(IConvertible control_scheme, IConvertible control, bool repeat = false)
    {
        List<Entry> entries = I.TryGetEntryList(control_scheme, control);
        if (entries == null) return 0;

        float f = 0;
        foreach (Entry e in entries)
        {
            AxisEntry ae = e as AxisEntry;
            if (ae != null)
            {
                f += ae.GetAxisOnce(repeat);
            }
        }
        return f < 0 ? -1 : f > 0 ? 1 : 0;
    }

    //public static string GetKeyName(KeyCode key)
    //{

    //    return key.ToString();
    //}
    //public static KeyCode GetJoystickKeyCode(int joystick_num, int btn_num)
    //{
    //    return KeyCode.None;
    //}
    public static string GetControlName(int id, IConvertible control)
    {
        if (!I.CheckPlayerIdValid(id)) return "";
        return GetControlNameCS(I.player_control_schemes[id], control);
    }
    public static string GetControlNameCS(IConvertible control_scheme, IConvertible control)
    {
        Dictionary<IConvertible, string> d;
        if (I.control_names.TryGetValue(control_scheme, out d))
        {
            string name;
            if (d.TryGetValue(control, out name))
            {
                return name;
            }
        }

        return "";
    }
    public static IConvertible GetPlayerScheme(int id)
    {
        I.CheckPlayerIdValid(id);
        return I.player_control_schemes[id];
    }


    // PRIVATE MODIFIERS

    private void Initialize()
    {
        controls = new Dictionary<IConvertible, Dictionary<IConvertible, List<Entry>>>();
        control_names = new Dictionary<IConvertible, Dictionary<IConvertible, string>>();
        DebugMode = false;
        RepeatDelay = 0.25f;
    }
    private void LateUpdate()
    {
        if (on_lateupdate != null) on_lateupdate();
    }

    private bool CheckPlayerIdValid(int id)
    {
        if (id < 0 || id > player_control_schemes.Length)
        {
            if (DebugMode) Debug.LogWarning("Invalid player id - insure to RegisterPlayers with the correct num_players");
            return false;
        }
        return true;
    }
    private List<Entry> TryGetEntryList(IConvertible control_scheme, IConvertible control)
    {
        Dictionary<IConvertible, List<Entry>> d;
        if (!controls.TryGetValue(control_scheme, out d))
        {
            if (DebugMode) Debug.LogWarning(string.Format("Control Scheme {0} not found", control_scheme));
            return null;
        }
        List<Entry> list;
        if (!d.TryGetValue(control, out list))
        {
            if (DebugMode) Debug.LogWarning(string.Format("Control {0} not found for scheme {1}", control, control_scheme));
            return null;
        }
        return list;
    }
    private List<Entry> GetOrAddEntryList(IConvertible control_scheme, IConvertible control)
    {
        Dictionary<IConvertible, List<Entry>> d;
        if (!controls.TryGetValue(control_scheme, out d))
        {
            d = new Dictionary<IConvertible, List<Entry>>();
            controls.Add(control_scheme, d);
        }
        List<Entry> list;
        if (!d.TryGetValue(control, out list))
        {
            list = new List<Entry>();
            d.Add(control, list);
        }
        return list;
    }
    private void SetControlName(IConvertible control_scheme, IConvertible control, string name)
    {
        Dictionary<IConvertible, string> d;
        if (!control_names.TryGetValue(control_scheme, out d))
        {
            d = new Dictionary<IConvertible, string>();
            control_names.Add(control_scheme, d);
        }
        if (d.ContainsKey(control)) d[control] = name;
        else d.Add(control, name);
    }

    // HELPER CLASSES

    public class Entry
    {
    }
    public class KeyEntry : Entry
    {
        public virtual bool GetKey()
        {
            return false;
        }
        public virtual bool GetKeyDown()
        {
            return false;
        }
        public virtual bool GetKeyUp()
        {
            return false;
        }
    }
    public class KeyCodeEntry : KeyEntry
    {
        private KeyCode keycode;

        public KeyCodeEntry(KeyCode keycode)
        {
            this.keycode = keycode;
        }

        public override bool GetKey()
        {
            return Input.GetKey(keycode);
        }
        public override bool GetKeyDown()
        {
            return Input.GetKeyDown(keycode);
        }
        public override bool GetKeyUp()
        {
            return Input.GetKeyUp(keycode);
        }
    }
    public class PseudoKeyEntry : KeyEntry
    {
        private Func<bool> is_down;
        private bool down_last_frame = false;

        public PseudoKeyEntry(Func<bool> is_down)
        {
            this.is_down = is_down;
            on_lateupdate += () =>
            {
                down_last_frame = is_down();
            };
        }

        public override bool GetKey()
        {
            return is_down();
        }
        public override bool GetKeyDown()
        {
            return is_down() && !down_last_frame;
        }
        public override bool GetKeyUp()
        {
            return !is_down() && down_last_frame;
        }
    }
    public class AxisEntry : Entry
    {
        private float repeat_timestamp = -RepeatDelay;
        private bool zero_last_frame = false;
        private Func<float> get_func;

        public AxisEntry(Func<float> get_func)
        {
            this.get_func = get_func;
            on_lateupdate += () =>
            {
                zero_last_frame = get_func() == 0;
            };
        }

        public float GetAxis()
        {
            return get_func();
        }
        public float GetAxisOnce(bool repeat=false)
        {
            float v = get_func();

            if (repeat)
            {
                if (v == 0)
                {
                    repeat_timestamp = -RepeatDelay;
                }
                else if (Time.time - repeat_timestamp >= RepeatDelay)
                {
                    repeat_timestamp = Time.time;
                }
                else v = 0;
            }
            else if (!zero_last_frame) v = 0;

            return v;
        }
    }
}



