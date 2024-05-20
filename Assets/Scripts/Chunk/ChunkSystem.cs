using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = System.Random;
using Unity.Jobs;
using Unity.Collections;

[RequireComponent(typeof(MeshGenerator))]
[RequireComponent(typeof(FloraManager))]
[RequireComponent(typeof(GroundFaunaManager))]
public class ChunkSystem : MonoBehaviour
{
    #region HELPER_CLASSES

    public class Chunk
    {
        public static FloraManager floraManager;
        public static GroundFaunaManager groundFaunaManager;
        public GameObject gameObject;
        public Vector3Int chunkPos;
        public Vector3 chunkWorldPos;
        public Mesh mesh;
        public bool meshSimple = true;
        public bool initFloraFauna = false;
        public bool initMeshComplexe = false;
        public Random _R = new Random();
        public GroundManager.Biome biome;

        // Stockage des donn�es de la flore et de la faune au sol
        public GroundManager.MeshGround meshFlora;
        public GroundManager.MeshGround meshGroundFauna;
        public GroundManager.MeshPointInfo[] pointInfosFlora;
        public GroundManager.MeshPointInfo[] pointInfosGroundFauna;
        public int[] floraCount;
        public int[] groundFaunaCount;


        public Chunk(GameObject gameObject, Vector3Int chunkPos, Mesh mesh, Vector3 chunkSpacing)
        {
            this.gameObject = gameObject;
            this.chunkPos = chunkPos;
            this.mesh = mesh;
            this.chunkWorldPos = new Vector3(chunkPos.x * chunkSpacing.x, chunkPos.y * chunkSpacing.y, chunkPos.z * chunkSpacing.z);
        }

        public void UpdateMesh(bool complexification)
        {
            if (this.mesh != null){
                if (complexification && this.meshSimple || !complexification && !this.meshSimple){
                    // Maj du mesh pour le niveau de detail + garde en memoire l'autre version
                    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                    Mesh meshTemp = meshFilter.mesh;
                    meshFilter.mesh = this.mesh;
                    this.mesh = meshTemp;
                    this.meshSimple = !this.meshSimple;
                }
            }
        }

