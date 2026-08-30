using UnityEngine;

public class Reparent : MonoBehaviour
{
    [SerializeField] Transform targetParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.transform.SetParent(targetParent);
    }

}
