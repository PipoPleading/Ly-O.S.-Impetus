using UnityEngine;

public class LevelData : MonoBehaviour
{
    [SerializeField] int fps;

    public void Save()
    {
        PlayerPrefs.SetInt("Fps", fps);
    }

    public void Load()
    {
        fps = PlayerPrefs.GetInt("Fps");
        Application.targetFrameRate = fps;
    }

    private void Start()
    {
        Application.targetFrameRate = fps;
        Debug.Log(fps);
    }

}
