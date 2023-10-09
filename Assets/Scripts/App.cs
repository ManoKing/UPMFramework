using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class App : MonoBehaviour
{
   
    void Start()
    {
        Debug.Log("热更完成，进入游戏");
        SceneManager.LoadScene("Start");
    }
    
}
