using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scan : MonoBehaviour
{
    [SerializeField] Material scanMaterial;
    Material[][] materials;
    [SerializeField] LayerMask layerMask;
    GameObject hitObject;
    [SerializeField] float maxDistance;
    [SerializeField] Text ScanPercent;
    [SerializeField] Text ScanInfos;
    [SerializeField] Image ScanImage;
    [SerializeField] GameObject ScanCone;
    Text Path;
    float scanned;
    int plantScanned = 0;

    public bool scanning = false;

    Dictionary<string, float> plantsScanned = new Dictionary<string, float>();

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Input.GetMouseButton(0) || scanning)
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDistance, layerMask))
            {
                if (hitObject != null && hitObject != hit.collider.gameObject)
                {
                    ScanPercent.enabled = false;
                    ScanImage.enabled = false;

                    if (tag == "VR")
                    {
                        ScanCone.SetActive(false);
                    }

                    RemoveMat();

                    hitObject = null;
                }

                hitObject = hit.collider.gameObject;
                int charLocation = hitObject.name.IndexOf(" ", StringComparison.Ordinal);
                String name;
                if (charLocation > 0)
                {
                    name = hitObject.name.Substring(0, charLocation);
                }
                else name = hitObject.name;

                if (plantsScanned.ContainsKey(name))
                {
                    plantsScanned.TryGetValue(name, out scanned);

                }
                else
                {
                    scanned = 0;
                    plantsScanned.Add(name, scanned);
                }

                ScanPercent.enabled = true;
                ScanPercent.text = ((int)scanned) + " %";

                ScanImage.enabled = true;
                ScanImage.sprite = Resources.Load("ObjectsImages/" + name, typeof(Sprite)) as Sprite;

                if (tag == "VR")
                {
                    ScanCone.SetActive(true);
                }

                if (scanned < 100)
                {
                    scanned += 0.25f;
                    plantsScanned[name] = scanned;

                    if (scanned == 100) plantScanned++;

                    SetMat();
                }
                else
                {
                    RemoveMat();
                }

                ScanInfos.text = plantScanned + " fully scanned objects \n" + (plantsScanned.Count - plantScanned) + " recorded objects";
            }
            else if (hitObject != null)
            {
                ScanPercent.enabled = false;
                ScanImage.enabled = false;


                if (this.tag == "VR")
                {
                    ScanCone.SetActive(false);
                }

                RemoveMat();

                hitObject = null;
            }
        }
        else if (hitObject != null)
        {
            ScanPercent.enabled = false;
            ScanImage.enabled = false;


            if (this.tag == "VR")
            {
                ScanCone.SetActive(false);
            }

            RemoveMat();

            hitObject = null;
        }
    }

    void SetMat()
    {
        if (hitObject.transform.childCount == 0)
        {
            foreach (var mat in hitObject.GetComponent<MeshRenderer>().materials)
            {
                mat.SetInt("_isScanned", 1);
            }
        }
        else
        {
            if (hitObject.GetComponent<MeshRenderer>() != null)
            {
                foreach (var mat in hitObject.GetComponent<MeshRenderer>().materials)
                {
                    mat.SetInt("_isScanned", 1);
                }
            }

            for (int i = 0; i < hitObject.transform.childCount; i++)
            {
                foreach (var mat in hitObject.transform.GetChild(i).GetComponent<MeshRenderer>().materials)
                {
                    mat.SetInt("_isScanned", 1);
                }
            }
        }
    }

    void RemoveMat()
    {
        if (hitObject.transform.childCount == 0)
        {
            foreach (var mat in hitObject.GetComponent<MeshRenderer>().materials)
            {
                mat.SetInt("_isScanned", 0);
            }
        }
        else
        {
            if (hitObject.GetComponent<MeshRenderer>() != null)
            {
                foreach (var mat in hitObject.GetComponent<MeshRenderer>().materials)
                {
                    mat.SetInt("_isScanned", 0);
                }
            }

            for (int i = 0; i < hitObject.transform.childCount; i++)
            {
                foreach (var mat in hitObject.transform.GetChild(i).GetComponent<MeshRenderer>().materials)
                {
                    mat.SetInt("_isScanned", 0);
                }
            }
        }
    }
}
