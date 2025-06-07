using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }
    
    public void LoadEditorScene()
    {
        SceneManager.LoadScene("MapEditorScene");
    }

    public void LoadBeatmapSelectScene()
    {
        SceneManager.LoadScene("BeatmapSelectScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}