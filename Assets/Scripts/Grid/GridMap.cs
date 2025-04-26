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
/// ΪUnity��Vector3д����չ����
/// </summary>
public static class Vector3Extensions
{
    /// <summary>
    /// ���ص�����start��Ŀ��end��������XZƽ���ϵľ���
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

//�������л��ͷ����л���������
public class GridsConfigTable
{
    public List<GridInfo> gridInfos = new List<GridInfo>();
}

[ExecuteInEditMode]
public class GridMap : MonoBehaviour
{
    public static GridMap Instance;

    [Title("Settings")]
    public bool showGrid = false; //��������߿���ʾ
    private bool lastShowGrid = false; //��¼��һ�ε� showGrid ״̬
    public bool showEdge = false; //�����������������߿���ʾ
    private bool lastShowEdge = false; //��¼��һ�ε� showEdge ״̬
    public bool showHelper = false; //��������Ƭ��ʾ
    private bool lastShowHelper = false; //��¼��һ�ε� showHelper ״̬

    public GameObject gridModel;

    [MinValue(0.1f), MaxValue(10.0f)]
    public float hexHalfSize = 0.5f; //����һ�볤�� 
    public float RayHeight = 100f; //���߼��߶�
    //public LayerMask ObstacleLayer; //�ϰ���㼶
    //public LayerMask BridgeLayer; //�Ų㼶

    [Range(10.0f, 90.0f)]
    public float MaxSlopeAngle = 45.0f; //���������������¶�
    private float tanSlopeAngle
    {
        get
        {
            return Mathf.Tan(Mathf.Deg2Rad* MaxSlopeAngle);
        }
    }
    [Range(0.0f, 1.0f)]
    public float MaxHeightDiff = 0.5f; //��������������߶Ȳ�

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
    public float startX; //��һ�������x����
    [HorizontalGroup("Start")]
    [Title("")]
    public float startZ; //��һ�������y����
    [HorizontalGroup("Num")]
    public int numX; //x�������������
    [HorizontalGroup("Num")]
    public int numZ; //z�������������
    public List<GridHelper> allGrids = new List<GridHelper>(); //����ϼ�

    [Title("Config")]
    [PropertyOrder(1)]
    public bool onlySaveWalkableGrids = true; //�Ƿ�ֻ�洢��������������

    [HideInInspector]
    public bool gridMapInitialized = false; //�������GridMap�Ƿ��Ѿ���ʼ�����

    //key:(Q,R,HeightOrder) value:Tile�����ص�GridHelper
    private Dictionary<Vector3, GridHelper> gridDic = new Dictionary<Vector3, GridHelper>();

    //������ʾ������
    private List<GridHelper> edgeGrids = new List<GridHelper>(); //��Ե���� 
    private List<LineRenderer> edgeRenderer = new List<LineRenderer>(); //��¼������Ⱦ����߿��LineRenderer
    private List<LineRenderer> gridRenderer = new List<LineRenderer>(); //��¼������Ⱦÿ�������LineRenderer

    //����
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
    /// ���inspector������(��Ҫ��Show��ر���)����
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

        if (!Application.isPlaying && showHelper != lastShowHelper) // ֻ�� showHelper �仯ʱ��ִ��
        {
            UpdateHelperVisibility();
            lastShowHelper = showHelper; // ���¼�¼��״̬
        }
    }

