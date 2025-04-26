using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Drawing;
using System.Linq;
using System.IO;
using System;

/// <summary>
/// 为Unity中Vector3写的拓展方法
/// </summary>
public static class Vector3Extensions
{
    /// <summary>
    /// 返回调用者start和目标end的连线在XZ平面上的距离
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float DistanceOnXZ(this Vector3 start, Vector3 end)
    {
        Vector2 startXZ = new Vector2(start.x, start.z);
        Vector2 endXZ = new Vector2(end.x, end.z);
        return Vector2.Distance(startXZ, endXZ);
    }
};

//用于序列化和反序列化网格数据
public class GridsConfigTable
{
    public List<GridInfo> gridInfos = new List<GridInfo>();
}

[ExecuteInEditMode]
public class GridMap : MonoBehaviour
{
    public static GridMap Instance;

    [Title("Settings")]
    public bool showGrid = false; //控制网格边框显示
    private bool lastShowGrid = false; //记录上一次的 showGrid 状态
    public bool showEdge = false; //控制所有网格的最外边框显示
    private bool lastShowEdge = false; //记录上一次的 showEdge 状态
    public bool showHelper = false; //控制网格方片显示
    private bool lastShowHelper = false; //记录上一次的 showHelper 状态

    public GameObject gridModel;

    [MinValue(0.1f), MaxValue(10.0f)]
    public float hexHalfSize = 0.5f; //网格一半长度 
    public float RayHeight = 100f; //射线检测高度
    //public LayerMask ObstacleLayer; //障碍物层级
    //public LayerMask BridgeLayer; //桥层级

    [Range(10.0f, 90.0f)]
    public float MaxSlopeAngle = 45.0f; //可行走网格的最大坡度
    private float tanSlopeAngle
    {
        get
        {
            return Mathf.Tan(Mathf.Deg2Rad* MaxSlopeAngle);
        }
    }
    [Range(0.0f, 1.0f)]
    public float MaxHeightDiff = 0.5f; //可行走网格的最大高度差

    //public GameObject gridsParent;
    public List<Transform> scenes = new List<Transform>();
    public List<Transform> gridsParents = new List<Transform>();
    public List<Transform> linesParents = new List<Transform>();
    //public GameObject lineParent;

    [Title("Search")]
    public int q;
    public int r;
    public int heightOrder = 0;
    [HorizontalGroup("Search")]
    public GridHelper searchResult;

    [Title("Grids")]
    [HorizontalGroup("Start")]
    public float startX; //第一个网格的x坐标
    [HorizontalGroup("Start")]
    [Title("")]
    public float startZ; //第一个网格的y坐标
    [HorizontalGroup("Num")]
    public int numX; //x方向的网格数量
    [HorizontalGroup("Num")]
    public int numZ; //z方向的网格数量
    public List<GridHelper> allGrids = new List<GridHelper>(); //网格合集

    [Title("Config")]
    [PropertyOrder(1)]
    public bool onlySaveWalkableGrids = true; //是否只存储可行走网格数据

    [HideInInspector]
    public bool gridMapInitialized = false; //用来标记GridMap是否已经初始化完成

    //key:(Q,R,HeightOrder) value:Tile所挂载的GridHelper
    private Dictionary<Vector3, GridHelper> gridDic = new Dictionary<Vector3, GridHelper>();

    //用于显示网格线
    private List<GridHelper> edgeGrids = new List<GridHelper>(); //边缘网格 
    private List<LineRenderer> edgeRenderer = new List<LineRenderer>(); //记录用于渲染最外边框的LineRenderer
    private List<LineRenderer> gridRenderer = new List<LineRenderer>(); //记录用于渲染每个网格的LineRenderer

    //常数
    const float sqrt3 = 1.73205f;

