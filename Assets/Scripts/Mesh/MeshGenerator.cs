using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class MeshData
{
    public Vector3 origin;
    public Vector3 size;
    public Vector3Int voxelDensity;
    public int chunkY;

    public MeshData(Vector3 origin, Vector3 size, Vector3Int voxelDensity, int chunkY)
    {
        this.origin = origin;
        this.size = size;
        this.voxelDensity = voxelDensity;
        this.chunkY = chunkY;
    }

    public Vector3Int GetBlockDim()
    {
        return new Vector3Int(
            voxelDensity.x / Vars.NOISE_THREAD_SIZE,
            voxelDensity.y,
            voxelDensity.z / Vars.NOISE_THREAD_SIZE
        );
    }

    public int GetVoxelNumber()
    {
        return voxelDensity.x * voxelDensity.y * voxelDensity.z;
    }
}

public class MeshGenerator : MonoBehaviour
{
    #region HELPER_STRUCTS

    private struct VertexData
    {
        public Vector3 position;
        public Vector3 normal;
    }

    private struct CSData
    {
        public int[] blockDim;
        public int voxelNumber;
        public int offsetZ;
        public int offsetY;
        public int surface;

        public CSData(MeshData data)
        {
            blockDim = data.GetBlockDim().ToArray();
            voxelNumber = data.GetVoxelNumber();
            offsetZ = Vars.NOISE_THREAD_SIZE * blockDim[0];
            offsetY = Vars.NOISE_THREAD_SIZE * blockDim[0] * offsetZ;

            surface = blockDim[0] * blockDim[2] * Vars.NOISE_THREAD_SIZE * Vars.NOISE_THREAD_SIZE;
        }
    }

    #endregion

    [SerializeField] NoiseParameters noiseParameters;
    [Space] [SerializeField, Range(0, 1)] float floorValue;
    [SerializeField] private int maxHeight = 20;
    [SerializeField] private int maxHeightSmoothing = 20;

    [Header("Compute Shaders")] [SerializeField]
    ComputeShader voxelsGeneratorCS;

    [SerializeField] ComputeShader computeNormalsCS;
    [SerializeField] private ComputeShader marchingCubesCS;
    [SerializeField] float asyncLatency = 0.1f;

    ComputeBuffer heightBuffer;
    ComputeBuffer voxelsBuffer;
    ComputeBuffer voxelNormalsBuffer;
    ComputeBuffer verticesBuffer;
    ComputeBuffer verticesCountBuffer;

    /// <summary>
    /// Génère de manière asynchrone le mesh définit par 'MeshData' en paramètre
    /// puis appelle 'handle' avec le mesh crée comme paramètre
    /// </summary>
    public IEnumerator GenerateMesh(MeshData meshData, Action<Mesh> handle)
    {
        CSData csData = new CSData(meshData);

        CallGenerateVoxels(csData, meshData);
        yield return new WaitForSeconds(asyncLatency);
        CallComputeNormals(csData);
        yield return new WaitForSeconds(asyncLatency);
        CallMarchingCubes(csData);
        yield return new WaitForSeconds(asyncLatency * 2f);

        int numVertices = GetVerticesCount();
        if (numVertices != 0)
        {
            // Récupération des sommets
            VertexData[] verticesData = null;

            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(verticesBuffer, numVertices * sizeof(uint) * 3 * 2,
                0, (s) => { verticesData = s.GetData<VertexData>().ToArray(); });

            yield return new WaitWhile(() => !request.done);
            yield return null;

            DisposeBuffers();

            Mesh mesh = new Mesh();
            if (numVertices >= 65535) mesh.indexFormat = IndexFormat.UInt32;

            Vector3[] mesh_normals = null;
            int[] mesh_triangles = null;
            Vector3[] mesh_vertices = null;

            Thread thread = new Thread(() =>
            {
                // Création des tableau pour le mesh
                ParseMesh(verticesData, out mesh_vertices, out mesh_triangles, out mesh_normals);
            });

            thread.Start();
            yield return new WaitForSeconds(asyncLatency);
            yield return new WaitWhile(() => thread.IsAlive);

            mesh.bounds = new Bounds(meshData.origin, meshData.size);
            mesh.vertices = mesh_vertices;
            yield return null;
            mesh.normals = mesh_normals;
            yield return null;
            mesh.triangles = mesh_triangles;
            yield return null;

            List<Vector3> newVertices = new List<Vector3>(mesh.vertices);
            for (int i = 0; i < newVertices.Count; i++)
            {
                newVertices[i] = transform.TransformPoint(newVertices[i]);
            }
            mesh.SetUVs(0, newVertices);
            yield return null;

            handle(mesh);
        }
        else
        {
            DisposeBuffers();
            handle(new Mesh());
        }
        

        
    }

