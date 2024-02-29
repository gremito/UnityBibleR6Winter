using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ProjectSceneManager : NetworkBehaviour
{
#if UNITY_EDITOR
    /// INFO: You can remove the #if UNITY_EDITOR code segment and make SceneName public,
    /// but this code assures if the scene name changes you won't have to remember to
    /// manually update it.
    public UnityEditor.SceneAsset SceneAsset;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }
    }
#endif

    [SerializeField]
    private string m_SceneName;
    [SerializeField]
    private Text resultTitle;

    void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !string.IsNullOrEmpty(m_SceneName))
        {
            var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load {m_SceneName} " +
                    $"with a {nameof(SceneEventProgressStatus)}: {status}");
            }
        }
    }

    public void GoScene()
    {
        // Debug.Log($"Scene Name: {SceneManager.GetActiveScene().name}");
        m_SceneName = "Scenes/Debug/Game";
        if (SceneManager.GetActiveScene().name == "Game")
        {
            m_SceneName = "Result";
            this.resultTitle.gameObject.SetActive(true);
        }
        else
        {
            this.resultTitle.gameObject.SetActive(false);
        }
        OnNetworkSpawn();
    }
}
