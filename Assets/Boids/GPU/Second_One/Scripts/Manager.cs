using System;
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

    public float zFar; // Distance maximale entre la caméra et les boids
    public float zNear;
    public float zFarCalcul;
    public LayerMask obstacleLayer;
    public float frustumMargin;
    public float raycastMargin;

    void Start()
    {
        Vector3 playerPos = new Vector3(player.position.x, -spawnRay, player.position.z);
        listOfBoids = new List<Boid>[Enum.GetNames(typeof(Boid.Type)).Length];

        List<CPU_PARAMETERS> listOfBoidData = new List<CPU_PARAMETERS>();

        for (int it = 0; it < listOfBoids.Length; it++)
        {
            listOfBoids[it] = new List<Boid>();

            for (int i = 0; i < (int)((percentSpawn[it] / 100.0f) * nbOfBoids); i++)
            {
                Vector3 pos = playerPos + UnityEngine.Random.insideUnitSphere * spawnRay;
                Boid boid = Instantiate(prefabs[it]);

                boid.transform.parent = transform;
                boid.transform.position = pos;

                Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
                float maxInclinationAngle = 30f;
                float maxInclinationRadians = maxInclinationAngle * Mathf.Deg2Rad;
                randomDirection.y = Mathf.Sin(maxInclinationRadians);
                float horizontalMagnitude = Mathf.Cos(maxInclinationRadians);
                randomDirection.x *= horizontalMagnitude;
                randomDirection.z *= horizontalMagnitude;
                randomDirection.Normalize();

                boid.transform.forward = randomDirection;
                listOfBoids[it].Add(boid);

                boid.Init(boidParam[it]);
            }

            CPU_PARAMETERS boidData = new CPU_PARAMETERS
            {
                percRay = boidParam[it].percRay,
                avoidRay = boidParam[it].avoidRay,
                maxSpeed = boidParam[it].maxSpeed,
                maxSteerForce = boidParam[it].maxSteerForce
            };
            listOfBoidData.Add(boidData);
        }

        // Passez le ComputeBuffer au shader
        using (ComputeBuffer parameterBuffer = new ComputeBuffer(listOfBoids.Length, sizeof(float) * 4))
        {
            parameterBuffer.SetData(listOfBoidData.ToArray());
            computeShader.SetBuffer(0, "boidParameters", parameterBuffer);
        }
    }

    void Update()
    {
        if (listOfBoids == null) return;

        Camera cam = Camera.main;
        int activeBoids = 0;
        int inactiveBoids = 0;
        int newBoids = 0;
        int notNewBoids = 0;

        for (int it = 0; it < listOfBoids.Length; it++)
        {
            if (listOfBoids[it] == null) continue;

            int sizeListOfBoids = listOfBoids[it].Count;
            CPU_BOID[] cpuBoid = new CPU_BOID[sizeListOfBoids];

            for (int i = 0; i < sizeListOfBoids; i++)
            {
                cpuBoid[i].pos = listOfBoids[it][i].transform.position;
                cpuBoid[i].dir = listOfBoids[it][i].transform.forward;
                cpuBoid[i].vel = listOfBoids[it][i].velocity;
            }

            int threadGroups = Mathf.CeilToInt(sizeListOfBoids / 64.0f);
            using (ComputeBuffer computeBuffer = new ComputeBuffer(sizeListOfBoids, sizeof(int) + 27 * sizeof(float)))
            {
                computeBuffer.SetData(cpuBoid);
                computeShader.SetBuffer(0, "boids", computeBuffer);
                computeShader.SetInt("sizeListOfBoids", sizeListOfBoids);
                computeShader.SetInt("listOfBoidID", it);
                computeShader.Dispatch(0, threadGroups, 1, 1);
                computeBuffer.GetData(cpuBoid);
            }

            for (int i = 0; i < sizeListOfBoids; i++)
            {
                Boid currentBoid = listOfBoids[it][i];

                if (IsInView(cam, currentBoid.transform.position))
                {
                    currentBoid.gameObject.SetActive(true);
                    activeBoids++;
                }
                else
                {
                    currentBoid.gameObject.SetActive(false);
                    inactiveBoids++;
                }

                if (!IsFar(cam, currentBoid.transform.position))
                {
                    currentBoid.nbTeammates = cpuBoid[i].nbTeammates;
                    currentBoid.alignmentForce = cpuBoid[i].alignmentForce;
                    currentBoid.cohesionForce = cpuBoid[i].cohesionForce;
                    currentBoid.seperationForce = cpuBoid[i].seperationForce;
                    currentBoid.new_Boid();
                    newBoids++;
                }
                else notNewBoids++;


            }
        }
        //zFarCalcul = CalculateDynamicZFar(newBoids, zFarCalcul, zFar);
        //zFar = CalculateDynamicZFarActive(zFarCalcul, zFar);
        //UnityEngine.Debug.Log($"Active Boids: {activeBoids}, Inactive Boids: {inactiveBoids}, New Boids: {newBoids}, not new boids: {notNewBoids}, total boids :{newBoids + notNewBoids}, zFar :{zFar},  zFarCalcul:{zFarCalcul}");
    }

    float CalculateDynamicZFar(int newBoidsCount, float zFarCalcul, float zFar)
    {
        // ajuste zFarCalcul en fonction du nombre de nouveaux boids calculés.
        float newZFarCalcul = 0;
        if (newBoidsCount >= 300) 
        {
            newZFarCalcul = Mathf.Max((zFarCalcul - 1), -1);
            
        }
        else if (newBoidsCount <= 300) 
        {
            newZFarCalcul = zFarCalcul + 0.5f;
        }


        return newZFarCalcul;
    }

    float CalculateDynamicZFarActive(float ZfarCalcul, float zFar) 
    {
        if (ZfarCalcul < zFar) zFar = Mathf.Max(zFar - 5f, 25f);
        else if (zFar < ZfarCalcul + 5f && zFar < 40f) zFar += 5f;
        return zFar;
    }


    bool IsInView(Camera cam, Vector3 worldPosition)
    {
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPosition);
        float distance = Vector3.Distance(cam.transform.position, worldPosition);

        bool isInViewFrustum = (viewportPos.x >= -frustumMargin && viewportPos.x <= 1 + frustumMargin &&
                               viewportPos.y >= -frustumMargin && viewportPos.y <= 1 + frustumMargin &&
                               viewportPos.z > 0 && distance <= zFar) || distance <= zNear;

        if (!isInViewFrustum)
        {
            return false;
        }

        // Adjust the direction for the raycast to include margin
        Vector3 rayDirection = worldPosition - cam.transform.position;
        rayDirection += rayDirection.normalized * raycastMargin;

        return !Physics.Raycast(cam.transform.position, rayDirection, out _, distance, obstacleLayer);
    }

    bool IsFar(Camera cam, Vector3 worldPosition)
    {
        float distance = Vector3.Distance(cam.transform.position, worldPosition);
        return distance >= zFarCalcul;
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