        public void UpdateState(Vector3Int refPos, int viewDist, int hviewDist, int distanceLOD)
        {
            float distance = Vector2Int.Distance(refPos.xz(), chunkPos.xz());
            float distanceX = Mathf.Abs(refPos.x - chunkPos.x);
            float distanceZ = Mathf.Abs(refPos.z - chunkPos.z);
            float distanceH = Mathf.Abs(refPos.y - chunkPos.y);

            if (distanceZ <= distanceLOD && distanceX <= distanceLOD &&  distanceH <= distanceLOD-1 && this.meshSimple)
            {
                //initialization de la flore que lorsque on est proche
                if (initFloraFauna == false){
                    initFloraFauna = true;
                    meshFlora = floraManager.InitializeFlora(gameObject.transform, pointInfosFlora, floraCount, biome);
                    meshGroundFauna = groundFaunaManager.InitializeGroundFauna(gameObject.transform, pointInfosGroundFauna, groundFaunaCount, biome);
                }
                //complexification du chunk
                this.UpdateMesh(true);
            } else {
                if (!this.meshSimple && refPos != this.chunkPos){
                    //simplification du chunk
                    this.UpdateMesh(false);
                }
            }


            if (distance > viewDist || distanceH > hviewDist)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                    // Si le chunk est d�sactiv�, alors il faut lib�rer la flore et la faune au sol
                    floraManager.FreeFlora(meshFlora);
                    groundFaunaManager.FreeGroundFauna(meshGroundFauna);
                }
            }
            else
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
            }
        }
    }

    public struct ChunkGenData
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

    [Rename("Level of details Distance")]
    public int distanceLOD;

    [Space] public int chunkUpdatePerFrame = 50;

    MeshGenerator meshGenerator;
    Dictionary<Vector3Int, Chunk> chunks;
    Chunk[] currentChunks;
    Stack<Vector3Int> chunkUpdateQueue;
    Queue<ChunkGenData> chunkGenQueue;
    Vector3Int lastChunkPos;
    Vector3 chunkSpacing;
    int maxYChunk;

    FloraManager floraManager;
    GroundFaunaManager groundFaunaManager;
    public static ChunkSystem inst;

    public List<GameObject> childs;

    Thread updateChunksThread;

    Vector3Int currentChunkPos;

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
        int numberOfChunks = (viewDist*2+1) * (viewDist*2+1) * (hviewDist * 2 + 1);  
        currentChunks = new Chunk[numberOfChunks];
        
    }



    public static GroundManager.Biome getRandomBiome(System.Random _R)
    {
        var v = Enum.GetValues(typeof(GroundManager.Biome));
        return (GroundManager.Biome)v.GetValue(_R.Next(v.Length));
    }

    private void Start()
    {
        // Calcul de l'espacage des chunks en fonction des param�tres de chunk
        Vector3 voxelWorldSize = chunkParameters.worldSize.Div(chunkParameters.voxelDensitySimple);

        chunkSpacing = chunkParameters.worldSize - 2 * voxelWorldSize;

        currentChunkPos = GetCurrentChunkPos();
        lastChunkPos = currentChunkPos;
        maxYChunk = meshGenerator.GetMaxHeight() / (int)chunkParameters.worldSize.y;

        int i = 0;

        // Pr�-g�n�ration des chunks
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
                    Chunk chunk = GenerateChunk(GetChunkGenData(chunkPos, worldPos, chunkParameters.voxelDensity), GetChunkGenData(chunkPos, worldPos, chunkParameters.voxelDensitySimple));
                    chunks.Add(chunkPos, chunk);
                    chunk.UpdateState(currentChunkPos, viewDist, hviewDist, distanceLOD);
                    
                }
            }
        }

        chunks[currentChunkPos].gameObject.GetComponent<MeshCollider>().sharedMesh =
            chunks[currentChunkPos].gameObject.GetComponent<MeshFilter>().mesh;

        StartCoroutine(UpdateChunks());
        StartCoroutine(GenerateChunks());
        StartCoroutine(UpdateChunkStates());
    }
            

    private void UpdateChunksFrustum(){
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        foreach (Chunk chunk in currentChunks){
            if (chunk != null){
                Vector3 chunkCenter = chunk.chunkWorldPos + chunkSpacing / 2f;
                Bounds chunkBounds = new Bounds(chunkCenter, chunkSpacing);
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, chunkBounds)){
                    chunk.gameObject.SetActive(true); 
                } else {
                    chunk.gameObject.SetActive(false); 
                }
            }
        }
    }


    private IEnumerator UpdateChunks()
    {
        while (true)
        {
            if (currentChunkPos != lastChunkPos){
                //chunks[currentChunkPos].gameObject.GetComponent<MeshCollider>().sharedMesh =
                    //   chunks[currentChunkPos].gameObject.GetComponent<MeshFilter>().mesh; //inutile ??

                int i = 0;
                for (int yOffset = -hpreGenDist; yOffset <= hpreGenDist; yOffset++)
                {
                    for (int zOffset = -preGenDist; zOffset <= preGenDist; zOffset++)
                    {
                        for (int xOffset = -preGenDist; xOffset <= preGenDist; xOffset++)
                        {
                            Vector3Int chunkPos = new Vector3Int(currentChunkPos.x + xOffset, currentChunkPos.y + yOffset,
                                currentChunkPos.z + zOffset);
                            if (chunkPos.y > maxYChunk) continue;
                            if (chunks.ContainsKey(chunkPos)) // Si le chunk est d�j� g�n�r�, m�j de l'�tat
                            {
                                float distanceX = Mathf.Abs(currentChunkPos.x - chunkPos.x);
                                float distanceZ = Mathf.Abs(currentChunkPos.z - chunkPos.z);
                                float distanceH = Mathf.Abs(currentChunkPos.y - chunkPos.y);


                                if (distanceX <= viewDist && distanceZ <= viewDist && distanceH <= hviewDist){
                                    currentChunks[i] = chunks[chunkPos];
                                    i++;
                                }

                                if (chunks[chunkPos] != null){

                                    //si le chunk est dans la zone de creation de chunk complexe on crée sa version complexe
                                    if (distanceX <= distanceLOD && distanceZ <= distanceLOD && distanceH <= distanceLOD && chunks[chunkPos].initMeshComplexe == false )
                                    { 
                                        chunks[chunkPos].initMeshComplexe = true;
                                        Vector3 worldPos = new Vector3(chunkPos.x * chunkSpacing.x, chunkPos.y * chunkSpacing.y,
                                            chunkPos.z * chunkSpacing.z);
                                        chunkGenQueue.Enqueue(GetChunkGenData(chunkPos, worldPos, chunkParameters.voxelDensity));
                                    }
                                    //update tous le temps
                                    chunkUpdateQueue.Push(chunkPos);
                                }
                            }
                            else // Sinon creation du chunk (VERSION SIMPLE)
                            {  
                                Vector3 worldPos = new Vector3(chunkPos.x * chunkSpacing.x, chunkPos.y * chunkSpacing.y,
                                        chunkPos.z * chunkSpacing.z);
                                chunks.Add(chunkPos, null);
                                chunkGenQueue.Enqueue(GetChunkGenData(chunkPos, worldPos, chunkParameters.voxelDensitySimple));   
                            }
                            
                        }
                    }
                }
                lastChunkPos = currentChunkPos;
            }   
            yield return new WaitForSeconds(updateTime * 2f);
        }
    }


    /// <summary>
    /// Met � jour les �tats des chunks de mani�re �tal�e
    /// </summary>
    private IEnumerator UpdateChunkStates()
    {
        while (true)
        {
            int i = 0;
            while (chunkUpdateQueue.Count > 0)
            {
                Vector3Int chunkPos = chunkUpdateQueue.Pop();
                chunks[chunkPos].UpdateState(currentChunkPos, viewDist, hviewDist, distanceLOD);

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
    /// Cr�e et renvoie la struct 'ChunkGenData' rempli des param�tres en entr�e.
    /// </summary>
    private ChunkGenData GetChunkGenData(Vector3Int chunkPos, Vector3 worldPos, Vector3Int voxelDensity)
    {
        return new ChunkGenData(chunkPos,
            new MeshData(worldPos, chunkParameters.worldSize, voxelDensity, chunkPos.y));
    }

    /// <summary>
    /// Coroutine permettant de g�n�rer les chunks de la pile � la suite.
    /// </summary>
    private IEnumerator GenerateChunks()
    {
        while (true)
        {
            while (chunkGenQueue.Count > 0){
                yield return GenerateChunkAsync(chunkGenQueue.Dequeue());
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    /// <summary>
    /// G�n�re imm�diatement le chunk en fonction de la 'ChunkGenData' en entr�e
    /// </summary>
    private Chunk GenerateChunk(ChunkGenData data, ChunkGenData dataSimple)
    {
        GameObject chunkGO = Instantiate(chunkPrefab, data.meshData.origin, Quaternion.identity, transform);
        #if (UNITY_EDITOR)
                chunkGO.name = data.chunkPos.ToString();
        #endif
        Mesh mesh, meshSimple;
        mesh = meshGenerator.GenerateMesh(data.meshData);
        meshSimple = meshGenerator.GenerateMesh(dataSimple.meshData);

        chunkGO.GetComponent<MeshFilter>().mesh = meshSimple;
        chunkGO.GetComponent<MeshCollider>().enabled = false;
        chunkGO.SetActive(false);
        Chunk chunk = new Chunk(chunkGO, data.chunkPos, mesh, chunkSpacing);

        chunk.biome = getRandomBiome(chunk._R);

        floraManager.GetFloraData(meshSimple, out chunk.pointInfosFlora, out chunk.floraCount, chunk.biome);
        groundFaunaManager.GetGroundFaunaData(meshSimple, out chunk.pointInfosGroundFauna, out chunk.groundFaunaCount);

        chunkGO.GetComponent<MeshCollider>().sharedMesh = meshSimple;
        
        childs.Add(chunkGO);

        return chunk;
    }

    /// <summary>
    /// G�n�re de fa�on asynchrone le chunk en fonction de la 'ChunkGenData' en entr�e
    /// </summary>
    private IEnumerator GenerateChunkAsync(ChunkGenData data)
    {
        if (data.meshData.voxelDensity == chunkParameters.voxelDensitySimple) //on genere le mesh simple
        {
            GameObject chunkGO = Instantiate(chunkPrefab, data.meshData.origin, Quaternion.identity, transform);
            #if (UNITY_EDITOR)
                    chunkGO.name = data.chunkPos.ToString();
            #endif
            Mesh meshSimple = null;
            yield return meshGenerator.GenerateMesh(data.meshData, (m) => meshSimple = m);

            chunkGO.GetComponent<MeshFilter>().mesh = meshSimple;
            Chunk chunk = new Chunk(chunkGO, data.chunkPos, null, chunkSpacing);

            chunk.biome = getRandomBiome(chunk._R);

            chunkGO.GetComponent<MeshCollider>().sharedMesh = meshSimple;
            chunkGO.GetComponent<MeshCollider>().enabled = false;


            //floraManager.GetFloraData(meshSimple, out chunk.pointInfosFlora, out chunk.floraCount, chunk.biome);
            //groundFaunaManager.GetGroundFaunaData(meshSimple, out chunk.pointInfosGroundFauna, out chunk.groundFaunaCount);

            // Appeler la fonction GetFloraData
            floraManager.GetFloraDataAsync(meshSimple, (result) =>
            {
                // Callback pour gérer les résultats
                chunk.pointInfosFlora = result.infos;
                chunk.floraCount = result.floraCount;
            }, chunk.biome);

            // Appeler la fonction GetFloraData
            groundFaunaManager.GetGroundFaunaDataAsync(meshSimple, (result) =>
            {
                // Callback pour gérer les résultats
                chunk.pointInfosGroundFauna = result.infos;
                chunk.groundFaunaCount = result.groundFaunaCount;
            });

            chunkGO.SetActive(false);
            chunks[data.chunkPos] = chunk;
        } else
        {
            //on genere la version compliqué du chunk
            Chunk chunk = chunks[data.chunkPos]; 
            if (chunk.mesh == null){;
                Mesh mesh = null;
                yield return meshGenerator.GenerateMesh(data.meshData, (m) => mesh = m);
                chunk.mesh = mesh;
                chunk.meshSimple = false;
                chunk.gameObject.GetComponent<MeshFilter>().mesh = mesh;

            }

        }
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
            if (!child.activeInHierarchy)
                child.GetComponent<MeshCollider>().enabled = false;
        }
        currentChunkPos = GetCurrentChunkPos();
        chunks[currentChunkPos].gameObject.GetComponent<MeshCollider>().enabled = true;
        UpdateChunksFrustum();        
    }

    #endregion
}