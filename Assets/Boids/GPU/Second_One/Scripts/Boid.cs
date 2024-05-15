using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.ComponentModel;

public class Boid : MonoBehaviour
{
    [HideInInspector]
    public Vector3[] tab_Dir;

    [HideInInspector]
    public Vector3 velocity;

    [HideInInspector]
    public int nbTeammates;

    [HideInInspector]
    public Vector3 alignmentForce;

    [HideInInspector]
    public Vector3 cohesionForce;

    [HideInInspector]
    public Vector3 seperationForce;

    BoidParameters boidParam;

    //Collider boidCollider = GetComponent<Collider>();

    public Type type;

    float scaredTimer = 0;
    Vector3 escapeDir = Vector3.zero;

    private GameObject player;

    public enum Type
    {
        Bar = 0,
        Clown = 1,
        ClownRouge = 2,
        ChirurgienBleu = 3,
        Shark = 4
    }

    //[BurstCompile]
    //public struct SphereCastJob : IJobParallelFor
    //{
    //    public Vector3 position;
    //    public Quaternion rotation;
    //    [Unity.Collections.ReadOnlyAttribute] public NativeArray<Vector3> tab_Dir;
    //    public float sphereRad;
    //    public float detectCollDst;
    //    public LayerMask obstacleLayer;
    //    public Vector3 escapeDirs;
    //    public bool foundEscape;

    //    public void Execute(int index)
    //    {
    //        if (foundEscape)
    //            return; // If escape direction already found, exit early

    //        Vector3 dir_gold = rotation * tab_Dir[index];
    //        Ray ray = new Ray(position, dir_gold);

    //        bool canEscape = !Physics.SphereCast(ray, sphereRad, detectCollDst, obstacleLayer);

    //        if (canEscape)
    //        {
    //            escapeDirs = dir_gold;
    //            foundEscape = true; // Set flag to indicate escape direction found
    //        }
    //    }
    //}

    //Creation des rayons de la sphere de la Fibonacci
    public void Awake()
    {
        tab_Dir = new Vector3[300];

        float grad = Mathf.PI * 2 * ((1 + Mathf.Sqrt(5)) / 2);

        for (int i = 0; i < tab_Dir.Length; i++)
        {
            float angle = Mathf.Acos(1 - 2 * ((float)i / tab_Dir.Length));
            float declin = grad * i;

            tab_Dir[i] = new Vector3(Mathf.Cos(declin) * Mathf.Sin(angle), Mathf.Sin(declin) * Mathf.Sin(angle), Mathf.Cos(angle));
        }

        player = GameObject.FindWithTag("Player");
    }

    //Initialisation du boid
    public void Init(BoidParameters param)
    {
        this.boidParam = param;

        velocity = transform.forward * boidParam.avgSpeed;
    }

    public void terrain_collision(ref Vector3 collisionForce, ref Vector3 sumForce, ref Vector3 escapeDir)
    {
        RaycastHit hitTerrain;
        if (Physics.Raycast(transform.position, transform.forward, out hitTerrain, 10f))
        {
            //Parcours de la liste des directions echappatoires
            for (int i = 0; i < tab_Dir.Length; i +=2)
            {
                Vector3 dir_gold = transform.TransformDirection(tab_Dir[i]);
                Ray ray = new Ray(transform.position, dir_gold);

                //Choisir cette direction si elle permet d'eviter l'obstacle
                bool canEscape = !Physics.Raycast(ray, out RaycastHit hit2, boidParam.detectCollDst, boidParam.obstacleLayer);
                if (canEscape)
                {
                    escapeDir = dir_gold;
                    break;
                }
                else escapeDir = transform.forward;
            }
                //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
            collisionForce = Vector3.ClampMagnitude(escapeDir.normalized * boidParam.maxSpeed - velocity, boidParam.maxSteerForce) * boidParam.weight;

                //job.tab_Dir.Dispose();
            sumForce += collisionForce;
        }

    }

    public void new_Boid()
    {
        Vector3 sumForce = Vector3.zero;
        Vector3 collisionForce = Vector3.zero;

        if (nbTeammates > 0)
        {
            //Force finale calculee par le compute shader (3 lois)
            sumForce += alignmentForce + cohesionForce + seperationForce;
        }

        float distanceBoidPlayer = Vector3.Distance(player.transform.position, transform.position);

        if (distanceBoidPlayer > 75)
        {
            escapeDir = player.transform.position - transform.position;

            //Mise a jour de la force finale avec une force rapprochant le poisson du joueur
            collisionForce = escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

            sumForce += collisionForce;
        }

        //Si une collision est detectee en face du boid
        else if (distanceBoidPlayer < 50)
        {
            // TEST
            // --------------------------------------------------------------------------------------------------------------------------------------
            if (distanceBoidPlayer < 7)
            {
                if (boidParam.canBeScared)
                {
                    if (scaredTimer > 0)
                    {
                        scaredTimer -= Time.deltaTime;
                        //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                        escapeDir = ((player.transform.position - transform.position) + transform.forward) / 2;

                        //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                        collisionForce = -escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

                        sumForce += collisionForce;
                    }

                    RaycastHit hitPlayer;
                    int layerMask = 1 << 3;
                    //collision avec le joueur uniquement
                    if (Physics.Raycast(transform.position, player.transform.position - transform.position, out hitPlayer))
                    {
                        //fuite
                        //escapeDir = -(transform.position - player.transform.position);

                        // Calcul de la direction du joueur par rapport à la position actuelle du boid
                        Vector3 directionToPlayer = player.transform.position - transform.position;

                        // Rotation de 40 degrés dans le plan horizontal
                        float angleInRadians = 40f * Mathf.Deg2Rad;
                        Vector3 rotatedDirection = new Vector3(
                            directionToPlayer.x * Mathf.Cos(angleInRadians) - directionToPlayer.z * Mathf.Sin(angleInRadians),
                            directionToPlayer.y,
                            directionToPlayer.x * Mathf.Sin(angleInRadians) + directionToPlayer.z * Mathf.Cos(angleInRadians)
                        );

                        // Normalisation de la nouvelle direction décalée
                        Vector3 escapeDir = rotatedDirection.normalized;
                        scaredTimer = UnityEngine.Random.Range(0.2f, 2);

                    }
                }
            }
            terrain_collision(ref collisionForce, ref sumForce, ref escapeDir);
            
        }


        //Mise à jour du mouvement avec la force finale
        velocity += sumForce * Time.deltaTime;

        transform.forward = velocity / velocity.magnitude;
        if (scaredTimer > 0) velocity = transform.forward * boidParam.escapeSpeed;
        else velocity = transform.forward * Mathf.Clamp(velocity.magnitude, boidParam.minSpeed, boidParam.maxSpeed);

        transform.position += velocity * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, 9.5f), transform.position.z);
    }
}