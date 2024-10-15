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

    private void Awake()
    {
        Save();
        Load();
        Application.targetFrameRate = fps;
        Debug.Log(fps);
    }

    private void Update()
    {
        //Debug.Log(fps);
    }
}
