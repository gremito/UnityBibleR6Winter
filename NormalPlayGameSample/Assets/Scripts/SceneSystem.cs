using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSystem : MonoBehaviour
{
    public void GoStartScene()
    {
        SceneManager.LoadScene("Start");
    }

    public void GoGameScene()
    {
        SceneManager.LoadScene("Game");
    }

    public void GoResultScene()
    {
        SceneManager.LoadScene("Result");
    }
}
