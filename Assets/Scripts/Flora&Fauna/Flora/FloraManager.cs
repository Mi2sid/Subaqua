using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class FloraManager : MonoBehaviour
{
    public List<GameObject> slopeFlora;
    public List<GameObject> flatFlora;
    public float slopeAngle = 50f;

    private int[] slopeFloraIds;
    private int[] flatFloraIds;

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
        Camera mainCamera = Camera.main;
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


                    /*Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
                    if (IsObjectVisible(frustumPlanes, flora.position))
                    {
                        flora.gameObject.SetActive(true); // Si visible, activer l'objet
                    }
                    else 
                    {
                        flora.gameObject.SetActive(false);
                    }*/

                    flora.gameObject.SetActive(true);           //
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

                   /* Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
                    if (IsObjectVisible(frustumPlanes, flora.position))
                    {
                        flora.gameObject.SetActive(true); // Si visible, activer l'objet
                    }
                    else
                    {
                        flora.gameObject.SetActive(false);
                    }*/
                    flora.gameObject.SetActive(true);           //
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

        int floraDenstiy = Vars.FLORA_DENSITY[(int)biome] * (triangles.Length / 3) / Vars.REF_CHUNK_TRI_COUNT;

        // Génération des infos
        infos = new GroundManager.MeshPointInfo[floraDenstiy];
        floraCount = new int[flatFlora.Count + slopeFlora.Count];

        for (int i = 0; i < floraDenstiy; i++)
        {
            GroundManager.MeshPointInfo pointInfo = GroundManager.GetRandomPointOnMesh(triangles, vertices, normals, slopeAngle);

            if (pointInfo.state == GroundManager.MeshPointState.FLAT)
                pointInfo.elementId = flatFloraIds[Random.Range(0, flatFlora.Count)];
            else
                pointInfo.elementId = slopeFloraIds[Random.Range(0, slopeFlora.Count)];

            floraCount[pointInfo.elementId]++;
            infos[i] = pointInfo;
        }
    }

    public void IsVisible() 
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null) 
        {
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            foreach (GameObject floraObject in slopeFlora)
            {
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(floraObject.transform.position);
                if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z >= 0)
                {
                    floraObject.SetActive(false);
                    UnityEngine.Debug.Log("GPU 1: ");
                }
                else
                {
                    floraObject.SetActive(false); // Sinon, désactiver l'objet
                }
            }



            foreach (GameObject floraObject in flatFlora)
            {
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(floraObject.transform.position);
                if (viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z >= 0)
                {
                    floraObject.SetActive(false);
                }
                else
                {
                    floraObject.SetActive(false); // Sinon, désactiver l'objet
                }
            }
        }
    }



    bool IsObjectVisible(Plane[] frustumPlanes, Vector3 position)
    {
        foreach (Plane plane in frustumPlanes)
        {
            if (plane.GetDistanceToPoint(position) < 0)
            {
                return false; // Si la position est derrière l'un des plans du frustrum, elle est invisible
            }
        }
        return true; // Sinon, la position est visible
    }


}