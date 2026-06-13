using UnityEngine;

public class FixedGameViewController : MonoBehaviour
{
    private const int TargetWidth = 960;
    private const int TargetHeight = 540;
    private const float TargetAspect = TargetWidth / (float)TargetHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindFirstObjectByType<FixedGameViewController>() != null)
        {
            return;
        }

        GameObject controller = new GameObject("Fixed Game View Controller");
        DontDestroyOnLoad(controller);
        controller.AddComponent<FixedGameViewController>();
    }

    private void Awake()
    {
        ApplyResolution();
        ApplyCameraViewport();
    }

    private void LateUpdate()
    {
        ApplyCameraViewport();
    }

    private static void ApplyResolution()
    {
        if (Screen.width == TargetWidth && Screen.height == TargetHeight && !Screen.fullScreen)
        {
            return;
        }

        Screen.SetResolution(TargetWidth, TargetHeight, false);
    }

    private static void ApplyCameraViewport()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float windowAspect = Screen.width / Mathf.Max(1f, Screen.height);
        if (windowAspect > TargetAspect)
        {
            float width = TargetAspect / windowAspect;
            camera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else
        {
            float height = windowAspect / TargetAspect;
            camera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }
}
