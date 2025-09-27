using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalState : MonoBehaviour
{
    public int power = 100;
    public static GlobalState Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(power <= 0) {
            RestartLevel();
        }
    }

    private void RestartLevel()
    {
        Debug.Log("Restarting Level");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        power = 100;
    }
}
