using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
