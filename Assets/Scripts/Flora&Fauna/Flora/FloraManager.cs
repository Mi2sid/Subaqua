using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Threading;

public class FloraManager : MonoBehaviour
{
    public List<GameObject> slopeFlora;
    public List<GameObject> flatFlora;
    public float slopeAngle = 50f;

    private int[] slopeFloraIds;
    private int[] flatFloraIds;

    public struct FloraDataResult
    {
        public GroundManager.MeshPointInfo[] infos;
        public int[] floraCount;
    }

    private void Awake()
    {
        slopeFloraIds = new int[slopeFlora.Count];
        flatFloraIds = new int[flatFlora.Count];

        // Création des reserves d'objets pour la flore
        ChunkSystem chunkSystem = GetComponent<ChunkSystem>();
        int maxObjects = ((chunkSystem.viewDist * 2 + 1) + (chunkSystem.hviewDist * 2 + 1)) * Vars.BASE_FLORA_DENSITY;
        for (int i = 0; i < flatFlora.Count; i++)
            flatFloraIds[i] = GameObjectPool.inst.CreateNewPool(flatFlora[i], maxObjects / flatFlora.Count);

        for (int i = 0; i < slopeFlora.Count; i++)
            slopeFloraIds[i] = GameObjectPool.inst.CreateNewPool(slopeFlora[i], maxObjects / slopeFlora.Count);
    }

