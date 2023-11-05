using UnityEngine;

public class Debug_Projector : MonoBehaviour
{
    public GameObject[] caustics;

    void FixedUpdate()
    {
        caustics = GameObject.FindGameObjectsWithTag("Projector");

        int layerMask = 1 << 7;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        {
            if (hit.transform.CompareTag("Water_Limits"))
            {
                foreach (GameObject test in caustics)
                    test.GetComponent<Projector>().enabled = false;
            }
            else
            {
                foreach (GameObject test in caustics)
                    test.GetComponent<Projector>().enabled = true;
            }
        }
    }
}