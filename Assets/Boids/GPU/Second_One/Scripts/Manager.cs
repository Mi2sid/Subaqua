﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

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
        ComputeBuffer parameterBuffer = new ComputeBuffer(listOfBoids.Length, sizeof(float) * 4);

        List<CPU_PARAMETERS> listOfBoidData = new List<CPU_PARAMETERS>();       // mettre les données sur CPU pour ensuite sur GPU

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

                boid.Init(boidParam[it]);       // initialise le boid
            }

            CPU_PARAMETERS boidData = new CPU_PARAMETERS();
            boidData.percRay = boidParam[it].percRay;
            boidData.avoidRay = boidParam[it].avoidRay;
            boidData.maxSpeed = boidParam[it].maxSpeed;
            boidData.maxSteerForce = boidParam[it].maxSteerForce;
            listOfBoidData.Add(boidData);
        }

        parameterBuffer.SetData(listOfBoidData.ToArray());      // mettre les données sur GPU

        // Passez le ComputeBuffer au shader
        computeShader.SetBuffer(0, "boidParameters", parameterBuffer);
    }

    void Update()
    {
        //Initailisation des parametres
        if (listOfBoids != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CPU_BOID[][] cpuBoid = new CPU_BOID[listOfBoids.Length][];
                ComputeBuffer[] computeBuffer = new ComputeBuffer[listOfBoids.Length];

                for (int it = 0; it < listOfBoids.Length; it++)
                {
                    if (listOfBoids[it] != null)
                    {
                        int sizeListOfBoids = listOfBoids[it].Count;                // on récupère le nombre de boid de chaque type de boid.
                        //{ UnityEngine.Debug.Log("Score du joueur : " + sizeListOfBoids); }

                        cpuBoid[it] = new CPU_BOID[sizeListOfBoids];

                        //Creation du computeBuffer de la taille de la classe CPU_BOID
                        computeBuffer[it] = new ComputeBuffer(sizeListOfBoids, (int)(sizeof(int) + 27 * sizeof(float)));

                        //Le castage en (int) ne fonctionne pas autrement
                        int threadGroups = Mathf.CeilToInt(sizeListOfBoids / (float)64);

                        for (int i = 0; i < listOfBoids[it].Count; i++)
                        {
                            Boid boid = listOfBoids[it][i];
                            Vector3 viewportPos = mainCamera.WorldToViewportPoint(boid.transform.position);

                            // Check if boid is within the camera's viewport
                            if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z >= 0)
                            {
                                cpuBoid[it][i].pos = boid.transform.position;
                                cpuBoid[it][i].dir = boid.transform.forward;
                                cpuBoid[it][i].vel = boid.velocity;
                            }
                        }

                        //Passage des parametres au GPU
                        computeBuffer[it].SetData(cpuBoid[it]);

                        computeShader.SetBuffer(0, "boids", computeBuffer[it]);
                        computeShader.SetInt("sizeListOfBoids", listOfBoids[it].Count);
                        computeShader.SetInt("listOfBoidID", it);

                        //Appel du compute shader (GPU)
                        computeShader.Dispatch(0, threadGroups, 1, 1);

                        //Recuperation des donnees calculee par le compute shader
                        computeBuffer[it].GetData(cpuBoid[it]);
                        computeBuffer[it].Dispose();

                        //Attribution de ces valeurs aux boids de la scene (maj CPU)
                        for (int i = 0; i < listOfBoids[it].Count; i++)
                        {
                            Boid boid = listOfBoids[it][i];
                            Vector3 viewportPos = mainCamera.WorldToViewportPoint(boid.transform.position);

                            // Check if boid is within the camera's viewport
                            if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z >= 0)
                            {
                                boid.nbTeammates = cpuBoid[it][i].nbTeammates;
                                boid.alignmentForce = cpuBoid[it][i].alignmentForce;
                                boid.cohesionForce = cpuBoid[it][i].cohesionForce;
                                boid.seperationForce = cpuBoid[it][i].seperationForce;

                                //Debug des 3 lois
                                /*Debug.Log("GPU 1: " + listOfBoids[i].alignmentForce);
                                Debug.Log("GPU 2: " + listOfBoids[i].cohesionForce);
                                Debug.Log("GPU 3: " + listOfBoids[i].seperationForce);*/

                                boid.new_Boid();
                            }
                        }
                    }
                }

                cpuBoid = null;
                computeBuffer = null;
                //computeBuffer.Release();
            }
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
