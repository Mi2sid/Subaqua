using UnityEngine;
using System.Collections;

public class MatScript : MonoBehaviour

{
   public OVRGrabbable oVRGrabbable;
    private void LateUpdate()
    {

        if (oVRGrabbable.isGrabbed == true)
        {
            transform.Rotate(Vector3.left * Time.deltaTime * 400);
        }
       
    }


}