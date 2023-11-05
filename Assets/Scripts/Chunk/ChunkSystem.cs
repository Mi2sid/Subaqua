using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(MeshGenerator))]
[RequireComponent(typeof(FloraManager))]
[RequireComponent(typeof(GroundFaunaManager))]
public class ChunkSystem : MonoBehaviour
{
    #region HELPER_CLASSES

    private class Chunk
    {
        public static FloraManager floraManager;
        public static GroundFaunaManager groundFaunaManager;
        public GameObject gameObject;
        public Vector3Int chunkPos;
        public Random _R = new Random();
        public GroundManager.Biome biome;

        // Stockage des données de la flore et de la faune au sol
        public GroundManager.MeshGround meshFlora;
        public GroundManager.MeshGround meshGroundFauna;
        public GroundManager.MeshPointInfo[] pointInfosFlora;
        public GroundManager.MeshPointInfo[] pointInfosGroundFauna;
        public int[] floraCount;
        public int[] groundFaunaCount;

        public Chunk(GameObject gameObject, Vector3Int chunkPos)
        {
            this.gameObject = gameObject;
            this.chunkPos = chunkPos;
        }

        public void UpdateState(Vector3Int refPos, int viewDist, int hviewDist)
        {
            if (Mathf.Abs(refPos.y - chunkPos.y) > hviewDist ||
                Vector2Int.Distance(refPos.xz(), chunkPos.xz()) >= viewDist)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                    // Si le chunk est désactivé, alors il faut libérer la flore et la faune au sol
                    floraManager.FreeFlora(meshFlora);
                    groundFaunaManager.FreeGroundFauna(meshGroundFauna);
                }
            }

