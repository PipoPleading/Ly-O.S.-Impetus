using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject settingsMenuCanvas;
    [SerializeField] private GameObject keyboardMenuCanvas;

    [SerializeField] private GameObject menuFirst;
    [SerializeField] private GameObject settingsFirst;
    [SerializeField] private GameObject keyboardFirst;

    [SerializeField] private KinematicCharacterController player;
    //[SerializeField] private GameObject mainMenuFirst;

    private bool isPaused;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMenuCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(UserInput.instance.pausePressed)
        {
            if (!isPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
        
    }

    #region Pause/Unpause functions

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OpenMainMenu();
    }

    public void Unpause()
    {
        isPaused = false;
        Time.timeScale = player.playerTimeScale;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CloseAllMenus();
    }

    public void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(false);
        keyboardMenuCanvas.SetActive(false);


        EventSystem.current.SetSelectedGameObject(menuFirst);
    }

    private void OpenSettingsMenu()
    {
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(true);
        keyboardMenuCanvas.SetActive(false);
        //keep main menu in bg, not the focus tho itll look cool
        EventSystem.current.SetSelectedGameObject(settingsFirst);

    }
    private void OpenKeyboardMenu()
    {
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(true);
        keyboardMenuCanvas.SetActive(true);
        //keep main menu in bg, not the focus tho itll look cool
        EventSystem.current.SetSelectedGameObject(keyboardFirst);

    }


    public void CloseAllMenus()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMenuCanvas.SetActive(false);
    }

    #endregion Pause/Unpause functions

    #region Main Menu Button Actions

    public void OnSettingsPress()
    {
        OpenSettingsMenu();
    }

    public void OnResumePressed()
    {
        Unpause();
    }

    #endregion

    #region Settings Menu Button Actions

    public void OnSettingsBackPress()
    {
        OpenMainMenu();
    }

    public void OnKeyboardPressed()
    {
        OpenKeyboardMenu();
    }

    #endregion

    #region Keyboard Config

    public void OnKeyboardBackPressed()
    {
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(true);
        keyboardMenuCanvas.SetActive(false);
    }

    #endregion Keyboard Config



}
