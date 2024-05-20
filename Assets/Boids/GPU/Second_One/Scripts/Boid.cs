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

    public Type type;

    float scaredTimer = 0;

    public GameObject player;

    public float rayAngleOffset = 0.2f;

    //private NativeArray<RaycastCommand> raycastCommand;
    //private NativeArray<RaycastHit> raycastResults;

    private RaycastHit[] hits;

    public enum Type
    {
        Bar = 0,
        Clown = 1,
        ClownRouge = 2,
        ChirurgienBleu = 3,
        Shark = 4
    }

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

        hits = new RaycastHit[1];
    //raycastCommand = new NativeArray<RaycastCommand>(2, Allocator.Persistent);
    //raycastResults = new NativeArray<RaycastHit>(2, Allocator.Persistent);
}

    public bool terrain_collision(ref Vector3 collisionForce, ref Vector3 sumForce, Vector3 rayDirection)
    {

        UnityEngine.Profiling.Profiler.BeginSample("RaycastNonAlloc");
        float maxOffset = Mathf.Tan(rayAngleOffset * Mathf.Deg2Rad);

        Vector3 escapeDir = -transform.forward;

        if (Physics.RaycastNonAlloc(transform.position, rayDirection, hits, boidParam.detectCollDst, boidParam.obstacleLayer) > 0)
        {
            //Parcours de la liste des directions echappatoires
            for (int i = 0; i < 50; i +=2)
            {
                Vector3 dir_gold = transform.TransformDirection(tab_Dir[i]);

                //Choisir cette direction si elle permet d'eviter l'obstacle
                int nbCollision = Physics.RaycastNonAlloc(transform.position, dir_gold, hits, boidParam.detectCollDst, boidParam.obstacleLayer);
                if (nbCollision == 0)
                {
                    escapeDir = dir_gold;
                    break;
                }
            }
                //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
            collisionForce = Vector3.ClampMagnitude(escapeDir.normalized * boidParam.maxSpeed - velocity, boidParam.maxSteerForce) * boidParam.weight;

            sumForce += collisionForce;

            UnityEngine.Profiling.Profiler.EndSample();
            return true;
        }

        UnityEngine.Profiling.Profiler.EndSample();
        return false;

    }

    public void new_Boid()
    {
        Vector3 sumForce = Vector3.zero;
        Vector3 collisionForce = Vector3.zero;
        Vector3 escapeDir = -transform.forward;

        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset), UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset), UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset));
        Vector3 newRayDirection = (transform.forward + randomOffset);

        //bool checkCollisionPlayer = false;

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

                    Vector3 directionToPlayer = player.transform.position - transform.position;
                    RaycastHit hitPlayer;
                    int layerMask = 1 << 3;
                    //collision avec le joueur uniquement
                    if (Physics.Raycast(transform.position, directionToPlayer, out hitPlayer, boidParam.scareLayer))
                    {
                        //fuite
                        //escapeDir = -(transform.position - player.transform.position);

                        float angleInRadians = UnityEngine.Random.Range(30f, 69.8f) * Mathf.Deg2Rad;

                        //on définit le signe de l'angle pour que la nouvelle direction l'éloigne du joueur
                        if (Vector3.Cross(directionToPlayer, transform.forward).y > 0)
                        {
                            angleInRadians = -angleInRadians;
                        }

                        Vector3 rotatedDirection = new Vector3(
                            directionToPlayer.x * Mathf.Cos(angleInRadians) - directionToPlayer.z * Mathf.Sin(angleInRadians),
                            directionToPlayer.y,
                            directionToPlayer.x * Mathf.Sin(angleInRadians) + directionToPlayer.z * Mathf.Cos(angleInRadians)
                        );

                        newRayDirection = rotatedDirection;
                        scaredTimer = UnityEngine.Random.Range(0.2f, 2);

                        //Mise a jour de la force finale avec une force rapprochant le poisson du joueur
                        collisionForce = newRayDirection.normalized * boidParam.escapeSpeed * boidParam.weight;

                        sumForce += collisionForce;

                    }
                    //checkCollisionPlayer = true;
                    

                    if (scaredTimer > 0)
                    {
                        scaredTimer -= Time.deltaTime;
                        //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                        escapeDir = ((player.transform.position - transform.position) + transform.forward) / 2;

                        //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                        collisionForce = -escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

                        sumForce += collisionForce;
                    }
                }
            }
            //collision_parallele(ref collisionForce, ref sumForce, checkCollisionPlayer);
            terrain_collision(ref collisionForce, ref sumForce, newRayDirection);
            
        }

        //Mise à jour du mouvement avec la force finale
        velocity += sumForce * Time.deltaTime;

        transform.forward = velocity / velocity.magnitude;
        if (scaredTimer > 0) velocity = transform.forward * boidParam.escapeSpeed;
        else velocity = transform.forward * Mathf.Clamp(velocity.magnitude, boidParam.minSpeed, boidParam.maxSpeed);

        transform.position += velocity * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, 9.5f), transform.position.z);
    }

    //public void collision_parallele(ref Vector3 collisionForce, ref Vector3 sumForce, bool collisionPlayer)
    //{
    //    // rayon joueur
    //    Vector3 directionToPlayer = player.transform.position - transform.position;
    //    raycastCommand[0] = new RaycastCommand(transform.position, directionToPlayer, 7f, boidParam.scareLayer);

    //    // rayon terrain
    //    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset), UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset), UnityEngine.Random.Range(-rayAngleOffset, rayAngleOffset));
    //    Vector3 rayDirection = (transform.forward + randomOffset).normalized;
    //    raycastCommand[1] = new RaycastCommand(transform.position, rayDirection, 10f, boidParam.obstacleLayer);

    //    JobHandle jobHandle = RaycastCommand.ScheduleBatch(raycastCommand, raycastResults, 1, default);
    //    jobHandle.Complete();

    //    if (collisionPlayer)
    //    {
    //        if (raycastResults[0].collider != null)
    //        {
    //            float angleInRadians = UnityEngine.Random.Range(30f, 69.8f) * Mathf.Deg2Rad;

    //            //on définit le signe de l'angle pour que la nouvelle direction l'éloigne du joueur
    //            if (Vector3.Cross(directionToPlayer, transform.forward).y > 0)
    //            {
    //                angleInRadians = -angleInRadians;
    //            }

    //            Vector3 rotatedDirection = new Vector3(
    //                directionToPlayer.x * Mathf.Cos(angleInRadians) - directionToPlayer.z * Mathf.Sin(angleInRadians),
    //                directionToPlayer.y,
    //                directionToPlayer.x * Mathf.Sin(angleInRadians) + directionToPlayer.z * Mathf.Cos(angleInRadians)
    //            );

    //            Vector3 escapeDir = rotatedDirection;
    //            scaredTimer = UnityEngine.Random.Range(0.2f, 2);

    //            //Mise a jour de la force finale avec une force rapprochant le poisson du joueur
    //            collisionForce = escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

    //            sumForce += collisionForce;
    //        }
    //    }

    //    if (raycastResults[1].collider != null)
    //    {
    //        Vector3 escapeDir = -transform.forward;
    //        RaycastHit[] hits = new RaycastHit[1];
    //        //Parcours de la liste des directions echappatoires
    //        for (int i = 0; i < tab_Dir.Length; i += 2)
    //        {
    //            Vector3 dir_gold = transform.TransformDirection(tab_Dir[i]);
    //            Ray ray = new Ray(transform.position, dir_gold);
    //            //Choisir cette direction si elle permet d'eviter l'obstacle
    //            int nbCollision = Physics.RaycastNonAlloc(ray, hits, boidParam.detectCollDst, boidParam.obstacleLayer);
    //            if (nbCollision == 0)
    //            {
    //                escapeDir = dir_gold;
    //                break;
    //            }
    //        }
    //        //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
    //        collisionForce = Vector3.ClampMagnitude(escapeDir.normalized * boidParam.maxSpeed - velocity, boidParam.maxSteerForce) * boidParam.weight;

    //        sumForce += collisionForce;
    //    }
    //}

    //void OnDestroy()
    //{
    //    raycastCommand.Dispose();
    //    raycastResults.Dispose();
    //}
}