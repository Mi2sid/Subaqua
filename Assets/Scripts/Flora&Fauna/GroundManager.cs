using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundManager : MonoBehaviour
{

    public enum MeshPointState
    {
        SLOPE,
        FLAT,
    }

    public struct MeshPointInfo
    {
        public MeshPointInfo(Vector3 pos, Vector3 normal, MeshPointState state, int elementId)
        {
            this.state = state;
            this.pos = pos;
            this.normal = normal;
            this.elementId = elementId;
        }

        public MeshPointState state;
        public Vector3 pos;
        public Vector3 normal;
        public int elementId;
    }

    public struct MeshGround
    {
        public MeshGround(Dictionary<int, GameObject[]> flatPool, Dictionary<int, GameObject[]> slopePool)
        {
            this.flatPool = flatPool;
            this.slopePool = slopePool;
            this.pool = null;
        }

        public MeshGround(Dictionary<int, GameObject[]> pool)
        {
            this.flatPool = null;
            this.slopePool = null;
            this.pool = pool;
        }

        public Dictionary<int, GameObject[]> flatPool;
        public Dictionary<int, GameObject[]> slopePool;
        public Dictionary<int, GameObject[]> pool;
    }

    public enum Biome
    {
        CORAL,
        GRASS,
        ROCKY,
        NORMAL,
        DESERT
    }

    public enum Rotation
    {
        GROUND,
        UP,
        DOWN
    }

    /// <summary>
    /// Retourne de mani�re al�atoire des points sur le mesh
    /// </summary>
    public static MeshPointInfo GetRandomPointOnMesh(int[] triangles, Vector3[] vertices, Vector3[] normals, float slopeAngle, System.Random random)
    {
        int triIndex = random.Next(0, triangles.Length / 3);

        Vector3 a = vertices[triangles[triIndex * 3]];
        Vector3 b = vertices[triangles[triIndex * 3 + 1]];
        Vector3 c = vertices[triangles[triIndex * 3 + 2]];

        Vector3 na = normals[triangles[triIndex * 3]];
        Vector3 nb = normals[triangles[triIndex * 3 + 1]];
        Vector3 nc = normals[triangles[triIndex * 3 + 2]];

        float r = (float)random.NextDouble();
        float s = (float)(random.NextDouble() * (1.0 - r) + r);

        Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);
        Vector3 normalAtPoint = na + r * (nb - na) + s * (nc - na);

        return new MeshPointInfo(pointOnMesh, normalAtPoint, (Vector3.Angle(Vector3.up, normalAtPoint) > slopeAngle ? MeshPointState.SLOPE : MeshPointState.FLAT), 0);
    }
}


    // public static MeshPointInfo GetRandomPointOnMesh(int[] triangles, Vector3[] vertices, Vector3[] normals, float slopeAngle)
    // {
    //     System.Random randomGenerator = new System.Random();

    //     int triIndex = randomGenerator.Next(0, triangles.Length / 3);

    //     Vector3 a = vertices[triangles[triIndex * 3]];
    //     Vector3 b = vertices[triangles[triIndex * 3 + 1]];
    //     Vector3 c = vertices[triangles[triIndex * 3 + 2]];

    //     Vector3 na = normals[triangles[triIndex * 3]];
    //     Vector3 nb = normals[triIndex * 3 + 1];
    //     Vector3 nc = normals[triIndex * 3 + 2];

    //     float r = (float)randomGenerator.NextDouble();
    //     float s = (float)randomGenerator.NextDouble() * (1f - r) + r;

    //     Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);
    //     Vector3 normalAtPoint = na + r * (nb - na) + s * (nc - na);

    //     return new MeshPointInfo(pointOnMesh, normalAtPoint, (Vector3.Angle(Vector3.up, normalAtPoint) > slopeAngle ? MeshPointState.SLOPE : MeshPointState.FLAT), 0);
    // }