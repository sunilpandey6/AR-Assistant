using System.IO;
using UnityEngine;
public static class Keys
{
    public static string LoadGemini () =>
        File.ReadAllText(Path.Combine(Application.dataPath, "Secure/gemini.txt")).Trim();
    public static string LoadEleven() =>
        File.ReadAllText(Path.Combine(Application.dataPath, "Secure/elevenlabs.txt")).Trim();
}
