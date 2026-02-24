using UnityEngine;

public class ForageItem : MonoBehaviour
{
    public bool isTargeted;
    public GameObject itemModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isTargeted = false;
    }

    public void Reset(Vector3 loc)
    {
        isTargeted = false;

        // Set a new spawn
        this.gameObject.transform.position = loc;
    }
}
