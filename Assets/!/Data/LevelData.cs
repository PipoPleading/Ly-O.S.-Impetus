using UnityEngine;

public class LevelData : MonoBehaviour
{
    //[SerializeField]
/*    int fps;

    public void Save()
    {
        PlayerPrefs.SetInt("Fps", fps);
    }

    public void Load()
    {
        fps = PlayerPrefs.GetInt("Fps");
        Application.targetFrameRate = 60;
    }

    private void Awake()
    {


        Save();
        Load();
        Application.targetFrameRate = fps;
        Debug.Log(fps);
    }*/

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

/*    private void Update()
    {
        Debug.Log(fps);
    }*/
}
