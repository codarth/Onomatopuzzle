using UnityEngine;

public class PersistentAudio : MonoBehaviour
{
    public static PersistentAudio Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
            Instance = this;
        }
    }
}
