using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Threading;
public class GroundFaunaManager : MonoBehaviour
{
    public List<GameObject> groundFauna;

    private int[] groundFaunaIds;

    private int firstId = -1;


    public struct GroundFaunaDataResult
    {
        public GroundManager.MeshPointInfo[] infos;
        public int[] groundFaunaCount;
    }


    private void Awake()
    {
        groundFaunaIds = new int[groundFauna.Count];

        // Cr�ation des reserves d'objets pour la faune
        ChunkSystem chunkSystem = GetComponent<ChunkSystem>();
        int maxObjects = ((chunkSystem.viewDist * 2 + 1) + (chunkSystem.hviewDist * 2 + 1)) * Vars.GROUNDFAUNA_DENSITY;

        for (int i = 0; i < groundFauna.Count; i++)
        {
            groundFaunaIds[i] = GameObjectPool.inst.CreateNewPool(groundFauna[i], maxObjects / groundFauna.Count);
            if (firstId == -1) firstId = groundFaunaIds[i];
        }
    }

    /// <summary>
    /// Initialise la faune sur le mesh selon les infos contenues dans 'infos'.
    /// Il faut �galement sp�cifier le parent de l'objet (chunk) et donner le 'fauneCount'
    /// </summary>
    public GroundManager.MeshGround InitializeGroundFauna(Transform parent, GroundManager.MeshPointInfo[] infos, int[] groundFaunaCount, GroundManager.Biome biome)
    {
        if (infos == null)
        {
            return new GroundManager.MeshGround(null, null);
        }

        // R�cup�ration des r�serves de faune
        Dictionary<int, GameObject[]> pool = new Dictionary<int, GameObject[]>(groundFauna.Count);
        Dictionary<int, int> nextId = new Dictionary<int, int>(groundFauna.Count);
        for (int i = 0; i < groundFauna.Count; i++)
        {
            GameObject[] GMs = GameObjectPool.inst.Request(groundFaunaIds[i], groundFaunaCount[i]);
            List<GameObject> finalGMs = new List<GameObject>();
            foreach (var item in GMs)
            {
                if (System.Array.IndexOf(item.GetComponent<GroundFaunaParameters>().biomes, biome) != -1) finalGMs.Add(item);
            }

            pool.Add(groundFaunaIds[i], finalGMs.ToArray());
            nextId.Add(groundFaunaIds[i], 0);
        }

        for (int i = 0; i < infos.Length; i++)
        {
            // Initialisation de la faune
            if (infos[i].elementId < pool.Count)
            {
                if (nextId[infos[i].elementId + firstId] < pool[infos[i].elementId + firstId].Count<GameObject>())
                {
                    Transform groundFauna;
                    GroundFaunaParameters groundFaunaParams;
                    groundFaunaParams = pool[infos[i].elementId + firstId][nextId[infos[i].elementId + firstId]].GetComponent<GroundFaunaParameters>();
                    groundFauna = pool[infos[i].elementId + firstId][nextId[infos[i].elementId + firstId]++].transform;
                    Random.InitState(i);
                    switch (groundFaunaParams.rotation)
                    {
                        case GroundManager.Rotation.DOWN:
                            groundFauna.rotation = new Quaternion(0, Random.rotation.y, 180, 0);
                            break;
                        case GroundManager.Rotation.UP:
                            groundFauna.rotation = new Quaternion(0, Random.rotation.y, 0, 0);
                            break;
                        default:
                            groundFauna.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(infos[i].normal.x, infos[i].normal.y, infos[i].normal.z));
                            break;
                    }
                    groundFauna.position = parent.position + infos[i].pos;
                    groundFauna.localScale = Vector3.one * Random.Range(0.4f, 1f);
                    groundFauna.gameObject.SetActive(true);
                    groundFauna.transform.parent = parent;
                }
            }

        }

        return new GroundManager.MeshGround(pool);
    }

    /// <summary>
    /// Lib�re la faune d'un mesh
    /// </summary>
    public void FreeGroundFauna(GroundManager.MeshGround meshGroundFauna)
    {
        if (meshGroundFauna.pool == null) return;

        for (int i = 0; i < groundFauna.Count; i++)
            GameObjectPool.inst.Free(groundFaunaIds[i], meshGroundFauna.pool[groundFaunaIds[i]]);
    }

    /// <summary>
    /// Calcule les positions de la faune sur un mesh, retourne en r�f�rance les infos et le compteur de faune.
    /// Le compteur de faune permet uniquement de ne pas recompter le nombre de chaque faune sur le mesh
    /// </summary>
    public void GetGroundFaunaData(Mesh mesh, out GroundManager.MeshPointInfo[] infos, out int[] groundFaunaCount)
    {
        int[] triangles = mesh.triangles;
        if (triangles.Length < 3)
        {
            infos = null;
            groundFaunaCount = null;
            return;
        }
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        System.Random random = new System.Random();

        int groundFaunaDenstiy = Vars.GROUNDFAUNA_DENSITY * (triangles.Length / 3) / Vars.REF_CHUNK_TRI_COUNT;

        // G�n�ration des infos
        infos = new GroundManager.MeshPointInfo[groundFaunaDenstiy];
        groundFaunaCount = new int[groundFauna.Count];

        for (int i = 0; i < groundFaunaDenstiy; i++)
        {
            GroundManager.MeshPointInfo pointInfo = GroundManager.GetRandomPointOnMesh(triangles, vertices, normals, 360, random);

            pointInfo.elementId = groundFaunaIds[Random.Range(0, groundFauna.Count)] - firstId;

            groundFaunaCount[pointInfo.elementId]++;
            infos[i] = pointInfo;
        }
    }


    public void GetGroundFaunaDataAsync(Mesh mesh,  Action<GroundFaunaDataResult> onComplete)
    {
        int[] triangles = mesh.triangles;
        if (triangles.Length < 3)
        {
            return;
        }
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        System.Random random = new System.Random();

       // Lancer un nouveau thread pour effectuer les calculs intensifs
        Thread thread = new Thread(() =>
        {
            var result = GenerateGroundFaunaData(triangles, vertices, normals, random);
            onComplete.Invoke(result);
        });
        thread.Start();


    }


    private GroundFaunaDataResult GenerateGroundFaunaData(int[] triangles, Vector3[] vertices, Vector3[] normals, System.Random random)
    {
        int groundFaunaDenstiy = Vars.GROUNDFAUNA_DENSITY * (triangles.Length / 3) / Vars.REF_CHUNK_TRI_COUNT;

        // G�n�ration des infos
        GroundManager.MeshPointInfo[] infos = new GroundManager.MeshPointInfo[groundFaunaDenstiy];
        int[] groundFaunaCount = new int[groundFauna.Count];

        for (int i = 0; i < groundFaunaDenstiy; i++)
        {
            GroundManager.MeshPointInfo pointInfo = GroundManager.GetRandomPointOnMesh(triangles, vertices, normals, 360, random);

            pointInfo.elementId = groundFaunaIds[random.Next(0, groundFauna.Count)] - firstId;


            groundFaunaCount[pointInfo.elementId]++;
            infos[i] = pointInfo;
        }

        return new GroundFaunaDataResult { infos = infos, groundFaunaCount = groundFaunaCount };
    }

}