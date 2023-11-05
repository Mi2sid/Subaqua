using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

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

        for (int it = 0; it < listOfBoids.Length; it++)
        {
            listOfBoids[it] = new List<Boid> { };

            for (int i = 0; i < (int)((percentSpawn[it] / 100.0f) * nbOfBoids); i++)
            {
                Vector3 pos = playerPos + Random.insideUnitSphere * spawnRay;

                Boid boid = Instantiate(prefabs[it]);

                boid.transform.parent = gameObject.transform;

                boid.transform.position = pos;

                boid.transform.forward = Random.insideUnitSphere;

                listOfBoids[it].Add(boid);

                boid.Init(boidParam[it]);
            }
        }


        
    }

    void Update() 
    {

        //Initailisation des parametres
        if (listOfBoids != null) 
        {
            CPU_BOID[][] cpuBoid = new CPU_BOID[listOfBoids.Length][];
            ComputeBuffer[] computeBuffer = new ComputeBuffer[listOfBoids.Length];

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
                    computeShader.SetFloat("percRay", boidParam[it].percRay);
                    computeShader.SetFloat("avoidRay", boidParam[it].avoidRay);
                    computeShader.SetFloat("maxSpeed", boidParam[it].maxSpeed);
                    computeShader.SetFloat("maxSteerForce", boidParam[it].maxSteerForce);

                    //Appel du compute shader (GPU)
                    computeShader.Dispatch(0, threadGroups, 1, 1);

                    //Recupertation des donnees calculee par le compute shader
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
            

            //computeBuffer.Release();
        }
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