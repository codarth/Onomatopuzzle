using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class UIReset : MonoBehaviour
    {

        private Button button;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(RestartGame);
        }

        public void RestartGame()
        {
            Debug.Log("Restarting Level");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

    }
}