    private void OnEnable()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Application.isPlaying)
        {
            LoadFormConfigTable();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (showGrid != lastShowGrid)
        {
            UpdateGridOutlineVisibility();
            lastShowGrid = showGrid;
        }
    }

    /// <summary>
    /// 监控inspector面板变量(主要是Show相关变量)更新
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying && showGrid != lastShowGrid)
        {
            UpdateGridOutlineVisibility();
            lastShowGrid = showGrid;
        }

        if (!Application.isPlaying && showEdge != lastShowEdge)
        {
            UpdateEdgeOutlineVisibility();
            lastShowEdge = showEdge;
        }

        if (!Application.isPlaying && showHelper != lastShowHelper) // 只有 showHelper 变化时才执行
        {
            UpdateHelperVisibility();
            lastShowHelper = showHelper; // 更新记录的状态
        }
    }

    /// <summary>
    /// 更新所有网格轮廓的可见性
    /// </summary>
    private void UpdateGridOutlineVisibility()
    {
        Vector3 offset = new Vector3(0, 0.1f, 0);
        //加载材质
        Material outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        Material walkableMaterial = Resources.Load<Material>("Materials/walkableMaterial");
        Material unwalkableMaterial = Resources.Load<Material>("Materials/unwalkableMaterial");
        if (outlineMaterial == null || walkableMaterial == null || unwalkableMaterial == null)
        {
            Debug.Log("Material Lost!");
            return;
        }
        //勾选状态
        if (showGrid)
        {
            if(allGrids.Count == 0)
            {
                Debug.Log("Draw Failed! There is no grid on this scene!");
                return;
            }
            foreach (GridHelper grid in allGrids)
            {
                GridInfo info = grid.info;
                LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
                //根据是否可行走选择渲染材质
                lr.material = grid.info.walkable ? walkableMaterial : unwalkableMaterial;

                lr.transform.SetParent(linesParents[info.sceneId]);
                lr.positionCount = 6;
                //offset.y = info.heightOrder == 0 ? 0.2f : 0.4f; //让桥下网格显示的更清晰
                lr.SetPositions(new Vector3[] { info.vertices[0] + offset,
                    info.vertices[1] + offset, info.vertices[2] + offset,
                    info.vertices[3] + offset, info.vertices[4] + offset,
                    info.vertices[5] + offset});
                gridRenderer.Add(lr);
            }
        }
        //非勾选状态
        else
        {
            foreach (LineRenderer lr in gridRenderer)
            {
                lr.material = outlineMaterial;
                LineRendererPool.Instance.ReturnLineRenderer(lr);
            }
            gridRenderer.Clear();
        }
    }

    /// <summary>
    /// 更新所有网格的最外边缘线的可见性
    /// </summary>
    private void UpdateEdgeOutlineVisibility()
    {
        Vector3 offset = new Vector3(0, 0.2f, 0);
        //勾选状态
        if (showEdge)
        {
            if(edgeGrids.Count == 0)
            {
                Debug.Log("Draw Failed! There is no grid on this scene!");
                return;
            }
            for(int i = 0; i < edgeGrids.Count; i++)
            {
                for(int j = 0; j < 6; j++)
                {
                    GridInfo info = edgeGrids[i].info;
                    if (edgeGrids[i].neighborGrids[j] == null)
                    {
                        int next = (j + 1) % 6;
                        LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
                        lr.transform.SetParent(linesParents[info.sceneId]);
                        lr.positionCount = 2;
                        lr.SetPositions(new Vector3[] 
                            { info.vertices[j] + offset, 
                            info.vertices[next] + offset }); //offset 0.2f
                        
                        edgeRenderer.Add(lr); //记录正在使用的LineRenderer
                    }
                }
            }
        }
        //非勾选状态
        else
        {
            foreach(LineRenderer lr in edgeRenderer)
            {
                LineRendererPool.Instance.ReturnLineRenderer(lr);
            }
            edgeRenderer.Clear();
        }
    }


    /// <summary>
    /// 更新每个HexTile的Helper面片可见性
    /// </summary>
    private void UpdateHelperVisibility()
    {
        foreach(GridHelper helper in allGrids)
        {
            Transform helperTransform = helper.transform.GetChild(0);
            if(helperTransform != null)
            {
                helperTransform.gameObject.SetActive(showHelper);
            }
        }
    }

    /// <summary>
    /// 通过x,y,z来寻找对应网格
    /// </summary>
    [HorizontalGroup("Search", Width = 0.3f)]
    [Button("Search")]
    public void SearchGridHelper()
    {
        if(allGrids.Count == 0)
        {
            Debug.Log("Search Failed! There is no grid on scene");
            return;
        }
        Vector3 searchTarget = new Vector3(q, r, heightOrder);
        if (gridDic.ContainsKey(searchTarget))
        {
            searchResult = gridDic[searchTarget];
        }
        else
        {
            searchResult = null;
        }
    }

    /// <summary>
    /// 提供给外部的搜索网格方法
    /// </summary>
    /// <param name="q"></param>
    /// <param name="r"></param>
    /// <param name="heightOrder"></param>
    /// <returns></returns>
    public GridHelper SearchGrid(int q, int r, int heightOrder)
    {
        Vector3 index = new Vector3(q, r, heightOrder);
        if (gridDic.ContainsKey(index))
        {
            return gridDic[index];
        }
        //Debug.Log("GridMap search null");
        return null;
    }


    /// <summary>
    /// 扫描地形, 通过射线检测生成用于寻路移动的网格
    /// </summary>
    [HorizontalGroup("Grids", Width = 0.5f)]
    [PropertyOrder(0)]
    [Button("Scan")]
    [ProgressBar(0,100)]
    public void Scan()
    {
        if(allGrids.Count != 0)
        {
            Debug.Log("Scan Failed! There are grids already in this scene, you should scan after clear all grids!");
            return;
        }
        GameObject tile = gridModel;
        tile.GetComponent<HexagonMeshGenerator>().hexSize = hexHalfSize; //初始化瓦片 设置大小

        //中心点的射线检测源点
        Vector3 rayCenter = new Vector3(startX, RayHeight, startZ);
        //HexTile的轴向坐标(q,r)
        int q = -1;
        int r = -1;
        
        int id = 0; //HexTile的Id
        float progress = 0.0f; //用于更新进度条
        float tanSlopeAngle = Mathf.Tan(Mathf.Deg2Rad * MaxSlopeAngle); //最大坡度的tan值
        int totalGrids = numX * numZ;
        for (int i = 0; i < numZ; i++)
        {
            rayCenter.x = startX + (i % 2 == 1 ? (hexHalfSize * 0.5f * sqrt3) : 0);
            rayCenter.z = startZ + i * hexHalfSize * 1.5f;

            q = -(i / 2) - 1;
            r++;
            for (int j = 0; j < numX; j++)
            {
                q++;
                rayCenter.x += hexHalfSize * sqrt3;

                //记录射线检测到中心点, 因为可能存在桥, 所以在同一位置的不同高度可能存在多个中心点,
                //用List记录该tile的GridInfo信息
                List<GridInfo> tileInfo = new List<GridInfo>();

                //是否忽略Bridge层, 第一次传入射线检测时为false, 即不忽略,
                //若第一次检测到Bridge层, 则将ignoreBrige设为true, 进行第二次射线检测, 并且忽略Bridge层
                bool ignoreBrige = false;

                //射线检测
                GridInfo gridInfo = getGridInfo(rayCenter, ref ignoreBrige);
                //若检测返回不为null, 说明在gridInfo.centerPos处至少需要创建1个HexTile
                if(gridInfo != null)
                {
                    gridInfo.id = id++;
                    tileInfo.Add(gridInfo);
                    //若ignoreBrige为true, 说明gridInfo所在位置的层级为Bridge, 需要进行第二次射线检测
                    if (ignoreBrige == true)
                    {
                        GridInfo gridInfo2 = getGridInfo(rayCenter, ref ignoreBrige);
                        if(gridInfo2 != null) {
                            gridInfo2.id = id++;
                            tileInfo.Add(gridInfo2);
                        }
                    }
                }
                //根据tileInfo中的GridInfo信息生成HexTeil, 挂载GridHelper脚本,
                //并加入到allGrids列表和gridDic字典中
                for(int k = 0; k < tileInfo.Count; k++)
                {
                    //根据坡度 判断中心点与6个顶点所形成的面是否可以行走
                    if (ExistSlopeLimit(tileInfo[k]))
                    {
                        tileInfo[k].walkable = false;
                        tileInfo[k].slopLimit = true;
                    }
                    //实例化grid网格
                    GameObject grid = Instantiate(tile, tileInfo[k].centerPos, Quaternion.identity);
                    grid.layer = LayerMask.NameToLayer("Grid");

                    //关键代码!!! 
                    //如果没有这行代码, 新实例化的网格grid将立即具备MeshCollider, 会影响后续射线检测
                    //从而导致误判地形高度, 射线可能击中自己生成的网格, 而不是地形, 记录错误的顶点位置
                    //因此在实例化新的grid网格时, 要先将MeshCollider组件失活, 避免干扰后续的射线检测
                    //等所有网格扫描完毕后 重新启用 MeshCollider
                    grid.GetComponent<MeshCollider>().enabled = false;
                    
                    grid.name = string.Format("grid_{0}_{1}_{2}", q, r, k);
                    grid.transform.SetParent(gridsParents[tileInfo[k].sceneId], true);
                    GridHelper helper = grid.AddComponent<GridHelper>();
                    //初始化tile瓦片所挂载的脚本helper上的信息
                    helper.info = tileInfo[k];
                    helper.info.q = q;
                    helper.info.r = r;
                    helper.info.s = -q - r;
                    helper.info.heightOrder = k;

                    allGrids.Add(helper);
                    gridDic.Add(new Vector3(q, r, k), helper);
                }
                //更新进度条
                progress = (i * numX + j + 1) / (float)totalGrids * 50f;
                EditorUtility.DisplayProgressBar("Scanning Scene", $"Processing Scanning Grid ({i},{j})", progress);
            }
        }

        //遍历gridDic, 初始化每个GridHelper脚本上GridInfo类中的neighborGrids信息
        //遍历方向时, 从左前方向(-x+z), 即vertex0与vertex1所构成的 edge0 的方向 逆时针遍历,
        Vector3[] dirs = new Vector3[] {
            new Vector3(-1, 1, 0), Vector3.left, Vector3.down,
            new Vector3(1, -1, 0), Vector3.right, Vector3.up};
        foreach(Vector3 key in gridDic.Keys)
        {
            //所有网格已扫描完毕 重新启用每个Grid的 MeshCollider
            gridDic[key].GetComponent<MeshCollider>().enabled = true;

            GridInfo info = gridDic[key].info;
            bool onEdge = false;
            for(int i = 0; i < 6; i++)
            {
                //如果同一heightOrder的dirs[i]方向
                //或者 在可以允许的高度差内但不同heightOrder的dirs[i]方向(主要用来处理同在桥下地面但不同heightOrder的网格的neighborGrids)
                //存在网格, 则加入neighborGrids中
                if (gridDic.ContainsKey(key + dirs[i])
                    && Mathf.Abs(info.centerPos.y - gridDic[key + dirs[i]].info.centerPos.y) < 2 * MaxHeightDiff)
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i]]);
                }
                else if(gridDic.ContainsKey(key + dirs[i] + Vector3.forward) 
                    && Mathf.Abs(info.centerPos.y - gridDic[key + dirs[i] + Vector3.forward].info.centerPos.y) < MaxHeightDiff)
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i] + Vector3.forward]);
                }
                else if(gridDic.ContainsKey(key + dirs[i] + Vector3.back)
                    && Mathf.Abs(info.centerPos.y - gridDic[key + dirs[i] + Vector3.back].info.centerPos.y) < MaxHeightDiff)
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i] + Vector3.back]);
                }
                //如果dirs[i]方向不存在网格, 说明该网格处于边缘, 将其加入到edgeGrids中
                else
                {
                    gridDic[key].neighborGrids.Add(null);
                    onEdge = true;
                }
                if(onEdge)
                {
                    edgeGrids.Add(gridDic[key]);
                }
            }
            //更新进度条
            progress = 50 + info.id / (float)gridDic.Count * 50f;
            EditorUtility.DisplayProgressBar("Scanning Scene", $"Processing Build Neighborhood ({info.q},{info.r},{info.heightOrder})", progress);
        }
        Debug.Log($"Scan Finished! You have created {allGrids.Count} grids on scene");
        EditorUtility.ClearProgressBar(); // 完成后清除进度条
    }

    /// <summary>
    /// 对中心点rayCenter和6个顶点做射线检测, 若不返回null, 代表该中心点可以生成HexTile
    /// </summary>
    /// <param name="rayCenter">射线检测的中心点</param>
    /// <param name="IgnoreBridge">若返回true, 则需要忽略Bridge层, 再次进行射线检测</param>
    /// <returns></returns>
    private GridInfo getGridInfo(Vector3 rayCenter, ref bool ignoreBridge)
    {
        GridInfo gridInfo = new GridInfo();
        int layerMask = ignoreBridge ? ~(1 << LayerMask.NameToLayer("Bridge")) : -1;
        for (int k = 0; k <= 6; k++)
        {
            Vector3 raySource = rayCenter;
            float angle = (Mathf.PI / 3 * k) + Mathf.PI / 6;
            if (k > 0)
            {
                //计算得到6个顶点的射线检测源点
                raySource = rayCenter + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * hexHalfSize;
            }
            RaycastHit hitInfo;
            //如果射线检测到collider
            if (Physics.Raycast(raySource, Vector3.down, out hitInfo, 150, layerMask))
            {
                //如果射线检测碰撞到的是障碍物
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    gridInfo.walkable = false;
                    gridInfo.obstacleLimit = true;
                }
                //如果射线检测碰撞到的是桥, 就将ignoreBridge设为true, 让第二次检测时忽略Bridge层
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Bridge"))
                {
                    ignoreBridge = true;
                }
                //设置中心点
                if (k == 0)
                {
                    gridInfo.centerPos = hitInfo.point + new Vector3(0, 0.02f, 0);
                    //根据中心点判断该网格所属场景id
                    for(int i = 0; i < scenes.Count; i++)
                    {
                        if(hitInfo.transform.parent == scenes[i])
                        {
                            gridInfo.sceneId = i;
                        }
                    }
                }
                //设置顶点
                else
                {
                    gridInfo.vertices.Add(hitInfo.point + new Vector3(0, 0.02f, 0));
                }
            }
            //只要中心点或者有1个顶点没有检测到collider
            else
            {
                return null;
            }
        }
        //如果中心点和所有顶点都检测到collider, 则返回网格信息
        return gridInfo;
    }

    /// <summary>
    /// 根据坡度判断当前tile是否不能行走
    /// </summary>
    /// <returns></returns>
    private bool ExistSlopeLimit(GridInfo info)
    {
        Vector3 center = info.centerPos;
        float height = 0; //两点在Y轴上的高度差
        float distanceV2V_Near = sqrt3 * hexHalfSize; //间隔1个顶点的两顶点距离
        float distanceV2V_Far = 2 * hexHalfSize; //对角顶点的两顶点距离

        for (int i = 0; i < 6; i++)
        {
            //判断网格中心点和6个顶点的连线 与 xz平面 形成的角度
            height = Mathf.Abs(center.y - info.vertices[i].y);
            //判断夹角是否大于最大坡度, 并判断高度差是否超过限制（小坡不算坡）
            if(height / hexHalfSize > tanSlopeAngle && height > MaxHeightDiff)
            {
                return true;
            }
            //判断任意两个紧邻顶点的连线 与 xz平面 形成的角度
            int next = (i + 1) % 6;
            height = Mathf.Abs(info.vertices[i].y - info.vertices[next].y);
            if (height / hexHalfSize > tanSlopeAngle && height > MaxHeightDiff)
            {
                return true;
            }
        }

        for(int i = 0; i < 3; i++)
        {
            for(int j = 2; j <= 4; j++) // j=2:间隔1个顶点, j=3:对角顶点, j=4:间隔1个顶点
            {
                int next = (i + j) % 6;
                height = Mathf.Abs(info.vertices[i].y - info.vertices[next].y);
                float distOnXZ = (j == 3) ? distanceV2V_Far : distanceV2V_Near;
                if(height / distOnXZ > tanSlopeAngle && height > MaxHeightDiff)
                {
                    return true;
                }
            }
        }
        //所有点之间的连线和XZ平面的角度都不超过最大坡度, 则不存在SlopeLimit
        return false;
    }

    /// <summary>
    /// 清除当前场景上的所有网格
    /// </summary>
    [HorizontalGroup("Grids", Width = 0.5f)]
    [PropertyOrder(0)]
    [Button("Clear")]
    public void Clear()
    {
        allGrids.Clear();
        gridDic.Clear();
        edgeGrids.Clear();
        foreach(Transform parent in gridsParents)
        {
            while (parent.childCount > 0)
            {
                DestroyImmediate(parent.GetChild(0).gameObject);
            }
        }
        Debug.Log("Clear Grids Finished!");
    }

    /// <summary>
    /// 将所有网格数据配置成Json文件, 存储到StreamingAssets中, 数据将用于游戏首次运行时配置网格
    /// </summary>
    [HorizontalGroup("Config", Width = 0.5f)]
    [PropertyOrder(1)]
    [Button("Save")]
    public void SaveToStreaming()
    {
        //如果使用LitJson, 则需要为LitJson扩展Vector3的序列化和反序列化方法
        //LitJsonExtensions.Vector3RegisterCustomConverters();
        
        //使用JsonUtlity
        //将allGrids中的所有GridHelper脚本中的GridInfo信息序列化
        GridsConfigTable table = new GridsConfigTable();
        foreach(GridHelper helper in allGrids)
        {
            helper.info.hadConfiguration = true; //标记为已经存储进配置表中
            //开启onlySaveWalkableGrids, 则只存储可行走网格(dynamicLimit除外)
            if (onlySaveWalkableGrids && !helper.info.slopLimit && !helper.info.obstacleLimit)
            {
                table.gridInfos.Add(helper.info);
            }
            //不开启onlySaveWalkableGrids
            else if(!onlySaveWalkableGrids)
            {
                table.gridInfos.Add(helper.info);
            }
        }
        JsonManager.Instance.SaveData(table, "GridMap/GridMapConfigTable", 
            PathType.Streaming, JsonType.JsonUtility);
        if(File.Exists(Application.streamingAssetsPath + "/GridMap/GridMapConfigTable.json"))
        {
            if(onlySaveWalkableGrids)
            {
                Debug.Log($"Save Finished! You have saved {table.gridInfos.Count} WALKABLE gridInfos into the GridMapConfigTable");
            }
            else
            {
                Debug.Log($"Save Finished! You have saved {table.gridInfos.Count} gridInfos into the GridMapConfigTable");
            }
        }
        table.gridInfos.Clear();
        table.gridInfos = null;
        table = null;
    }

    /// <summary>
    /// 将所有网格数据配置成Json文件, 存储到PersistentData中, 用于玩家存档和调用存档
    /// </summary>
    public void SaveToPesistent()
    {

    }


    /// <summary>
    /// 从Json配置文件中读取网格数据并加载到场景上
    /// </summary>
    [HorizontalGroup("Config", Width = 0.5f)]
    [PropertyOrder(1)]
    [Button("Load")]
    public void LoadFormConfigTable()
    {
        //如果场景上已经存在网格数据, 就全部清除
        if(allGrids.Count != 0)
        {
            //清除allGrids, gridDic, edgeGrids
            Clear();
            //将所有LineRenderer返回给Pool并清除
            showGrid = false;
            showEdge = false;
            LineRendererPool.Instance.ClearAllLineRenderers();
        }
        //使用JsonUtlity和LitJson都可以反序列化带有空格和换行的json数据
        //使用JsonUtlity
        GridsConfigTable table = new GridsConfigTable();
        table = JsonManager.Instance.LoadData<GridsConfigTable>("GridMap/GridMapConfigTable", 
            PathType.Streaming, JsonType.JsonUtility);
        //重新填入所有网格数据, 和Scan流程相似
        GameObject tile = gridModel;
        float progress = 0.0f;
        tile.GetComponent<HexagonMeshGenerator>().hexSize = hexHalfSize; //初始化瓦片 设置大小
        for (int i = 0; i < table.gridInfos.Count; i++)
        {
            GridInfo info = table.gridInfos[i];
            //初始化allGrids
            //实例化tile瓦片
            GameObject grid = Instantiate(tile, info.centerPos, Quaternion.identity);
            grid.layer = LayerMask.NameToLayer("Grid");
            //grid.GetComponent<MeshCollider>().enabled = false;
            grid.name = string.Format("grid_{0}_{1}_{2}", info.q, info.r, info.heightOrder);
            grid.transform.SetParent(gridsParents[info.sceneId], true);
            GridHelper helper = grid.AddComponent<GridHelper>();
            //初始化tile瓦片所挂载的脚本helper上的信息
            helper.info = info;
            helper.info.q = info.q;
            helper.info.r = info.r;
            helper.info.s = -info.q - info.r;
            helper.info.heightOrder = info.heightOrder;
            helper.info.walkable = info.walkable;
            helper.info.dynamicLimit = info.dynamicLimit;
            allGrids.Add(helper);

            //初始化gridDic
            gridDic.Add(new Vector3(info.q, info.r, info.heightOrder), helper);

            //更新进度条
            progress = i / (float)table.gridInfos.Count * 50f;
            EditorUtility.DisplayProgressBar("Loading GridMap", $"Processing Loading Grid ({info.q},{info.r},{info.heightOrder})", progress);
        }
        //初始化每个GridHelper脚本上GridInfo类中的neighborGrids信息, 以及edgeGrids列表
        Vector3[] dirs = new Vector3[] {
            new Vector3(-1, 1, 0), Vector3.left, Vector3.down,
            new Vector3(1, -1, 0), Vector3.right, Vector3.up};
        foreach (Vector3 key in gridDic.Keys)
        {
            //gridDic[key].GetComponent<MeshCollider>().enabled = true;

            GridInfo info = gridDic[key].info;
            bool onEdge = false;
            for (int i = 0; i < 6; i++)
            {
                //如果dirs[i]方向存在网格 或者 在可以允许的高度差内但不同heightOrder的dirs[i]方向存在网格, 则加入neighborGrids中
                if (gridDic.ContainsKey(key + dirs[i]))
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i]]);
                }
                else if (gridDic.ContainsKey(key + dirs[i] + Vector3.forward)
                    && Mathf.Abs(info.centerPos.y - gridDic[key + dirs[i] + Vector3.forward].info.centerPos.y) < 0.3f)
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i] + Vector3.forward]);
                }
                else if (gridDic.ContainsKey(key + dirs[i] + Vector3.back)
                    && Mathf.Abs(info.centerPos.y - gridDic[key + dirs[i] + Vector3.back].info.centerPos.y) < 0.3f)
                {
                    gridDic[key].neighborGrids.Add(gridDic[key + dirs[i] + Vector3.back]);
                }
                //如果dirs[i]方向不存在网格, 说明该网格处于边缘, 将其加入到edgeGrids中
                else
                {
                    gridDic[key].neighborGrids.Add(null);
                    onEdge = true;
                }
                if (onEdge)
                {
                    edgeGrids.Add(gridDic[key]);
                }
            }
            //更新进度条
            progress = 50 + info.id / (float)gridDic.Count * 50f;
            EditorUtility.DisplayProgressBar("Loading GridMap", $"Processing Build Neighborhood ({info.q},{info.r},{info.heightOrder})", progress);
        }
        EditorUtility.ClearProgressBar(); // 完成后清除进度条

        if (allGrids.Count != 0)
        {
            Debug.Log($"Load Finished! You have loaded {allGrids.Count} gridInfos from the GridMapConfigTable");
        }
        table.gridInfos.Clear();
        table.gridInfos = null;
        table = null;

        gridMapInitialized = true;
    }

    /// <summary>
    /// 需要再写一个用于玩家加载存档的重载
    /// </summary>
    public void LoadFormConfigTable(string path)
    {

    }

    /// <summary>
    /// 删除PersistentData中的GridMapConfigTable, 用于玩家删除存档
    /// </summary>
    public void DeletaConfigTable(string path)
    {

    }
}
