using UnityEngine;

public class CutLine : MonoBehaviour
{
    [SerializeField] private LineRenderer LR;
    [SerializeField] private Vector3 StartPos;    //切割线的开始点
    [SerializeField] private Vector3 EndPos;      //切割线的终止点
    public bool isDrawing;      //是否正在切割

    public SliceObject[] sliceObjects;      //场景中的全部SliceObject
    public void Start()
    {
        LR = GetComponent<LineRenderer>();
        LR.positionCount = 2;

    }
    public void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isDrawing)
        {
            //按下鼠标时，取得开始点
            isDrawing = true;

            //获取物体的的世界坐标，转换为屏幕坐标，得到物体所在位置的深度；
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
            //获取鼠标的屏幕坐标
            Vector3 mousePositionOnScreen = Input.mousePosition;
            //为鼠标的屏幕坐标赋值深度
            mousePositionOnScreen.z = screenPosition.z;
            //将鼠标的屏幕坐标转换为世界坐标
            StartPos = Camera.main.ScreenToWorldPoint(mousePositionOnScreen);
            //设置起始点
            LR.SetPosition(0, StartPos);
        }
        if (Input.GetMouseButton(0))
        {
            //按住鼠标时，实时更新终止点
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
            Vector3 mousePositionOnScreen = Input.mousePosition;
            mousePositionOnScreen.z = screenPosition.z;
            EndPos = Camera.main.ScreenToWorldPoint(mousePositionOnScreen);
            LR.SetPosition(1, EndPos);
        }
        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            //抬起鼠标时，进行一次切割判断
            isDrawing = false;
            Slice();
        }
    }

    private void Slice()
    {
        //获取场景中的全部SliceObject
        sliceObjects = FindObjectsOfType<SliceObject>();
        if (sliceObjects.Length == 0)
        {
            Debug.Log("未找到切割物体");
            return;
        }
        for (int i = 0; i < sliceObjects.Length; i++)
        {
            //将开始点和终止点由世界坐标系转为物体子坐标系
            Vector2 startPosLocal = sliceObjects[i].transform.InverseTransformPoint(StartPos);
            Vector2 endPosLocal = sliceObjects[i].transform.InverseTransformPoint(EndPos);
            //将切割的开始结束点传给物体并进行Slice计算
            sliceObjects[i].Slice(startPosLocal, endPosLocal);
        }
    }
}
public static class Tool
{
    ///<summary>计算一个点是否在一个多边形内</summary>
    ///<param name="point">点的坐标</param>
    ///<param name="polygon">多边形的各个顶点坐标</param>
    ///<returns>如果在多边形内返回True，否则返回False</returns>
    ///<remarks>思路：从给定点绘制一条射线，然后计算该射线与多边形边界的交点数量</remarks>
    public static bool InnerGraphByAngle(Vector2 point, params Vector2[] polygon)
    {
        int intersectCount = 0;
        int vertexCount = polygon.Length;

        for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                intersectCount++;
            }
        }
        return intersectCount % 2 == 1;
    }

    ///<summary>计算坐标系中的任意两条线段是否相交</summary>
    /// <param name="a">第一条线段的一个端点坐标</param>
    /// <param name="b">第一条线段的另一个端点坐标</param>
    /// <param name="c">第二条线段的一个端点坐标</param>
    /// <param name="d">第二条线段的另一个端点坐标</param>
    /// <param name="crossPoint">输出交点的坐标，如果没有交点输出(0,0)</param>
    /// <returns>如果有交点返回True，否则返回False</returns>
    public static bool GetCrossPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 crossPoint)
    {
        crossPoint = Vector2.zero;
        double denominator = (b.y - a.y) * (d.x - c.x) - (a.x - b.x) * (c.y - d.y);
        if (denominator == 0) return false;
        double x = ((b.x - a.x) * (d.x - c.x) * (c.y - a.y)
                    + (b.y - a.y) * (d.x - c.x) * a.x
                    - (d.y - c.y) * (b.x - a.x) * c.x) / denominator;
        double y = -((b.y - a.y) * (d.y - c.y) * (c.x - a.x)
                    + (b.x - a.x) * (d.y - c.y) * a.y
                    - (d.x - c.x) * (b.y - a.y) * c.y) / denominator;
        if ((x - a.x) * (x - b.x) <= 0 && (y - a.y) * (y - b.y) <= 0
             && (x - c.x) * (x - d.x) <= 0 && (y - c.y) * (y - d.y) <= 0)
        {
            crossPoint = new Vector2((float)x, (float)y);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断一个点是否在某条线段（直线）的左侧
    /// </summary>
    /// <param name="point">点的坐标</param>
    /// <param name="startPoint">线段起点</param>
    /// <param name="endPoint">线段终点</param>
    /// <returns>在直线左侧</returns>
    public static bool IsPointOnLeftSideOfLine(Vector2 point, Vector2 startPoint, Vector2 endPoint)
    {
        // 计算直线上的向量
        Vector2 lineVector = endPoint - startPoint;
        // 计算起始点到点的向量
        Vector2 pointVector = point - startPoint;
        // 使用叉乘判断点是否在直线的左侧
        float crossProduct = (lineVector.x * pointVector.y) - (lineVector.y * pointVector.x);
        // 如果叉乘结果大于0，则点在直线的左侧
        return crossProduct > 0;
    }
}