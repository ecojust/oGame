using UnityEngine;

public static class BeadGameSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetup()
    {
        if (Object.FindFirstObjectByType<BeadGameManager>() == null)
        {
            var go = new GameObject("BeadGameManager");
            go.AddComponent<BeadGameManager>();
        }
    }
}
