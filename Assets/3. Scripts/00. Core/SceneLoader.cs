using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static string NextScene;

    public static void StartLoad(string nextScene)
    {
        NextScene = nextScene;

        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }
}