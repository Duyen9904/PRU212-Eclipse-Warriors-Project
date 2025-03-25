using UnityEngine;
using UnityEngine.SceneManagement;
public class StartScene : MonoBehaviour
{
    public void LoadGlacierScene()
    {
        SceneManager.LoadScene("GlacierBiome"); // Thay tên scene nếu cần
    }
}