    /// <summary>
    /// �����������������Ŀɼ���
    /// </summary>
    private void UpdateGridOutlineVisibility()
    {
        Vector3 offset = new Vector3(0, 0.1f, 0);
        //���ز���
        Material outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        Material walkableMaterial = Resources.Load<Material>("Materials/walkableMaterial");
        Material unwalkableMaterial = Resources.Load<Material>("Materials/unwalkableMaterial");
        if (outlineMaterial == null || walkableMaterial == null || unwalkableMaterial == null)
        {
            Debug.Log("Material Lost!");
            return;
        }
        //��ѡ״̬
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
                //�����Ƿ������ѡ����Ⱦ����
                lr.material = grid.info.walkable ? walkableMaterial : unwalkableMaterial;

                lr.transform.SetParent(linesParents[info.sceneId]);
                lr.positionCount = 6;
                //offset.y = info.heightOrder == 0 ? 0.2f : 0.4f; //������������ʾ�ĸ�����
                lr.SetPositions(new Vector3[] { info.vertices[0] + offset,
                    info.vertices[1] + offset, info.vertices[2] + offset,
                    info.vertices[3] + offset, info.vertices[4] + offset,
                    info.vertices[5] + offset});
                gridRenderer.Add(lr);
            }
        }
        //�ǹ�ѡ״̬
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
    /// ������������������Ե�ߵĿɼ���
    /// </summary>
    private void UpdateEdgeOutlineVisibility()
    {
        Vector3 offset = new Vector3(0, 0.2f, 0);
        //��ѡ״̬
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
                        
                        edgeRenderer.Add(lr); //��¼����ʹ�õ�LineRenderer
                    }
                }
            }
        }
        //�ǹ�ѡ״̬
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
    /// ����ÿ��HexTile��Helper��Ƭ�ɼ���
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
    /// ͨ��x,y,z��Ѱ�Ҷ�Ӧ����
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
    /// �ṩ���ⲿ���������񷽷�
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
    /// ɨ�����, ͨ�����߼����������Ѱ·�ƶ�������
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
        tile.GetComponent<HexagonMeshGenerator>().hexSize = hexHalfSize; //��ʼ����Ƭ ���ô�С

        //���ĵ�����߼��Դ��
        Vector3 rayCenter = new Vector3(startX, RayHeight, startZ);
        //HexTile����������(q,r)
        int q = -1;
        int r = -1;
        
        int id = 0; //HexTile��Id
        float progress = 0.0f; //���ڸ��½�����
        float tanSlopeAngle = Mathf.Tan(Mathf.Deg2Rad * MaxSlopeAngle); //����¶ȵ�tanֵ
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

                //��¼���߼�⵽���ĵ�, ��Ϊ���ܴ�����, ������ͬһλ�õĲ�ͬ�߶ȿ��ܴ��ڶ�����ĵ�,
                //��List��¼��tile��GridInfo��Ϣ
                List<GridInfo> tileInfo = new List<GridInfo>();

                //�Ƿ����Bridge��, ��һ�δ������߼��ʱΪfalse, ��������,
                //����һ�μ�⵽Bridge��, ��ignoreBrige��Ϊtrue, ���еڶ������߼��, ���Һ���Bridge��
                bool ignoreBrige = false;

                //���߼��
                GridInfo gridInfo = getGridInfo(rayCenter, ref ignoreBrige);
                //����ⷵ�ز�Ϊnull, ˵����gridInfo.centerPos��������Ҫ����1��HexTile
                if(gridInfo != null)
                {
                    gridInfo.id = id++;
                    tileInfo.Add(gridInfo);
                    //��ignoreBrigeΪtrue, ˵��gridInfo����λ�õĲ㼶ΪBridge, ��Ҫ���еڶ������߼��
                    if (ignoreBrige == true)
                    {
                        GridInfo gridInfo2 = getGridInfo(rayCenter, ref ignoreBrige);
                        if(gridInfo2 != null) {
                            gridInfo2.id = id++;
                            tileInfo.Add(gridInfo2);
                        }
                    }
                }
                //����tileInfo�е�GridInfo��Ϣ����HexTeil, ����GridHelper�ű�,
                //�����뵽allGrids�б��gridDic�ֵ���
                for(int k = 0; k < tileInfo.Count; k++)
                {
                    //�����¶� �ж����ĵ���6���������γɵ����Ƿ��������
                    if (ExistSlopeLimit(tileInfo[k]))
                    {
                        tileInfo[k].walkable = false;
                        tileInfo[k].slopLimit = true;
                    }
                    //ʵ����grid����
                    GameObject grid = Instantiate(tile, tileInfo[k].centerPos, Quaternion.identity);
                    grid.layer = LayerMask.NameToLayer("Grid");

                    //�ؼ�����!!! 
                    //���û�����д���, ��ʵ����������grid�������߱�MeshCollider, ��Ӱ��������߼��
                    //�Ӷ��������е��θ߶�, ���߿��ܻ����Լ����ɵ�����, �����ǵ���, ��¼����Ķ���λ��
                    //�����ʵ�����µ�grid����ʱ, Ҫ�Ƚ�MeshCollider���ʧ��, ������ź��������߼��
                    //����������ɨ����Ϻ� �������� MeshCollider
                    grid.GetComponent<MeshCollider>().enabled = false;
                    
                    grid.name = string.Format("grid_{0}_{1}_{2}", q, r, k);
                    grid.transform.SetParent(gridsParents[tileInfo[k].sceneId], true);
                    GridHelper helper = grid.AddComponent<GridHelper>();
                    //��ʼ��tile��Ƭ�����صĽű�helper�ϵ���Ϣ
                    helper.info = tileInfo[k];
                    helper.info.q = q;
                    helper.info.r = r;
                    helper.info.s = -q - r;
                    helper.info.heightOrder = k;

                    allGrids.Add(helper);
                    gridDic.Add(new Vector3(q, r, k), helper);
                }
                //���½�����
                progress = (i * numX + j + 1) / (float)totalGrids * 50f;
                EditorUtility.DisplayProgressBar("Scanning Scene", $"Processing Scanning Grid ({i},{j})", progress);
            }
        }

        //����gridDic, ��ʼ��ÿ��GridHelper�ű���GridInfo���е�neighborGrids��Ϣ
        //��������ʱ, ����ǰ����(-x+z), ��vertex0��vertex1�����ɵ� edge0 �ķ��� ��ʱ�����,
        Vector3[] dirs = new Vector3[] {
            new Vector3(-1, 1, 0), Vector3.left, Vector3.down,
            new Vector3(1, -1, 0), Vector3.right, Vector3.up};
        foreach(Vector3 key in gridDic.Keys)
        {
            //����������ɨ����� ��������ÿ��Grid�� MeshCollider
            gridDic[key].GetComponent<MeshCollider>().enabled = true;

            GridInfo info = gridDic[key].info;
            bool onEdge = false;
            for(int i = 0; i < 6; i++)
            {
                //���ͬһheightOrder��dirs[i]����
                //���� �ڿ�������ĸ߶Ȳ��ڵ���ͬheightOrder��dirs[i]����(��Ҫ��������ͬ�����µ��浫��ͬheightOrder�������neighborGrids)
                //��������, �����neighborGrids��
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
                //���dirs[i]���򲻴�������, ˵���������ڱ�Ե, ������뵽edgeGrids��
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
            //���½�����
            progress = 50 + info.id / (float)gridDic.Count * 50f;
            EditorUtility.DisplayProgressBar("Scanning Scene", $"Processing Build Neighborhood ({info.q},{info.r},{info.heightOrder})", progress);
        }
        Debug.Log($"Scan Finished! You have created {allGrids.Count} grids on scene");
        EditorUtility.ClearProgressBar(); // ��ɺ����������
    }

    /// <summary>
    /// �����ĵ�rayCenter��6�����������߼��, ��������null, ��������ĵ��������HexTile
    /// </summary>
    /// <param name="rayCenter">���߼������ĵ�</param>
    /// <param name="IgnoreBridge">������true, ����Ҫ����Bridge��, �ٴν������߼��</param>
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
                //����õ�6����������߼��Դ��
                raySource = rayCenter + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * hexHalfSize;
            }
            RaycastHit hitInfo;
            //������߼�⵽collider
            if (Physics.Raycast(raySource, Vector3.down, out hitInfo, 150, layerMask))
            {
                //������߼����ײ�������ϰ���
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    gridInfo.walkable = false;
                    gridInfo.obstacleLimit = true;
                }
                //������߼����ײ��������, �ͽ�ignoreBridge��Ϊtrue, �õڶ��μ��ʱ����Bridge��
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Bridge"))
                {
                    ignoreBridge = true;
                }
                //�������ĵ�
                if (k == 0)
                {
                    gridInfo.centerPos = hitInfo.point + new Vector3(0, 0.02f, 0);
                    //�������ĵ��жϸ�������������id
                    for(int i = 0; i < scenes.Count; i++)
                    {
                        if(hitInfo.transform.parent == scenes[i])
                        {
                            gridInfo.sceneId = i;
                        }
                    }
                }
                //���ö���
                else
                {
                    gridInfo.vertices.Add(hitInfo.point + new Vector3(0, 0.02f, 0));
                }
            }
            //ֻҪ���ĵ������1������û�м�⵽collider
            else
            {
                return null;
            }
        }
        //������ĵ�����ж��㶼��⵽collider, �򷵻�������Ϣ
        return gridInfo;
    }

    /// <summary>
    /// �����¶��жϵ�ǰtile�Ƿ�������
    /// </summary>
    /// <returns></returns>
    private bool ExistSlopeLimit(GridInfo info)
    {
        Vector3 center = info.centerPos;
        float height = 0; //������Y���ϵĸ߶Ȳ�
        float distanceV2V_Near = sqrt3 * hexHalfSize; //���1����������������
        float distanceV2V_Far = 2 * hexHalfSize; //�ԽǶ�������������

        for (int i = 0; i < 6; i++)
        {
            //�ж��������ĵ��6����������� �� xzƽ�� �γɵĽǶ�
            height = Mathf.Abs(center.y - info.vertices[i].y);
            //�жϼн��Ƿ��������¶�, ���жϸ߶Ȳ��Ƿ񳬹����ƣ�С�²����£�
            if(height / hexHalfSize > tanSlopeAngle && height > MaxHeightDiff)
            {
                return true;
            }
            //�ж������������ڶ�������� �� xzƽ�� �γɵĽǶ�
            int next = (i + 1) % 6;
            height = Mathf.Abs(info.vertices[i].y - info.vertices[next].y);
            if (height / hexHalfSize > tanSlopeAngle && height > MaxHeightDiff)
            {
                return true;
            }
        }

        for(int i = 0; i < 3; i++)
        {
            for(int j = 2; j <= 4; j++) // j=2:���1������, j=3:�ԽǶ���, j=4:���1������
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
        //���е�֮������ߺ�XZƽ��ĽǶȶ�����������¶�, �򲻴���SlopeLimit
        return false;
    }

    /// <summary>
    /// �����ǰ�����ϵ���������
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
    /// �����������������ó�Json�ļ�, �洢��StreamingAssets��, ���ݽ�������Ϸ�״�����ʱ��������
    /// </summary>
    [HorizontalGroup("Config", Width = 0.5f)]
    [PropertyOrder(1)]
    [Button("Save")]
    public void SaveToStreaming()
    {
        //���ʹ��LitJson, ����ҪΪLitJson��չVector3�����л��ͷ����л�����
        //LitJsonExtensions.Vector3RegisterCustomConverters();
        
        //ʹ��JsonUtlity
        //��allGrids�е�����GridHelper�ű��е�GridInfo��Ϣ���л�
        GridsConfigTable table = new GridsConfigTable();
        foreach(GridHelper helper in allGrids)
        {
            helper.info.hadConfiguration = true; //���Ϊ�Ѿ��洢�����ñ���
            //����onlySaveWalkableGrids, ��ֻ�洢����������(dynamicLimit����)
            if (onlySaveWalkableGrids && !helper.info.slopLimit && !helper.info.obstacleLimit)
            {
                table.gridInfos.Add(helper.info);
            }
            //������onlySaveWalkableGrids
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
    /// �����������������ó�Json�ļ�, �洢��PersistentData��, ������Ҵ浵�͵��ô浵
    /// </summary>
    public void SaveToPesistent()
    {

    }


    /// <summary>
    /// ��Json�����ļ��ж�ȡ�������ݲ����ص�������
    /// </summary>
    [HorizontalGroup("Config", Width = 0.5f)]
    [PropertyOrder(1)]
    [Button("Load")]
    public void LoadFormConfigTable()
    {
        //����������Ѿ�������������, ��ȫ�����
        if(allGrids.Count != 0)
        {
            //���allGrids, gridDic, edgeGrids
            Clear();
            //������LineRenderer���ظ�Pool�����
            showGrid = false;
            showEdge = false;
            LineRendererPool.Instance.ClearAllLineRenderers();
        }
        //ʹ��JsonUtlity��LitJson�����Է����л����пո�ͻ��е�json����
        //ʹ��JsonUtlity
        GridsConfigTable table = new GridsConfigTable();
        table = JsonManager.Instance.LoadData<GridsConfigTable>("GridMap/GridMapConfigTable", 
            PathType.Streaming, JsonType.JsonUtility);
        //��������������������, ��Scan��������
        GameObject tile = gridModel;
        float progress = 0.0f;
        tile.GetComponent<HexagonMeshGenerator>().hexSize = hexHalfSize; //��ʼ����Ƭ ���ô�С
        for (int i = 0; i < table.gridInfos.Count; i++)
        {
            GridInfo info = table.gridInfos[i];
            //��ʼ��allGrids
            //ʵ����tile��Ƭ
            GameObject grid = Instantiate(tile, info.centerPos, Quaternion.identity);
            grid.layer = LayerMask.NameToLayer("Grid");
            //grid.GetComponent<MeshCollider>().enabled = false;
            grid.name = string.Format("grid_{0}_{1}_{2}", info.q, info.r, info.heightOrder);
            grid.transform.SetParent(gridsParents[info.sceneId], true);
            GridHelper helper = grid.AddComponent<GridHelper>();
            //��ʼ��tile��Ƭ�����صĽű�helper�ϵ���Ϣ
            helper.info = info;
            helper.info.q = info.q;
            helper.info.r = info.r;
            helper.info.s = -info.q - info.r;
            helper.info.heightOrder = info.heightOrder;
            helper.info.walkable = info.walkable;
            helper.info.dynamicLimit = info.dynamicLimit;
            allGrids.Add(helper);

            //��ʼ��gridDic
            gridDic.Add(new Vector3(info.q, info.r, info.heightOrder), helper);

            //���½�����
            progress = i / (float)table.gridInfos.Count * 50f;
            EditorUtility.DisplayProgressBar("Loading GridMap", $"Processing Loading Grid ({info.q},{info.r},{info.heightOrder})", progress);
        }
        //��ʼ��ÿ��GridHelper�ű���GridInfo���е�neighborGrids��Ϣ, �Լ�edgeGrids�б�
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
                //���dirs[i]����������� ���� �ڿ�������ĸ߶Ȳ��ڵ���ͬheightOrder��dirs[i]�����������, �����neighborGrids��
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
                //���dirs[i]���򲻴�������, ˵���������ڱ�Ե, ������뵽edgeGrids��
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
            //���½�����
            progress = 50 + info.id / (float)gridDic.Count * 50f;
            EditorUtility.DisplayProgressBar("Loading GridMap", $"Processing Build Neighborhood ({info.q},{info.r},{info.heightOrder})", progress);
        }
        EditorUtility.ClearProgressBar(); // ��ɺ����������

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
    /// ��Ҫ��дһ��������Ҽ��ش浵������
    /// </summary>
    public void LoadFormConfigTable(string path)
    {

    }

    /// <summary>
    /// ɾ��PersistentData�е�GridMapConfigTable, �������ɾ���浵
    /// </summary>
    public void DeletaConfigTable(string path)
    {

    }
}
