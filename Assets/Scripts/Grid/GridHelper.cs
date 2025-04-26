using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class GridInfo
{
    [ReadOnly]
    public int id;
    public int sceneId;

    [ReadOnly]
    public int q;
    [ReadOnly]
    public int r;
    [ReadOnly]
    public int s;
    [ReadOnly]
    public int heightOrder;
    public bool hadConfiguration = false;

    [Header("坐标位置")]
    public Vector3 centerPos;
    //从中心点开始, 正前方向(+z)的顶点为vertex0, 逆时针依次为vertex1,vertex2,vertex3,vertex4,vertex5
    //vertex0与vertex1构成的边为edge0, 逆时针依次为edge1, edge2, edge3, edge4, edge5
    public List<Vector3> vertices = new List<Vector3>();

    [Header("行走代价")]
    public float movePrice = 1;

    public bool walkable = true;
    //slopLimit标记 通过Scan判定为坡度过大的网格, 若开启onlySaveWalkableGrids, 则不会序列化到配置表中
    public bool slopLimit = false;
    //obstacleLimit标记 通过Scan判定为被障碍物遮挡的网格, 若开启onlySaveWalkableGrids, 则不会序列化到配置表中
    public bool obstacleLimit = false;
    //edgeLimit标记 通过手动判定为动态Limit的网格, 这些网格可以通过一定条件变为walkable, 因此会被正常序列化
    public bool dynamicLimit = false;
}

[ExecuteInEditMode]
public class GridHelper : MonoBehaviour
{
    public GridInfo info = new GridInfo();

    [Header("相邻单位")]
    public List<GridHelper> neighborGrids = new List<GridHelper>(); //用于后续的A*寻路

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //[Title("Config")]
    [Button("Register Grid")]
    public void RegisterGrid()
    {

    }

    //[Button("Unregister Grid")]
    public void UnregisterGrid()
    {

    }

}
