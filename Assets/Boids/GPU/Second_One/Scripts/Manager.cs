using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System.Linq;

public class Manager : MonoBehaviour
{

    List<Boid>[] listOfBoids;

    public BoidParameters[] boidParam;
    public ComputeShader computeShader;

    public Transform player;

    public Boid[] prefabs;

    public int spawnRay;

    public int nbOfBoids;

    public float[] percentSpawn;

    //Instanciation et initialisation des boids a une position et rotation aleatoire
    void Start()
    {
        Vector3 playerPos = new Vector3(player.position.x, -spawnRay, player.position.z);

        listOfBoids = new List<Boid>[System.Enum.GetNames(typeof(Boid.Type)).Length];

        // Créez le ComputeBuffer avec la taille de votre liste de données
        ComputeBuffer parameterBuffer = new ComputeBuffer(listOfBoids.Length, sizeof(float)*4);

        List<CPU_PARAMETERS> listOfBoidData = new List<CPU_PARAMETERS>();

        for (int it = 0; it < listOfBoids.Length; it++)
        {
            listOfBoids[it] = new List<Boid> { };

            for (int i = 0; i < (int)((percentSpawn[it] / 100.0f) * nbOfBoids); i++)
            {
                Vector3 pos = playerPos + UnityEngine.Random.insideUnitSphere * spawnRay;

                Boid boid = Instantiate(prefabs[it]);

                boid.transform.parent = gameObject.transform;

                boid.transform.position = pos;

                Vector3 randomDirection = UnityEngine.Random.onUnitSphere;

                // Restreindre la composante Y pour éviter les mouvements purement verticaux
                float maxInclinationAngle = 30f; // Angle maximal d'inclinaison en degrés
                float maxInclinationRadians = maxInclinationAngle * Mathf.Deg2Rad;
                randomDirection.y = Mathf.Sin(maxInclinationRadians);

                // Recalculer la composante X et Z pour que le vecteur soit unitaire
                float horizontalMagnitude = Mathf.Cos(maxInclinationRadians);
                randomDirection.x *= horizontalMagnitude;
                randomDirection.z *= horizontalMagnitude;

                randomDirection.Normalize();

                boid.transform.forward = randomDirection;

                boid.transform.forward = UnityEngine.Random.insideUnitSphere;

                listOfBoids[it].Add(boid);

                boid.Init(boidParam[it]);
            }

            CPU_PARAMETERS boidData = new CPU_PARAMETERS();
            boidData.percRay = boidParam[it].percRay;
            boidData.avoidRay = boidParam[it].avoidRay;
            boidData.maxSpeed = boidParam[it].maxSpeed;
            boidData.maxSteerForce = boidParam[it].maxSteerForce;

            listOfBoidData.Add(boidData);
        }

        parameterBuffer.SetData(listOfBoidData.ToArray());

        // Passez le ComputeBuffer au shader
        computeShader.SetBuffer(0, "boidParameters", parameterBuffer);
    }

    //[BurstCompile]
    //public struct Pass_GPU : IJobParallelFor
    //{

    //    //public NativeArray<List<Boid>> jobListOfBoids;

    //    public NativeArray<Vector3>[] positions;
    //    public NativeArray<Vector3>[] directions;
    //    public NativeArray<Vector3>[] velocities;

    //    public ComputeShader computeShader;
    //    public CPU_BOID[][] jobCpuBoid;
    //    public ComputeBuffer[] jobComputeBuffer;

    //    public void Execute(int index)
    //    {
    //        if (jobListOfBoids[index] != null)
    //        {
    //            int sizeListOfBoids = jobListOfBoids[index].Count;
    //            jobCpuBoid[index] = new CPU_BOID[sizeListOfBoids];

    //            //Creation du computeBuffer de la taille de la classe CPU_BOID
    //            jobComputeBuffer[index] = new ComputeBuffer(sizeListOfBoids, (int)(sizeof(int) + 27 * sizeof(float)));

    //            //Le castage en (int) ne fonctionne pas autrement
    //            int threadGroups = Mathf.CeilToInt(sizeListOfBoids / (float)64);

    //            //Recuperation des donnees du boid a l'instant t
    //            for (int i = 0; i < jobListOfBoids[index].Count; i++)
    //            {
    //                jobCpuBoid[index][i].pos = listOfBoids[index][i].transform.position;
    //                jobCpuBoid[index][i].dir = listOfBoids[index][i].transform.forward;
    //                jobCpuBoid[index][i].vel = listOfBoids[index][i].velocity;
    //            }

    //            //Passage des parametres au GPU
    //            jobComputeBuffer[index].SetData(jobCpuBoid[index]);

    //            computeShader.SetBuffer(0, "boids", jobComputeBuffer[index]);
    //            computeShader.SetInt("sizeListOfBoids", jobListOfBoids[index].Count);
    //            computeShader.SetInt("listOfBoidID", index);

    //            //Appel du compute shader (GPU)
    //            computeShader.Dispatch(0, threadGroups, 1, 1);

    //            //Recuperation des donnees calculees par le compute shader
    //            jobComputeBuffer[index].GetData(jobCpuBoid[index]);
    //            jobComputeBuffer[index].Dispose();

    //            //Attribution de ces valeurs aux boids de la scene (maj CPU)
    //            for (int i = 0; i < jobListOfBoids[index].Count; i++)
    //            {
    //                jobListOfBoids[index][i].nbTeammates = jobCpuBoid[it][i].nbTeammates;

