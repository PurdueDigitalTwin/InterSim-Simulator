using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Tutorial Prompt UI")]
    public GameObject tutorialPromptPanel;
    
    [Header("Loading UI")]
    public GameObject loadingPanel;
    public UnityEngine.UI.Image loadingImage; // Optional: for loading animation
    
    private string selectedSceneName;

    public void LoadDLT()
    {
        ShowTutorialPrompt("DLT Traffic");
    }

    public void LoadRCI()
    {
        ShowTutorialPrompt("RCI Traffic");
    }

    public void LoadRBI()
    {
        ShowTutorialPrompt("RBI Traffic");
    }

    public void LoadDDI()
    {
        ShowTutorialPrompt("DDI Traffic");
    }

    public void LoadRAB()
    {
        ShowTutorialPrompt("RAB Traffic");
    }

    public void ShowTutorialPrompt(string sceneName)
    {
        selectedSceneName = sceneName;
        if (tutorialPromptPanel != null)
        {
            tutorialPromptPanel.SetActive(true);
        }
    }

    public void OnTutorialYes()
    {
        if (tutorialPromptPanel != null)
        {
            tutorialPromptPanel.SetActive(false);
        }
        // Store the target scene for after tutorial completion
        TutorialController.targetSceneAfterTutorial = selectedSceneName;
        StartCoroutine(LoadSceneWithLoading("Tutorial"));
    }

    public void OnTutorialNo()
    {
        if (tutorialPromptPanel != null)
        {
            tutorialPromptPanel.SetActive(false);
        }
        StartCoroutine(LoadSceneWithLoading(selectedSceneName));
    }

    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        // Show loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Optional: Show loading animation while scene loads
        if (loadingImage != null)
        {
            StartCoroutine(AnimateLoading());
        }

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator AnimateLoading()
    {
        if (loadingImage == null) yield break;

        float rotationSpeed = 90f; // degrees per second
        while (loadingPanel != null && loadingPanel.activeInHierarchy)
        {
            loadingImage.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void ExitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
