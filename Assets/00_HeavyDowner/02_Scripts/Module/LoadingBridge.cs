using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    Splash,
    Login
}

public static class LoadingBridge
{
    public static bool IsLoading { get; private set; }
    public static float Progress { get; private set; }

    public static async Awaitable LoadSceneAsync(SceneType sceneType)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        Progress = 0f;

        try
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                sceneType.ToString(),
                LoadSceneMode.Single);

            while (!loadOperation.isDone)
            {
                Progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                await Awaitable.NextFrameAsync();
            }

            Progress = 1f;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
