using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightActivateScript : MonoBehaviour
{
    public Light lght;
    public OVRCameraRig oVRCameraRig;

    // Use this for initialization
    void Start()
    {
        lght = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {

        lght.transform.rotation = oVRCameraRig.transform.rotation;
        lght.transform.position = oVRCameraRig.rightHandAnchor.position;
        lght.transform.forward = oVRCameraRig.rightHandAnchor.forward;
        

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            lght.enabled = !lght.enabled;
           
        }

       

    }
}
