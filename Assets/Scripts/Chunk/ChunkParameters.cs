using UnityEngine;

[CreateAssetMenu(fileName = "ChunkParameters", menuName = "ScriptableObjects/ChunkParameters", order = 1)]
public class ChunkParameters : ScriptableObject
{
    [Header("World size for the chunk")]
    public Vector3 worldSize;

    [Header("Density of the chunk")]
    public Vector3Int voxelDensity;

    private void OnValidate()
    {
        voxelDensity = Vector3Int.Max(voxelDensity, Vector3Int.zero);
        worldSize = Vector3.Max(worldSize, Vector3.zero);

        voxelDensity.x -= voxelDensity.x % Vars.NOISE_THREAD_SIZE;
        voxelDensity.z = voxelDensity.x;

        voxelDensity.z -= voxelDensity.z % Vars.NOISE_THREAD_SIZE;
        voxelDensity.x = voxelDensity.z;
    }
}