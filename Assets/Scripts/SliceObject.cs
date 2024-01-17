using System.Collections.Generic;
using UnityEngine;

public class SliceObject : MonoBehaviour
{
    private static int ObjectCount = 1;
    ///<summary>切割函数</summary>
    ///<param name="startPos">切割起始点</param>
    ///<param name="endPos">切割终止点</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:内联变量声明", Justification = "<挂起>")]
    public void Slice(Vector2 startPos, Vector2 endPos)
    {
        #region 获取Mesh信息
        MeshFilter MF = GetComponent<MeshFilter>();
        Mesh mesh = MF.mesh;
        //获取当前Mesh的顶点、三角面、UV信息
        List<Vector3> vertList = new(mesh.vertices);
        List<int> triList = new(mesh.triangles);
        List<Vector2> uvList = new(mesh.uv);
        #endregion

        #region 判断是否为有效切割

        //如果完全没有交点且跑出循环了，代表整个切割线都在图形外侧，属于不合法切割
        if (!IsEffectiveCutting(triList, vertList, startPos, endPos))
        {
            Debug.Log($"{name}无法切割");
            return;
        }
        #endregion

        #region 切割
        //遍历三角面，每3个三角顶点为一个三角面进行遍历
        for (int i = 0; i < triList.Count; i += 3)
        {
            int triIndex0 = triList[i];
            int triIndex1 = triList[i + 1];
            int triIndex2 = triList[i + 2];

            Vector2 point0 = vertList[triIndex0];
            Vector2 point1 = vertList[triIndex1];
            Vector2 point2 = vertList[triIndex2];
            //分别代表与一个三角面的01边、12边、02边的交点
            Vector2 crossPoint0_1, crossPoint1_2, crossPoint0_2;
            //如果与01边和12边有交点
            if (Tool.GetCrossPoint(startPos, endPos, point0, point1, out crossPoint0_1) &&
                Tool.GetCrossPoint(startPos, endPos, point1, point2, out crossPoint1_2))
            {
                //将两个交点添加到顶点坐标
                vertList.Add(crossPoint0_1);
                vertList.Add(crossPoint1_2);

                //为两个交点计算UV坐标
                uvList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex1], point0, point1, crossPoint0_1));
                uvList.Add(GetUVPoint(uvList[triIndex1], uvList[triIndex2], point1, point2, crossPoint1_2));