    /// <summary>
    /// Initialise la flore sur le mesh selon les infos contenues dans 'infos'.
    /// Il faut également spécifier le parent de l'objet (chunk) et donner le 'floraCount'
    /// </summary>
    public GroundManager.MeshGround InitializeFlora(Transform parent, GroundManager.MeshPointInfo[] infos, int[] floraCount, GroundManager.Biome biome)
    {
        if (infos == null)
        {
            return new GroundManager.MeshGround(null, null);
        }

        // Récupération des réserves de flore pour terrain plat
        Dictionary<int, GameObject[]> flatPool = new Dictionary<int, GameObject[]>(flatFlora.Count);
        Dictionary<int, int> nextFlatId = new Dictionary<int, int>(flatFlora.Count);
        for (int i = 0; i < flatFlora.Count; i++)
        {
            GameObject[] flatGMs = GameObjectPool.inst.Request(flatFloraIds[i], floraCount[i]);
            List<GameObject> finalFlatGMs = new List<GameObject>();
            foreach (var item in flatGMs)
            {
                if (System.Array.IndexOf(item.GetComponent<FloraParameters>().biomes, biome) != -1) finalFlatGMs.Add(item);
            }

            flatPool.Add(flatFloraIds[i], finalFlatGMs.ToArray());
            nextFlatId.Add(flatFloraIds[i], 0);
        }

        // Récupération des réserves de flore pour terrain non-plat
        Dictionary<int, GameObject[]> slopePool = new Dictionary<int, GameObject[]>();
        Dictionary<int, int> nextSlopeId = new Dictionary<int, int>(slopeFlora.Count);
        for (int i = 0; i < slopeFlora.Count; i++)
        {
            GameObject[] slopeGMs = GameObjectPool.inst.Request(slopeFloraIds[i], floraCount[i + flatFlora.Count]);
            List<GameObject> finalSlopeGMs = new List<GameObject>();
            foreach (var item in slopeGMs)
            {
                if (System.Array.IndexOf(item.GetComponent<FloraParameters>().biomes, biome) != -1) finalSlopeGMs.Add(item);
            }

            slopePool.Add(slopeFloraIds[i], finalSlopeGMs.ToArray());
            nextSlopeId.Add(slopeFloraIds[i], 0);
        }

        for (int i = 0; i < infos.Length; i++)
        {
            // Initialisation de la flore
            if (infos[i].state == GroundManager.MeshPointState.FLAT && infos[i].elementId < flatPool.Count)
            {
                if (nextFlatId[infos[i].elementId] < flatPool[infos[i].elementId].Count<GameObject>())
                {
                    Transform flora;
                    FloraParameters floraParams;
                    floraParams = flatPool[infos[i].elementId][nextFlatId[infos[i].elementId]].GetComponent<FloraParameters>();
                    flora = flatPool[infos[i].elementId][nextFlatId[infos[i].elementId]++].transform;
                    switch (floraParams.rotation)
                    {
                        case GroundManager.Rotation.DOWN:
                            flora.rotation = new Quaternion(0, Random.rotation.y, 180, 0);
                            break;
                        case GroundManager.Rotation.UP:
                            flora.rotation = new Quaternion(0, Random.rotation.y, 0, 0);
                            break;
                        default:
                            flora.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(infos[i].normal.x, infos[i].normal.y, infos[i].normal.z));
                            break;
                    }
                    flora.position = parent.position + infos[i].pos - infos[i].normal * 0.05f;
                    flora.localScale = Vector3.one * Random.Range(0.4f, 1f);
                    flora.gameObject.SetActive(true);
                    flora.transform.parent = parent;
                }
            }
            else if (infos[i].state == GroundManager.MeshPointState.SLOPE && infos[i].elementId < slopePool.Count)
            {
                if (nextSlopeId[infos[i].elementId] < slopePool[infos[i].elementId].Count<GameObject>())
                {
                    Transform flora;
                    FloraParameters floraParams;
                    floraParams = slopePool[infos[i].elementId][nextSlopeId[infos[i].elementId]].GetComponent<FloraParameters>();
                    flora = slopePool[infos[i].elementId][nextSlopeId[infos[i].elementId]++].transform;
                    Random.InitState(i);
                    switch (floraParams.rotation)
                    {
                        case GroundManager.Rotation.DOWN:
                            flora.rotation = new Quaternion(0, Random.rotation.y, 180, 0);
                            break;
                        case GroundManager.Rotation.UP:
                            flora.rotation = new Quaternion(0, Random.rotation.y, 0, 0);
                            break;
                        default:
                            flora.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(infos[i].normal.x, infos[i].normal.y, infos[i].normal.z));
                            break;
                    }
                    flora.position = parent.position + infos[i].pos - infos[i].normal * 0.05f;
                    flora.localScale = Vector3.one * Random.Range(0.4f, 1f);
                    flora.gameObject.SetActive(true);
                    flora.transform.parent = parent;
                }

            }
        }

        return new GroundManager.MeshGround(flatPool, slopePool);
    }

    /// <summary>
    /// Libère la flore d'un mesh
    /// </summary>
    public void FreeFlora(GroundManager.MeshGround meshFlora)
    {
        if (meshFlora.flatPool == null) return;

        for (int i = 0; i < flatFlora.Count; i++)
            GameObjectPool.inst.Free(flatFloraIds[i], meshFlora.flatPool[flatFloraIds[i]]);

        for (int i = 0; i < slopeFlora.Count; i++)
            GameObjectPool.inst.Free(slopeFloraIds[i], meshFlora.slopePool[slopeFloraIds[i]]);
    }

    /// <summary>
    /// Calcule les positions de la flore sur un mesh, retourne en référance les infos et le compteur de flore.
    /// Le compeur de flore permet uniquement de ne pas recompter le nombre de chaque fleur sur le mesh
    /// </summary>
    public void GetFloraData(Mesh mesh, out GroundManager.MeshPointInfo[] infos, out int[] floraCount, GroundManager.Biome biome)
    {
        int[] triangles = mesh.triangles;
        if (triangles.Length < 3)
        {
            infos = null;
            floraCount = null;
            return;
        }
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        System.Random random = new System.Random();


        int floraDenstiy = Vars.FLORA_DENSITY[(int)biome] * triangles.Length / Vars.REF_CHUNK_TRI_COUNT;

        // Génération des infos
        infos = new GroundManager.MeshPointInfo[floraDenstiy];
        floraCount = new int[flatFlora.Count + slopeFlora.Count];

        for (int i = 0; i < floraDenstiy; i++)
        {
            GroundManager.MeshPointInfo pointInfo = GroundManager.GetRandomPointOnMesh(triangles, vertices, normals, slopeAngle, random);

            if (pointInfo.state == GroundManager.MeshPointState.FLAT)
                pointInfo.elementId = flatFloraIds[Random.Range(0, flatFlora.Count)];
            else
                pointInfo.elementId = slopeFloraIds[Random.Range(0, slopeFlora.Count)];

            floraCount[pointInfo.elementId]++;
            infos[i] = pointInfo;
        }
    }


    public void GetFloraDataAsync(Mesh mesh,  Action<FloraDataResult> onComplete,  GroundManager.Biome biome)
    {
        int[] triangles = mesh.triangles;
        if (triangles.Length < 3)
        {
           return;
        }

        // Récupérer les données du mesh sur le thread principal
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        System.Random random = new System.Random();

        // Lancer un nouveau thread pour effectuer les calculs intensifs
        Thread thread = new Thread(() =>
        {
            var result = GenerateFloraData(triangles, vertices, normals, biome, random);
            onComplete.Invoke(result);
        });
        thread.Start();
    }

    private FloraDataResult GenerateFloraData(int[] triangles, Vector3[] vertices, Vector3[] normals, GroundManager.Biome biome, System.Random random)
    {
        int floraDensity = (Vars.FLORA_DENSITY[(int)biome] * triangles.Length / Vars.REF_CHUNK_TRI_COUNT);

        GroundManager.MeshPointInfo[] infos = new GroundManager.MeshPointInfo[floraDensity];
        int[] floraCount = new int[flatFlora.Count + slopeFlora.Count];

        for (int i = 0; i < floraDensity; i++)
        {
            GroundManager.MeshPointInfo pointInfo = GroundManager.GetRandomPointOnMesh(triangles, vertices, normals, slopeAngle, random);
            if (pointInfo.state == GroundManager.MeshPointState.FLAT)
                pointInfo.elementId = flatFloraIds[random.Next(0, flatFlora.Count)];
            else
                pointInfo.elementId = slopeFloraIds[random.Next(0, slopeFlora.Count)];

            lock (floraCount)
            {
                floraCount[pointInfo.elementId]++;
            }

            infos[i] = pointInfo;
        }

        return new FloraDataResult { infos = infos, floraCount = floraCount };
    }

}