    /// <summary>
    /// Génère et retourne directement le mesh définit par 'MeshData' en paramètre
    /// </summary>
    public Mesh GenerateMesh(MeshData meshData)
    {
        CSData csData = new CSData(meshData);

        //CallGenerateHeightMap(csData, meshData);
        CallGenerateVoxels(csData, meshData);
        Vector4[] debug = new Vector4[csData.voxelNumber];
        voxelsBuffer.GetData(debug);
        CallComputeNormals(csData);
        CallMarchingCubes(csData);

        int numVertices = GetVerticesCount();
        VertexData[] verticesData = new VertexData[numVertices];
        verticesBuffer.GetData(verticesData, 0, 0, numVertices);

        DisposeBuffers();

        Mesh mesh = new Mesh();
        if (numVertices >= 65535) mesh.indexFormat = IndexFormat.UInt32;

        // Création des tableau du mesh
        ParseMesh(verticesData, out Vector3[] mesh_vertices, out int[] mesh_triangles, out Vector3[] mesh_normals);

        mesh.bounds = new Bounds(meshData.origin, meshData.size);
        mesh.vertices = mesh_vertices;
        mesh.normals = mesh_normals;
        mesh.triangles = mesh_triangles;

        List<Vector3> newVertices = new List<Vector3>(mesh.vertices);
        for (int i = 0; i < newVertices.Count; i++)
        {
            newVertices[i] = transform.TransformPoint(newVertices[i]);
        }
        mesh.SetUVs(0, newVertices);

        return mesh;
    }

    /// <summary>
    /// Parse les données de 'verticesData' pour créer les tableau nécessaire pour créer
    /// un mesh
    /// </summary>
    private void ParseMesh(VertexData[] verticesData, out Vector3[] mesh_vertices, out int[] mesh_triangles,
        out Vector3[] mesh_normals)
    {
        int numVertices = verticesData.Length;

        mesh_vertices = new Vector3[numVertices];
        mesh_triangles = new int[numVertices];
        mesh_normals = new Vector3[numVertices];

        for (int i = 0; i < numVertices; i++)
        {
            mesh_vertices[i] = verticesData[i].position;
            mesh_triangles[numVertices - i - 1] = i;
            mesh_normals[i] = verticesData[i].normal;
        }
    }

    /// <summary>
    /// Récupère et renvoie le nombre de sommets dans le buffer de sommets
    /// </summary>
    private int GetVerticesCount()
    {
        verticesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        verticesCountBuffer.SetCounterValue(0);

        // Récupération du nombre de vertex du AppendBuffer
        ComputeBuffer.CopyCount(verticesBuffer, verticesCountBuffer, 0);
        int[] verticesCountArray = {0};
        verticesCountBuffer.GetData(verticesCountArray);
        verticesCountBuffer.Dispose();
        
        return verticesCountArray[0] * 3;
    }

    /// <summary>
    /// Fait appel au kernel de Marching Cubes
    /// </summary>
    private void CallMarchingCubes(CSData csData)
    {
        int marchingCubesKernel = marchingCubesCS.FindKernel("GenerateMesh");

        // Création des buffers
        verticesBuffer = new ComputeBuffer(csData.voxelNumber * 5 * 3, sizeof(float) * 3 * 2, ComputeBufferType.Append);
        verticesBuffer.SetCounterValue(0);

        // Transfer des buffers
        marchingCubesCS.SetFloat("floorValue", floorValue);
        marchingCubesCS.SetInts("blockDim", csData.blockDim);
        marchingCubesCS.SetInt("offsetZ", csData.offsetZ);
        marchingCubesCS.SetInt("offsetY", csData.offsetY);
        marchingCubesCS.SetBuffer(marchingCubesKernel, "VoxelsNormals", voxelNormalsBuffer);
        marchingCubesCS.SetBuffer(marchingCubesKernel, "Voxels", voxelsBuffer);
        marchingCubesCS.SetBuffer(marchingCubesKernel, "Results", verticesBuffer);

        // Appel de Marching Cubes
        marchingCubesCS.Dispatch(marchingCubesKernel, csData.blockDim[0], csData.blockDim[1] - 1, csData.blockDim[2]);
    }

