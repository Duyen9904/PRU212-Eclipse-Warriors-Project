using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager instance;

    private int enemyCount;
    [HideInInspector]
    public int nextSceneIndex = 0; // Chọn scene bằng index thay vì nhập tay
    public GameObject levelExit;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CountEnemies();
    }

    void CountEnemies()
    {
        enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        Debug.Log("Số lượng quái: " + enemyCount);
    }

    public void EnemyKilled()
    {
        enemyCount--;

        if (enemyCount <= 0)
        {
            levelExit.SetActive(true);
        }
    }

    public void LoadNextLevel()
    {
        if (nextSceneIndex >= 0 && nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            string sceneName = GetSceneNameByIndex(nextSceneIndex);
            Debug.Log("Chuyển sang màn: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Index Scene không hợp lệ!");
        }
    }

    private string GetSceneNameByIndex(int index)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(index);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}
