using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static DataHolder;

public class EnableByLevel : MonoBehaviour
{
    public int levelToEnable;
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        _button.interactable = levelToEnable <= CurrentLevel;
    }}
