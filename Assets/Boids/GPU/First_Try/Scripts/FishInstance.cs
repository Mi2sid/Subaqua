using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ObjData
{
    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;

    public Matrix4x4 matrix
    {
        get
        {
            return Matrix4x4.TRS(pos, rot, scale);
        }
    }

    public ObjData(Vector3 pos, Vector3 scale, Quaternion rot)
    {
        this.pos = pos;
        this.scale = scale;
        this.rot = rot;
    }
}

public class FishInstance : MonoBehaviour
{

    public Mesh fish;
    public int instances;
    public Vector3 scale;
    public Material fish_mat;

    public int ite = 0;

    private List<List<ObjData>> batches = new List<List<ObjData>>();

    // Start is called before the first frame update
    void Start()
    {
        int batchIndexNum = 0;
        List<ObjData> currBatch = new List<ObjData>();
        for(int i = 0; i < instances; i++)
        {
            AddObj(currBatch, i);
            batchIndexNum++;

            if(batchIndexNum >= 1000)
            {
                batches.Add(currBatch);
                currBatch = BuildNewBatch();
                batchIndexNum = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        RenderBatches();
        
    }

    private void AddObj(List<ObjData> currBatch, int i)
    {
        Vector3 position = new Vector3(Random.Range(-scale.x, scale.x), Random.Range(-scale.y, scale.y), Random.Range(-scale.z, scale.z));

        currBatch.Add(new ObjData(position, new Vector3(2, 2, 2), Quaternion.identity));
    }

    private List<ObjData> BuildNewBatch()
    {
        return new List<ObjData>();
    }

    private void RenderBatches()
    {
        int temp = 0;

        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(fish, 0, fish_mat, batch.Select((a) => a.matrix).ToList());

            List <Matrix4x4> Test = batch.Select((a) => a.matrix).ToList();

            temp += Test.Capacity;
        }

        Debug.Log(temp);

    }
}
