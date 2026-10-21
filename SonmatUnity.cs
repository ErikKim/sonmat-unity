// Sonmat for Unity games (BepInEx 5, Mono): every sound the game plays, by name, one line each into
// C:\Users\Public\dshaptics\events_unity.log for the engine (hapticsd.py, adapter "wwise"):
//   name:<clip name>          Unity's own audio: AudioSource.Play / PlayOneShot / PlayDelayed / PlayScheduled,
//                             AudioSource.PlayClipAtPoint
//   name:Stop_<clip name>     AudioSource.Stop / Pause of a source that has a clip (a held vibration ends with its
//                             sound: profile "stops"; Wwise names its stop events the same way)
//   name:<event name>         Wwise posted by name: AkSoundEngine.PostEvent(string, ...)
//   wid:<event id>:0          Wwise posted by id: AkSoundEngine.PostEvent(uint, ...) (the profile maps ids to names)
// Nothing here knows a game: Unity's and Wwise's own functions are found by name when the game starts, so one DLL
// serves Unity 2017 (.NET 3.5) to 2022 (.NET 4.x). Built for net35 with no Unity reference.
using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;

[BepInPlugin("sonmat.unity", "Sonmat", "0.1.0")]
public class SonmatUnity : BaseUnityPlugin
{
    static StreamWriter log;
    static readonly object gate = new object();
    static PropertyInfo clipProp, nameProp;

    void Awake()
    {
        var dir = Path.Combine(Environment.GetEnvironmentVariable("PUBLIC") ?? @"C:\Users\Public", "dshaptics");
        Directory.CreateDirectory(dir);
        log = new StreamWriter(new FileStream(Path.Combine(dir, "events_unity.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        log.AutoFlush = true;
        var h = new Harmony("sonmat.unity");
        int n = 0;
        var src = AccessTools.TypeByName("UnityEngine.AudioSource");
        if (src != null)
        {
            clipProp = src.GetProperty("clip");
            nameProp = AccessTools.TypeByName("UnityEngine.Object").GetProperty("name");
            foreach (var m in src.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (m.Name == "Play" || m.Name == "PlayDelayed" || m.Name == "PlayScheduled")
                    n += Patch(h, m, "OwnClip");
                else if (m.Name == "PlayOneShot" || m.Name == "PlayClipAtPoint")
                    n += Patch(h, m, "FirstArgClip");
                else if ((m.Name == "Stop" || m.Name == "Pause") && !m.IsStatic)
                    n += Patch(h, m, "OwnClipStop");
            }
        }
        var ak = AccessTools.TypeByName("AkSoundEngine");
        if (ak != null)
            foreach (var m in ak.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var ps = m.GetParameters();
                if (m.Name == "PostEvent" && ps.Length > 0 && (ps[0].ParameterType == typeof(string) || ps[0].ParameterType == typeof(uint)))
                    n += Patch(h, m, "WwiseEvent");
            }
        Logger.LogInfo("Sonmat: " + n + " functions watched" + (ak != null ? " (Wwise found)" : ""));
        Write("hello:unity:" + n);
    }

    static int Patch(Harmony h, MethodInfo m, string prefix)
    {
        try { h.Patch(m, new HarmonyMethod(typeof(SonmatUnity).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic))); return 1; }
        catch (Exception e) { Console.WriteLine("Sonmat: cannot watch " + m + ": " + e.Message); return 0; }
    }

    static void Write(string line)
    {
        try { lock (gate) log.WriteLine(line); } catch { }
    }

    static string NameOf(object clip)
    {
        if (clip == null) return null;
        try { return nameProp.GetValue(clip, null) as string; } catch { return null; }
    }

    static void OwnClip(object __instance)
    {
        var n = NameOf(clipProp.GetValue(__instance, null));
        if (!string.IsNullOrEmpty(n)) Write("name:" + n);
    }

    static void OwnClipStop(object __instance)
    {
        var n = NameOf(clipProp.GetValue(__instance, null));
        if (!string.IsNullOrEmpty(n)) Write("name:Stop_" + n);
    }

    static void FirstArgClip(object[] __args)
    {
        var n = __args.Length > 0 ? NameOf(__args[0]) : null;
        if (!string.IsNullOrEmpty(n)) Write("name:" + n);
    }

    static void WwiseEvent(object[] __args)
    {
        if (__args.Length == 0) return;
        if (__args[0] is string) Write("name:" + __args[0]);
        else if (__args[0] is uint) Write("wid:" + __args[0] + ":0");
    }
}
