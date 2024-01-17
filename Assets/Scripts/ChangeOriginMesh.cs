using UnityEngine;

public class ChangeOriginMesh : MonoBehaviour
{
    public MeshFilter MF;
    public void Start()
    {
        MF = GetComponent<MeshFilter>();
        Mesh shared_mesh = MF.sharedMesh;
        Vector3[] arr = shared_mesh.vertices;
        // 对sharedmesh的顶点全部更改
        for (int i = 0; i < shared_mesh.vertexCount; i++)
        {
            Vector3 v = arr[i];
            arr[i] = new Vector3(v.x * 3, v.y / 2, v.z);
        }
        shared_mesh.SetVertices(arr);
    }

    private void OnApplicationQuit()
    {
        var shared_mesh = MF.sharedMesh;
        var arr = shared_mesh.vertices;
        for (int i = 0; i < shared_mesh.vertexCount; i++)
        {
            var v = arr[i];
            arr[i] = new Vector3(v.x / 3, v.y * 2, v.z);
        }
        shared_mesh.SetVertices(arr);
        }
}
