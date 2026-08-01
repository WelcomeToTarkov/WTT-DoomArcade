using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ManagedDoom;

public static class DoomStaticReset
{
    private static bool _captured;
    private static DoomSnapshot _snapshot;

    public static void RestoreVanilla()
    {
        if (!_captured)
        {
            _snapshot = CaptureSnapshot();
            _captured = true;
            return;
        }

        RestoreSnapshot(_snapshot);
    }

    private static DoomSnapshot CaptureSnapshot()
    {
        return new DoomSnapshot
        {
            MobjInfos = DeepCloneArray(DoomInfo.MobjInfos),
            States = DeepCloneArray(DoomInfo.States),
            WeaponInfos = DeepCloneArray(DoomInfo.WeaponInfos),
            AmmoMax = (int[])DoomInfo.AmmoInfos.Max.Clone(),
            AmmoClip = (int[])DoomInfo.AmmoInfos.Clip.Clone(),
            DehackedConsts = CaptureStaticFields(typeof(DoomInfo.DeHackEdConst)),
            Doom1Pars = DoomInfo.ParTimes.Doom1.Select(x => x.ToArray()).ToArray(),
            Doom2Pars = DoomInfo.ParTimes.Doom2.ToArray(),
            Strings = CaptureDoomStrings()
        };
    }

    private static void RestoreSnapshot(DoomSnapshot s)
    {
        RestoreArrayContents(DoomInfo.MobjInfos, s.MobjInfos);
        RestoreArrayContents(DoomInfo.States, s.States);
        RestoreArrayContents(DoomInfo.WeaponInfos, s.WeaponInfos);

        Array.Copy(s.AmmoMax, DoomInfo.AmmoInfos.Max, s.AmmoMax.Length);
        Array.Copy(s.AmmoClip, DoomInfo.AmmoInfos.Clip, s.AmmoClip.Length);

        RestoreStaticFields(typeof(DoomInfo.DeHackEdConst), s.DehackedConsts);

        for (int i = 0; i < s.Doom2Pars.Length; i++)
            DoomInfo.ParTimes.Doom2[i] = s.Doom2Pars[i];

        for (int i = 0; i < s.Doom1Pars.Length; i++)
            for (int j = 0; j < s.Doom1Pars[i].Length; j++)
                DoomInfo.ParTimes.Doom1[i][j] = s.Doom1Pars[i][j];

        RestoreDoomStrings(s.Strings);
    }

    private static object[] DeepCloneArray(Array source)
    {
        var result = new object[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = DeepCloneObject(source.GetValue(i));
        }
        return result;
    }

    private static void RestoreArrayContents(Array liveArray, object[] savedArray)
    {
        for (int i = 0; i < liveArray.Length; i++)
        {
            CopyAllFields(savedArray[i], liveArray.GetValue(i));
        }
    }

    private static object DeepCloneObject(object source)
    {
        if (source == null)
            return null;

        var type = source.GetType();
        var clone = RuntimeHelpers.GetUninitializedObject(type);
        CopyAllFields(source, clone);
        return clone;
    }

    private static void CopyAllFields(object source, object target)
    {
        if (source == null || target == null)
            return;

        var type = source.GetType();
        while (type != null)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                var value = field.GetValue(source);

                if (value == null ||
                    field.FieldType.IsPrimitive ||
                    field.FieldType.IsEnum ||
                    field.FieldType == typeof(string) ||
                    field.FieldType.IsValueType)
                {
                    field.SetValue(target, value);
                }
                else if (field.FieldType.IsArray)
                {
                    var arr = (Array)value;
                    field.SetValue(target, arr == null ? null : arr.Clone());
                }
                else
                {
                    field.SetValue(target, value);
                }
            }

            type = type.BaseType;
        }
    }

    private static Dictionary<string, object> CaptureStaticFields(Type type)
    {
        var result = new Dictionary<string, object>();
        var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            result[field.Name] = field.GetValue(null);
        }

        return result;
    }

    private static void RestoreStaticFields(Type type, Dictionary<string, object> values)
    {
        var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (values.TryGetValue(field.Name, out var value))
                field.SetValue(null, value);
        }
    }

    private static Dictionary<string, string> CaptureDoomStrings()
    {
        var result = new Dictionary<string, string>();
        var fields = typeof(DoomInfo.Strings).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(DoomString))
                continue;

            var obj = field.GetValue(null);
            if (obj == null)
                continue;

            var nameProp = obj.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var valueProp = obj.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (nameProp == null || valueProp == null)
                continue;

            var name = nameProp.GetValue(obj) as string;
            var value = valueProp.GetValue(obj) as string;

            if (!string.IsNullOrEmpty(name))
                result[name] = value;
        }

        return result;
    }

    private static void RestoreDoomStrings(Dictionary<string, string> strings)
    {
        var fields = typeof(DoomInfo.Strings).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(DoomString))
                continue;

            var obj = field.GetValue(null);
            if (obj == null)
                continue;

            var nameProp = obj.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var replaceMethod = obj.GetType().GetMethod("Replace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (nameProp == null || replaceMethod == null)
                continue;

            var name = nameProp.GetValue(obj) as string;
            if (name != null && strings.TryGetValue(name, out var value))
                replaceMethod.Invoke(obj, new object[] { value });
        }
    }

    private sealed class DoomSnapshot
    {
        public object[] MobjInfos;
        public object[] States;
        public object[] WeaponInfos;
        public int[] AmmoMax;
        public int[] AmmoClip;
        public Dictionary<string, object> DehackedConsts;
        public int[][] Doom1Pars;
        public int[] Doom2Pars;
        public Dictionary<string, string> Strings;
    }
}