    /// <summary>
    /// Fait appel au kernel qui calcule les normales des voxels
    /// </summary>
    private void CallComputeNormals(CSData csData)
    {
        // Calculer les normales
        int normalsKernel = computeNormalsCS.FindKernel("ComputeNormals");
        voxelNormalsBuffer = new ComputeBuffer(csData.voxelNumber, sizeof(float) * 3);

        computeNormalsCS.SetInts("blockDim", csData.blockDim);
        computeNormalsCS.SetInt("offsetZ", csData.offsetZ);
        computeNormalsCS.SetInt("offsetY", csData.offsetY);
        computeNormalsCS.SetBuffer(normalsKernel, "Voxels", voxelsBuffer);
        computeNormalsCS.SetBuffer(normalsKernel, "Results", voxelNormalsBuffer);

        computeNormalsCS.Dispatch(normalsKernel, csData.blockDim[0], csData.blockDim[1], csData.blockDim[2]);
    }

    /// <summary>
    /// Fait appel au kernel génèrant les voxels
    /// </summary>
    private void CallGenerateVoxels(CSData csData, MeshData meshData)
    {
        int voxelsKernel = voxelsGeneratorCS.FindKernel("ComputeVoxels");
        voxelsBuffer = new ComputeBuffer(csData.voxelNumber, sizeof(float) * 4);

        voxelsGeneratorCS.SetFloats("origin", meshData.origin.ToArray());
        voxelsGeneratorCS.SetFloat("scale", noiseParameters.scale);
        voxelsGeneratorCS.SetFloat("persistance", noiseParameters.persistance);
        voxelsGeneratorCS.SetFloat("lacunarity", noiseParameters.lacunarity);
        voxelsGeneratorCS.SetFloats("local2World",
            meshData.size.Div(meshData.voxelDensity).ToArray());
        voxelsGeneratorCS.SetInt("octaves", noiseParameters.octaves);
        voxelsGeneratorCS.SetInt("chunkY", meshData.chunkY);
        voxelsGeneratorCS.SetInt("maxHeight", maxHeight);
        voxelsGeneratorCS.SetInt("minHeight", maxHeight - maxHeightSmoothing);
        voxelsGeneratorCS.SetInts("blockDim", csData.blockDim);
        voxelsGeneratorCS.SetBuffer(voxelsKernel, "Results", voxelsBuffer);

        voxelsGeneratorCS.Dispatch(voxelsKernel, csData.blockDim[0], csData.blockDim[1], csData.blockDim[2]);
    }

    /// <summary>
    /// Détruit les buffers
    /// </summary>
    private void DisposeBuffers()
    {
        voxelsBuffer.Dispose();
        voxelNormalsBuffer.Dispose();
        verticesBuffer.Dispose();
        verticesCountBuffer.Dispose();
    }

    public int GetMaxHeight()
    {
        return maxHeight;
    }


    MeshFilter meshFilter;

    public void CalculateUVW()
    {
    }

    #region UNITY_EDITOR

#if (UNITY_EDITOR)

    [Header("Visualisation [EDITOR ONLY]")]
    public GameObject chunkPrefab;

    public ChunkParameters chunkParameters;
    public int chunkHeightNb = 3;
    [Space] public bool autoPreview = false;

    public void GeneratePreview()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        float chunkSpacingY = getChunkSpacingY();
        for (int yOffset = -chunkHeightNb / 2; yOffset <= chunkHeightNb / 2; yOffset++)
        {
            Vector3 worldPosition = new Vector3(0, chunkSpacingY * yOffset - 20f, 0);

            GameObject chunkPreview = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, transform);
            MeshFilter chunkPreviewMesh = chunkPreview.GetComponent<MeshFilter>();


            MeshData data = new MeshData(worldPosition, chunkParameters.worldSize, chunkParameters.voxelDensity,
                yOffset);
            chunkPreviewMesh.sharedMesh = GenerateMesh(data);
        }
    }
    
    private float getChunkSpacingY()
    {
        return (chunkParameters.worldSize - 2 * chunkParameters.worldSize.Div(chunkParameters.voxelDensity)).y;
    }

#endif

    #endregion
}