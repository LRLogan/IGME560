using UnityEngine;

public class ForageItem : MonoBehaviour
{
    public bool isTargeted;
    public GameObject itemModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Reset();
    }

    public void Reset()
    {
        isTargeted = false;
    }
}