    //                jobListOfBoids[index][i].alignmentForce = jobCpuBoid[index][i].alignmentForce;
    //                jobListOfBoids[index][i].cohesionForce = jobCpuBoid[index][i].cohesionForce;
    //                jobListOfBoids[index][i].seperationForce = jobCpuBoid[index][i].seperationForce;

    //                //Debug des 3 lois
    //                /*Debug.Log("GPU 1: " + listOfBoids[i].alignmentForce);
    //                Debug.Log("GPU 2: " + listOfBoids[i].cohesionForce);
    //                Debug.Log("GPU 3: " + listOfBoids[i].seperationForce);*/

    //                jobListOfBoids[index][i].new_Boid();
    //            }
    //        }
    //    }
    //}

    void Update()
    {

        //Initailisation des parametres
        if (listOfBoids != null)
        {
            CPU_BOID[][] cpuBoid = new CPU_BOID[listOfBoids.Length][];
            ComputeBuffer[] computeBuffer = new ComputeBuffer[listOfBoids.Length];

            //Boid[] boidArray = listOfBoids.ToArray();
            //NativeArray<Vector3> positions = new NativeArray<Vector3>(boidArray.Length, Allocator.TempJob);
            //NativeArray<Vector3> directions = new NativeArray<Vector3>(boidArray.Length, Allocator.TempJob);
            //NativeArray<Vector3> velocity = new NativeArray<Vector3>(boidArray.Length, Allocator.TempJob);

            //for (int i = 0; i < boidArray.Length; i++)
            //{
            //    positions[i] = boidArray[i].transform.position;
            //    directions[i] = boidArray[i].transform.forward;
            //    velocity[i] = boidArray[i].velocity;
            //}

            //Pass_GPU pass_GPU = new Pass_GPU
            //{
            //    //jobListOfBoids = new NativeArray<List<Boid>>(boidArray, Allocator.TempJob),

            //    positions = positions,
            //    directions = directions,
            //    velocity = velocity,
            //    computeShader = computeShader,
            //    jobCpuBoid= cpuBoid,
            //    jobComputeBuffer = computeBuffer
            //};

            //JobHandle jobHandle = pass_GPU.Schedule(listOfBoids.Length, threadGroups);

            //// Attendre la fin du job
            //jobHandle.Complete();

            //// Libérer la mémoire du ComputeBuffer et du NativeArray
            //jobListOfBoids.Dispose();
            //cpuBoid = null;
            //computeBuffer = null;

            for (int it = 0; it < listOfBoids.Length; it++)
            {
                if (listOfBoids[it] != null)
                {
                    int sizeListOfBoids = listOfBoids[it].Count;
                    cpuBoid[it] = new CPU_BOID[sizeListOfBoids];

                    //Creation du computeBuffer de la taille de la classe CPU_BOID
                    computeBuffer[it] = new ComputeBuffer(sizeListOfBoids, (int)(sizeof(int) + 27 * sizeof(float)));

                    //Le castage en (int) ne fonctionne pas autrement
                    int threadGroups = Mathf.CeilToInt(sizeListOfBoids / (float)64);

                    //Recuperation des donnees du boid a l'instant t
                    for (int i = 0; i < listOfBoids[it].Count; i++)
                    {
                        cpuBoid[it][i].pos = listOfBoids[it][i].transform.position;
                        cpuBoid[it][i].dir = listOfBoids[it][i].transform.forward;
                        cpuBoid[it][i].vel = listOfBoids[it][i].velocity;
                    }

                    //Passage des parametres au GPU
                    computeBuffer[it].SetData(cpuBoid[it]);

                    computeShader.SetBuffer(0, "boids", computeBuffer[it]);
                    computeShader.SetInt("sizeListOfBoids", listOfBoids[it].Count);
                    computeShader.SetInt("listOfBoidID", it);

                    //Appel du compute shader (GPU)
                    computeShader.Dispatch(0, threadGroups, 1, 1);

                    //Recuperation des donnees calculees par le compute shader
                    computeBuffer[it].GetData(cpuBoid[it]);
                    computeBuffer[it].Dispose();

                    //Attribution de ces valeurs aux boids de la scene (maj CPU)
                    for (int i = 0; i < listOfBoids[it].Count; i++)
                    {
                        listOfBoids[it][i].nbTeammates = cpuBoid[it][i].nbTeammates;

                        listOfBoids[it][i].alignmentForce = cpuBoid[it][i].alignmentForce;
                        listOfBoids[it][i].cohesionForce = cpuBoid[it][i].cohesionForce;
                        listOfBoids[it][i].seperationForce = cpuBoid[it][i].seperationForce;

                        //Debug des 3 lois
                        /*Debug.Log("GPU 1: " + listOfBoids[i].alignmentForce);
                        Debug.Log("GPU 2: " + listOfBoids[i].cohesionForce);
                        Debug.Log("GPU 3: " + listOfBoids[i].seperationForce);*/

                        listOfBoids[it][i].new_Boid();
                    }
                }
            }

            cpuBoid = null;
            computeBuffer = null;
            //computeBuffer.Release();
        }
    }

    public struct CPU_PARAMETERS
    {
        public float percRay;
        public float avoidRay;
        public float maxSpeed;
        public float maxSteerForce;
    }

    public struct CPU_BOID
    {
        public int nbTeammates;

        public Vector3 pos;
        public Vector3 dir;
        public Vector3 vel;

        public Vector3 groupBoss;
        public Vector3 groupMiddle;
        public Vector3 avoid;

        public Vector3 alignmentForce;
        public Vector3 cohesionForce;
        public Vector3 seperationForce;
    }
}