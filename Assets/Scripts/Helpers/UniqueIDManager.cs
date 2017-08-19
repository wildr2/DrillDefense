using UnityEngine;
using System.Collections.Generic;

public static class UniqueIDManager
{
    private static List<int> next_id;

    static UniqueIDManager()
    {
        next_id = new List<int>();
        NextGroup();
    }

    public static int NextGroup()
    {
        next_id.Add(0);
        return next_id.Count-1;
    }
    public static int NextID()
    {
        return next_id[0]++;
    }
    public static int NextID(UIDGroup group)
    {
        return next_id[group.Value]++;
    }
}

public class UID
{
    public int Value { get; private set; }
    public UID()
    {
        Value = UniqueIDManager.NextID();
    }
    public UID(UIDGroup group)
    {
        Value = UniqueIDManager.NextID(group);
    }
    public override string ToString()
    {
        return Value.ToString();
    }
}
public class UIDGroup
{
    public int Value { get; private set; }
    public UIDGroup()
    {
        Value = UniqueIDManager.NextGroup();
    }
    public override string ToString()
    {
        return Value.ToString();
    }
}
