using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Result : MonoBehaviour
{
    [SerializeField] private Text resultText;

    void Start()
    {
        var gaemResult = PlayerPrefs.GetInt("GaemResult");
        PlayerPrefs.DeleteKey("GaemResult");
        this.resultText.text = $"あなたのスコアは、\n{gaemResult}ポイント\nでした！！";
    }
}
