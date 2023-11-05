using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshGenerator))]
public class MeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MeshGenerator meshGenerator = (MeshGenerator)target;

        if (DrawDefaultInspector() && meshGenerator.autoPreview)
            meshGenerator.GeneratePreview();

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Chunk"))
            meshGenerator.GeneratePreview();
    }
}