            else
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    // Si le chunk est activé alors il faut placer la flore et la faune au sol
                    meshFlora = floraManager.InitializeFlora(gameObject.transform, pointInfosFlora, floraCount, biome);
                    meshGroundFauna = groundFaunaManager.InitializeGroundFauna(gameObject.transform, pointInfosGroundFauna, groundFaunaCount, biome);
                }
            }
        }
    }

    private struct ChunkGenData
    {
        public Vector3Int chunkPos;
        public MeshData meshData;

        public ChunkGenData(Vector3Int chunkPos, MeshData meshData)
        {
            this.chunkPos = chunkPos;
            this.meshData = meshData;
        }
    }

    #endregion

    [Header("General Parameters")]
    [Rename("Player")]
    public Transform target;

    [SerializeField] ChunkParameters chunkParameters;
    [SerializeField] GameObject chunkPrefab;
    [SerializeField] float updateTime;

    [Header("Player Graphical Settings")]
    [Rename("View Distance")]
    public int viewDist;

    [Rename("Pre-Generation Distance")] public int preGenDist;

    [Rename("Startup Generation Distance")]
    public int startupGenDist;

    [Space]
    [Rename("Height View Distance")]
    public int hviewDist;

    [Rename("Height Pre-Generation Distance")]
    public int hpreGenDist;

    [Rename("Height Startup Generation Distance")]
    public int hstartupGenDist;

    [Space] public int chunkUpdatePerFrame = 50;

    MeshGenerator meshGenerator;
    Dictionary<Vector3Int, Chunk> chunks;
    Stack<Vector3Int> chunkUpdateQueue;
    Queue<ChunkGenData> chunkGenQueue;
    Vector3Int lastChunkPos;
    Vector3 chunkSpacing;
    int maxYChunk;

    FloraManager floraManager;
    GroundFaunaManager groundFaunaManager;
    public static ChunkSystem inst;

    public List<GameObject> childs;

    private void Awake()
    {
        inst = this;
        if (target == null)
        {
            target = GameObject.FindWithTag("Player").transform;
            if (target == null)
                Debug.LogError("'Player' target is null, please assign it in 'ChunkSystem'");
        }

        meshGenerator = GetComponent<MeshGenerator>();
        floraManager = GetComponent<FloraManager>();
        Chunk.floraManager = floraManager;
        groundFaunaManager = GetComponent<GroundFaunaManager>();
        Chunk.groundFaunaManager = groundFaunaManager;

        for (int i = transform.childCount - 1; i >= 0; i--) // Destruction de tout les objets fils
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        chunkGenQueue = new Queue<ChunkGenData>();
        chunkUpdateQueue = new Stack<Vector3Int>();
        chunks = new Dictionary<Vector3Int, Chunk>();
    }



    public GroundManager.Biome getRandomBiome(System.Random _R)
    {
        var v = Enum.GetValues(typeof(GroundManager.Biome));
        return (GroundManager.Biome)v.GetValue(_R.Next(v.Length));
    }

    private void Start()
    {
        // Calcul de l'espacage des chunks en fonction des paramètres de chunk
        Vector3 voxelWorldSize = chunkParameters.worldSize.Div(chunkParameters.voxelDensity);

        chunkSpacing = chunkParameters.worldSize - 2 * voxelWorldSize;

        Vector3Int currentChunkPos = GetCurrentChunkPos();
        lastChunkPos = currentChunkPos;

        maxYChunk = meshGenerator.GetMaxHeight() / (int)chunkParameters.worldSize.y;

        // Pré-génération des chunks
        for (int yOffset = -hstartupGenDist; yOffset <= hstartupGenDist; yOffset++)
        {
            for (int zOffset = -startupGenDist; zOffset <= startupGenDist; zOffset++)
            {
                for (int xOffset = -startupGenDist; xOffset <= startupGenDist; xOffset++)
                {
                    Vector3Int chunkPos = new Vector3Int(currentChunkPos.x + xOffset, currentChunkPos.y + yOffset,
                        currentChunkPos.z + zOffset);
                    if (chunkPos.y > maxYChunk) continue;
                    Vector3 worldPos = new Vector3(chunkPos.x * chunkSpacing.x, chunkPos.y * chunkSpacing.y,
                        chunkPos.z * chunkSpacing.z);
                    Chunk chunk = GenerateChunk(GetChunkGenData(chunkPos, worldPos));
                    chunks.Add(chunkPos, chunk);

                    chunk.UpdateState(currentChunkPos, viewDist, hviewDist);
                }
            }
        }

        chunks[currentChunkPos].gameObject.GetComponent<MeshCollider>().sharedMesh =
            chunks[currentChunkPos].gameObject.GetComponent<MeshFilter>().mesh;

        StartCoroutine(UpdateChunks());
        StartCoroutine(GenerateChunks());
        StartCoroutine(UpdateChunkStates());
    }

    /// <summary>
    /// Met à jour tout les chunks (génère si nécessaire ou alors met à jour leur état)
    /// </summary>
    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            Vector3Int currentChunk = GetCurrentChunkPos();
            if (currentChunk != lastChunkPos)
            {
                chunks[currentChunk].gameObject.GetComponent<MeshCollider>().sharedMesh =
                    chunks[currentChunk].gameObject.GetComponent<MeshFilter>().mesh;
                for (int yOffset = -hpreGenDist; yOffset <= hpreGenDist; yOffset++)
                {
                    for (int zOffset = -preGenDist; zOffset <= preGenDist; zOffset++)
                    {
                        for (int xOffset = -preGenDist; xOffset <= preGenDist; xOffset++)
                        {
                            Vector3Int chunkPos = new Vector3Int(currentChunk.x + xOffset, currentChunk.y + yOffset,
                                currentChunk.z + zOffset);
                            if (chunkPos.y > maxYChunk) continue;
                            if (chunks.ContainsKey(chunkPos)) // Si le chunk est déjà généré, màj de l'état
                            {
                                if (chunks[chunkPos] != null)
                                    chunkUpdateQueue.Push(chunkPos);
                            }
                            else // Sinon création du chunk
                            {
                                Vector3 worldPos = new Vector3(chunkPos.x * chunkSpacing.x, chunkPos.y * chunkSpacing.y,
                                    chunkPos.z * chunkSpacing.z);
                                chunks.Add(chunkPos, null);
                                chunkGenQueue.Enqueue(GetChunkGenData(chunkPos, worldPos));
                            }
                        }
                    }
                }

                lastChunkPos = currentChunk;
            }

            yield return new WaitForSeconds(updateTime * 2f);
        }
    }

    /// <summary>
    /// Met à jour les états des chunks de manière étalée
    /// </summary>
    private IEnumerator UpdateChunkStates()
    {
        while (true)
        {
            int i = 0;
            Vector3Int currentChunk = GetCurrentChunkPos();
            while (chunkUpdateQueue.Count > 0)
            {
                Vector3Int chunkPos = chunkUpdateQueue.Pop();
                chunks[chunkPos].UpdateState(currentChunk, viewDist, hviewDist);

                if (i++ % chunkUpdatePerFrame == 0)
                    yield return null;
            }

            yield return new WaitForSeconds(updateTime);
        }
    }

    /// <summary>
    /// Calcule la position chunk courrante du joueur et la renvoie.
    /// </summary>
    public Vector3Int GetCurrentChunkPos()
    {
        return (target.position - Vector3.up * (target.position.y < 0 ? chunkSpacing.y : 0)).Div(chunkSpacing).AsInts();
    }

    /// <summary>
    /// Crée et renvoie la struct 'ChunkGenData' rempli des paramêtres en entrée.
    /// </summary>
    private ChunkGenData GetChunkGenData(Vector3Int chunkPos, Vector3 worldPos)
    {
        return new ChunkGenData(chunkPos,
            new MeshData(worldPos, chunkParameters.worldSize, chunkParameters.voxelDensity, chunkPos.y));
    }

    /// <summary>
    /// Coroutine permettant de générer les chunks de la pile à la suite.
    /// </summary>
    private IEnumerator GenerateChunks()
    {
        while (true)
        {
            while (chunkGenQueue.Count > 0)
                yield return GenerateChunkAsync(chunkGenQueue.Dequeue());

            yield return new WaitForSeconds(updateTime);
        }
    }

    /// <summary>
    /// Génère immédiatement le chunk en fonction de la 'ChunkGenData' en entrée
    /// </summary>
    private Chunk GenerateChunk(ChunkGenData data)
    {
        GameObject chunkGO = Instantiate(chunkPrefab, data.meshData.origin, Quaternion.identity, transform);
#if (UNITY_EDITOR)
        chunkGO.name = data.chunkPos.ToString();
#endif
        Mesh mesh;
        mesh = meshGenerator.GenerateMesh(data.meshData);

        chunkGO.GetComponent<MeshFilter>().mesh = mesh;
        chunkGO.SetActive(false);
        Chunk chunk = new Chunk(chunkGO, data.chunkPos);

        chunk.biome = getRandomBiome(chunk._R);

        floraManager.GetFloraData(mesh, out chunk.pointInfosFlora, out chunk.floraCount, chunk.biome);
        groundFaunaManager.GetGroundFaunaData(mesh, out chunk.pointInfosGroundFauna, out chunk.groundFaunaCount);

        chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
        childs.Add(chunkGO);

        return chunk;
    }

    /// <summary>
    /// Génère de façon asynchrone le chunk en fonction de la 'ChunkGenData' en entrée
    /// </summary>
    private IEnumerator GenerateChunkAsync(ChunkGenData data)
    {
        GameObject chunkGO = Instantiate(chunkPrefab, data.meshData.origin, Quaternion.identity, transform);
#if (UNITY_EDITOR)
        chunkGO.name = data.chunkPos.ToString();
#endif
        Mesh mesh = null;
        yield return meshGenerator.GenerateMesh(data.meshData, (m) => mesh = m);

        chunkGO.GetComponent<MeshFilter>().mesh = mesh;
        Chunk chunk = new Chunk(chunkGO, data.chunkPos);

        chunk.biome = getRandomBiome(chunk._R);

        chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;


        floraManager.GetFloraData(mesh, out chunk.pointInfosFlora, out chunk.floraCount, chunk.biome);
        groundFaunaManager.GetGroundFaunaData(mesh, out chunk.pointInfosGroundFauna, out chunk.groundFaunaCount);

        chunkGO.SetActive(false);
        chunks[data.chunkPos] = chunk;
    }

    #region DEBUG_FUNCTIONS

    public int GetChunksSize()
    {
        return chunks.Count;
    }

    public int GetGenQueueSize()
    {
        return chunkGenQueue.Count;
    }

    public void Update()
    {
        foreach (GameObject child in childs)
        {
            if (child.activeInHierarchy)
                child.GetComponent<MeshCollider>().enabled = true;
            else child.GetComponent<MeshCollider>().enabled = false;
        }
    }

    #endregion
}