                //用插入法将三角面重新分割
                triList.Insert(i + 1, vertList.Count - 2);  //A
                triList.Insert(i + 2, vertList.Count - 1);  //B
                triList.Insert(i + 3, vertList.Count - 1);  //B
                triList.Insert(i + 4, vertList.Count - 2);  //A
                triList.Insert(i + 6, triIndex0);   //0
                triList.Insert(i + 7, vertList.Count - 1);  //B
                i += 6;
            }
            else if (Tool.GetCrossPoint(startPos, endPos, point1, point2, out crossPoint1_2) &&
                     Tool.GetCrossPoint(startPos, endPos, point2, point0, out crossPoint0_2))
            {
                vertList.Add(crossPoint1_2);
                vertList.Add(crossPoint0_2);

                uvList.Add(GetUVPoint(uvList[triIndex1], uvList[triIndex2], point1, point2, crossPoint1_2));
                uvList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex2], point0, point2, crossPoint0_2));

                triList.Insert(i + 2, vertList.Count - 2);
                triList.Insert(i + 3, vertList.Count - 1);
                triList.Insert(i + 4, vertList.Count - 2);
                triList.Insert(i + 6, vertList.Count - 1);
                triList.Insert(i + 7, triIndex0);
                triList.Insert(i + 8, vertList.Count - 2);
                i += 6;
            }
            else if (Tool.GetCrossPoint(startPos, endPos, point0, point1, out crossPoint0_1) &&
                     Tool.GetCrossPoint(startPos, endPos, point2, point0, out crossPoint0_2))
            {
                vertList.Add(crossPoint0_1);
                vertList.Add(crossPoint0_2);

                uvList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex1], point0, point1, crossPoint0_1));
                uvList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex2], point0, point2, crossPoint0_2));

                triList.Insert(i + 1, vertList.Count - 2);
                triList.Insert(i + 2, vertList.Count - 1);
                triList.Insert(i + 3, vertList.Count - 2);
                triList.Insert(i + 6, triIndex2);
                triList.Insert(i + 7, vertList.Count - 1);
                triList.Insert(i + 8, vertList.Count - 2);
                i += 6;
            }
        }

        #endregion

        #region 测试调试
        //Debug.Log("现在开始打印顶点信息");
        //foreach (var vert in vertList)
        //    Debug.Log(vert);
        //Debug.Log("现在开始打印三角面信息");
        //for(int i = 0; i < triList.Count; i+= 3)
        //{
        //    Debug.Log($"{vertList[triList[i]]}和{vertList[triList[i + 1]]}和{vertList[triList[i + 2]]}");
        //}
        #endregion

        #region 分离成两个Mesh
        MeshData LeftMesh = new();
        MeshData RightMesh = new();
        for (int i = 0; i < triList.Count; i += 3)
        {
            Vector2 middle = (vertList[triList[i]] + vertList[triList[i + 1]] + vertList[triList[i + 2]]) / 3;
            bool isLeft = Tool.IsPointOnLeftSideOfLine(middle, startPos, endPos);
            if (isLeft) LeftMesh.AddTriangles(vertList, triList, uvList, i);
            else RightMesh.AddTriangles(vertList, triList, uvList, i);
        }

        //生成切割图形，并设置Mesh
        GameObject rightGo = Instantiate(gameObject, transform.position, transform.rotation);
        rightGo.name = $"Mesh{++ObjectCount}";
        SliceObject so = rightGo.GetComponent<SliceObject>();
        so.SetMesh(RightMesh.VertList, RightMesh.TriList, RightMesh.UVList);
        SetMesh(LeftMesh.VertList, LeftMesh.TriList, LeftMesh.UVList);


        //分离切割后的图形，以切割线为标准，将分割后的两物体向相反方向位移
        Vector2 normalline = RotateVectorHalfPi(startPos - endPos).normalized;
        rightGo.transform.position += (Vector3)normalline / 10;
        transform.position -= (Vector3)normalline / 10;
        #endregion
    }

    private Vector2 RotateVectorHalfPi(Vector2 vector)
    {
        return new Vector2(-vector.y, vector.x);
    }

    /// <summary>
    /// 设置网格体并更新法向量
    /// </summary>
    /// <param name="vList">顶点列表</param>
    /// <param name="tList">三角面列表</param>
    /// <param name="uvList">UV列表</param>
    public void SetMesh(List<Vector3> vList, List<int> tList, List<Vector2> uvList)
    {
        MeshFilter MF = GetComponent<MeshFilter>();
        //一定要清空，否则永远不知道会发生什么错误
        MF.mesh.Clear();
        //重新赋值顶点，由于多了交点的顶点，三角面和顶点都与切割前不同
        MF.mesh.SetVertices(vList);

        //重新赋值三角面与UV顶点
        MF.mesh.SetTriangles(tList.ToArray(), 0);
        MF.mesh.SetUVs(0, uvList.ToArray());
        MF.mesh.RecalculateNormals();
    }

    /// <summary>
    /// 获取三角面的一边的中间点，相对边的两个顶点位置与UV坐标得到的UV点
    /// </summary>
    /// <param name="startUV">边的起点UV</param>
    /// <param name="endUV">边的终点</param>
    /// <param name="startPoint">边的起点</param>
    /// <param name="endPoint">边的终点</param>
    /// <param name="curPoint">待计算UV的中间点</param>
    /// <returns>中间点的UV</returns>
    public static Vector2 GetUVPoint(in Vector2 startUV, in Vector2 endUV, in Vector2 startPoint, in Vector2 endPoint, Vector2 curPoint)
    {
        //计算出中间点相对起点的距离
        float relate = (startPoint - curPoint).magnitude / (startPoint - endPoint).magnitude;
        // Mathf.Lerp函数的返回值本质含义是，a到b执行了t的进度时，应该在哪里
        return new Vector2(Mathf.Lerp(startUV.x, endUV.x, relate),
                           Mathf.Lerp(startUV.y, endUV.y, relate));
    }

    /// <summary>
    /// 判断起始点组成线段的切割是否有效
    /// </summary>
    /// <param name="triList">三角面数组</param>
    /// <param name="vertList">顶点数组</param>
    /// <param name="startPos">起始点</param>
    /// <param name="endPos">终止点</param>
    /// <returns>是否有效</returns>
    private static bool IsEffectiveCutting(List<int> triList, List<Vector3> vertList, in Vector2 startPos, in Vector2 endPos)
    {
        bool isEffective = false;
        //遍历三角面，每3个三角顶点为一个三角面进行遍历
        for (int i = 0; i < triList.Count; i += 3)
        {
            int triIndex0 = triList[i];
            int triIndex1 = triList[i + 1];
            int triIndex2 = triList[i + 2];

            Vector2 point0 = vertList[triIndex0];
            Vector2 point1 = vertList[triIndex1];
            Vector2 point2 = vertList[triIndex2];
            //判断切割线与每个三角面的任意边是否有交点
            if (Tool.GetCrossPoint(startPos, endPos, point0, point1, out _) ||
                Tool.GetCrossPoint(startPos, endPos, point1, point2, out _) ||
                Tool.GetCrossPoint(startPos, endPos, point2, point0, out _))
            {
                isEffective = true;
            }
            //判断是否有交点后判断是否是不合法的切割线，如果不合法直接return
            //原理：任何一点在任意三角面内，都代表整个物体没有被合法切割
            if (Tool.InnerGraphByAngle(startPos, point0, point1, point2) ||
                Tool.InnerGraphByAngle(endPos, point0, point1, point2))
            {
                // Debug.Log("切割不完整，有至少一个点在图片内");
                return false;
            }
        }
        if (!isEffective)
        {
            // Debug.Log("无效切割！没有交点");
            return false;
        }
        return true;
    }
}

public class MeshData
{
    public List<Vector3> VertList = new(64);
    public List<int> TriList = new(64 * 3);
    public List<Vector2> UVList = new(64);
    //Key代表原Mesh中某顶点下标，Value代表在现在的Mesh中的顶点下标
    private readonly Dictionary<int, int> vertMap = new();
    public void AddTriangles(List<Vector3> vList, List<int> tList, List<Vector2> uvList, int index)
    {
        for (int i = index; i < index + 3; ++i)
        {
            if (!vertMap.ContainsKey(tList[i]))
            {
                //Count不等于Capacity!
                vertMap.Add(tList[i], VertList.Count);
                VertList.Add(vList[tList[i]]);
                UVList.Add(uvList[tList[i]]);
            }
            TriList.Add(vertMap[tList[i]]);
        }
    }
}
