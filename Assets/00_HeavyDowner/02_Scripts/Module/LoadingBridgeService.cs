using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeavyDowner.Module
{
    public enum SceneType
    {
        Login,
        Loading,
        Main,
        Ingame
    }

    public enum LoadingMode
    {
        None,
        Overlay
    }

    public static class LoadingBridgeService
    {
        public static async Awaitable LoadAsync(SceneType scene, LoadingMode mode)
        {
            if (mode == LoadingMode.Overlay)
            {
                await LoadOverlayAsync(scene);
                return;
            }

            await LoadDirectAsync(scene);
        }

        private static async Awaitable LoadDirectAsync(SceneType scene)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single);

            await WaitAsync(op);
        }

        private static async Awaitable LoadOverlayAsync(SceneType scene)
        {
            Scene prevScene = SceneManager.GetActiveScene();

            AsyncOperation op = SceneManager.LoadSceneAsync(SceneType.Loading.ToString(), LoadSceneMode.Additive);

            await WaitAsync(op);
            float shownTime = 0f;
            await Awaitable.NextFrameAsync();
            shownTime += Time.deltaTime;

            op = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Additive);

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                await Awaitable.NextFrameAsync();
                shownTime += Time.deltaTime;
            }

            op.allowSceneActivation = true;
            shownTime += await WaitAsync(op);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene.ToString()));

            op = SceneManager.UnloadSceneAsync(prevScene);
            shownTime += await WaitAsync(op);

            await WaitMinAsync(shownTime, 1f);

            op = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(SceneType.Loading.ToString()));
            await WaitAsync(op);
        }

        private static async Awaitable<float> WaitAsync(AsyncOperation op)
        {
            float elapsed = 0f;

            while (!op.isDone)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.deltaTime;
            }

            return elapsed;
        }

        private static async Awaitable WaitMinAsync(float elapsed, float duration)
        {
            while (elapsed < duration)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.deltaTime;
            }
        }
    }
}
