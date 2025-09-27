using TMPro;
using UnityEngine;

public class EnergyDisplayHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private TextMeshProUGUI tmp;
    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobalState.Instance)
        {
            tmp.text = GlobalState.Instance.power + "%";
        }
    }
}
