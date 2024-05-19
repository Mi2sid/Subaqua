using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabMovement : MonoBehaviour
{
    public float moveSpeed = 1.0f; // Vitesse de d�placement du crabe
    public float turnSpeed = 180.0f; // Vitesse de rotation du crabe
    public float crabHeight = 0.5f; // Hauteur du crabe par rapport au sol
    public float crabRotationOffset = -90.0f; // Rotation initiale du crabe par rapport au sol
    public float maxTimeMoving = 3.0f; // Temps maximal pendant lequel le crabe peut bouger

    private MeshCollider groundCollider; // R�f�rence au MeshCollider du sol
    private Vector3 groundPoint; // Point du sol � l'endroit o� se trouve le crabe
    private float crabDirection; // Direction de d�placement du crabe
    private float timeMoving; // Temps pendant lequel le crabe se d�place
    private float timeUntilNextMove; // Temps avant le prochain d�placement du crabe
    private bool changeDirection = true;

    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private float currentRotationTime = 0.0f;
    private float rotationDuration = 2.0f;

    void Start()
    {
        // Initialisation de la direction de d�placement et du temps avant le prochain d�placement
        crabDirection = Random.Range(0.0f, 360.0f);
        timeUntilNextMove = Random.Range(0.0f, 10.0f);
        timeMoving = Random.Range(1.0f, maxTimeMoving);
        initialRotation = transform.rotation;
        targetRotation = Quaternion.identity;
    }

    void Update()
    {
        // V�rification du temps avant le prochain d�placement
        timeMoving -= Time.deltaTime;

        if(timeMoving > 0.0f)
        {
            // D�placement du crabe dans sa nouvelle direction
            Ray ray = new Ray(transform.position + transform.up * 0.3f, -transform.up);
            RaycastHit hitInfo;
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("Terrain");


            Vector3 crabMovement = Quaternion.Euler(new Vector3(0.0f, crabDirection, 0.0f)) * Vector3.forward;
            crabMovement *= moveSpeed * Time.deltaTime;
            transform.position += crabMovement;

            bool isHit = Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask);
            if (!isHit)
            {
                crabMovement = Quaternion.Euler(new Vector3(0.0f, crabDirection, 0.0f)) * Vector3.forward;
                crabMovement *= moveSpeed * Time.deltaTime;
                transform.position += crabMovement;
            }

            if (isHit)
            {
                // Calcul de la hauteur et de la rotation du sol � l'endroit o� se trouve le crabe
                groundCollider = hit.collider.gameObject.GetComponent<MeshCollider>();

                if (groundCollider.Raycast(ray, out hitInfo, Mathf.Infinity))
                {
                    groundPoint = hitInfo.point;
                    Quaternion groundRotation = Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, groundRotation, turnSpeed * Time.deltaTime);
                }

                // Positionnement du crabe � la m�me hauteur que le sol
                transform.position = new Vector3(groundPoint.x, groundPoint.y + crabHeight, groundPoint.z);
                transform.position += crabMovement;
            }
        }
        else
        {
            if(changeDirection)
            {
                crabDirection = Random.Range(0.0f, 360.0f);
                changeDirection = false;

                initialRotation = transform.rotation;

                // Calculer la rotation cible en utilisant l'angle souhait�
                targetRotation = initialRotation * Quaternion.Euler(0, crabDirection, 0);
                currentRotationTime = 0.0f;
            }

            // V�rification du temps avant le prochain d�placement
            timeUntilNextMove -= Time.deltaTime;

            // Incr�menter le temps �coul� depuis le d�but de la rotation
            currentRotationTime += Time.deltaTime;

            // Calculer le ratio d'ach�vement de la rotation (entre 0 et 1)
            float t = Mathf.Clamp01(currentRotationTime / rotationDuration);

            if (t < 1.0f)
            {
                // Interpoler entre la rotation initiale et la rotation cible
                Quaternion newRotation = Quaternion.Lerp(initialRotation, targetRotation, t);

                // Appliquer la nouvelle rotation � l'objet
                transform.rotation = newRotation;
            }

            if (timeUntilNextMove <= 0.0f && t >= 1.0f)
            {
                // Changement de direction et de temps avant le prochain d�placement
                timeUntilNextMove = Random.Range(0.0f, 10.0f);
                timeMoving = Random.Range(0.0f, maxTimeMoving);
                changeDirection = true;
            }
        }       
    }
}