using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableCollider : MonoBehaviour
{
    void OnBecameVisible()
    {
        enabled = true;
        GetComponent<BoxCollider>().enabled = true;
    }

    void OnBecameInvisible()
    {

        enabled = false;
        GetComponent<BoxCollider>().enabled = false;
    }
}
