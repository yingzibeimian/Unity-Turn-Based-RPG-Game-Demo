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

    [Header("����λ��")]
    public Vector3 centerPos;
    //�����ĵ㿪ʼ, ��ǰ����(+z)�Ķ���Ϊvertex0, ��ʱ������Ϊvertex1,vertex2,vertex3,vertex4,vertex5
    //vertex0��vertex1���ɵı�Ϊedge0, ��ʱ������Ϊedge1, edge2, edge3, edge4, edge5
    public List<Vector3> vertices = new List<Vector3>();

    [Header("���ߴ���")]
    public float movePrice = 1;

    public bool walkable = true;
    //slopLimit��� ͨ��Scan�ж�Ϊ�¶ȹ��������, ������onlySaveWalkableGrids, �򲻻����л������ñ���
    public bool slopLimit = false;
    //obstacleLimit��� ͨ��Scan�ж�Ϊ���ϰ����ڵ�������, ������onlySaveWalkableGrids, �򲻻����л������ñ���
    public bool obstacleLimit = false;
    //edgeLimit��� ͨ���ֶ��ж�Ϊ��̬Limit������, ��Щ�������ͨ��һ��������Ϊwalkable, ��˻ᱻ�������л�
    public bool dynamicLimit = false;
}

[ExecuteInEditMode]
public class GridHelper : MonoBehaviour
{
    public GridInfo info = new GridInfo();

    [Header("���ڵ�λ")]
    public List<GridHelper> neighborGrids = new List<GridHelper>(); //���ں�����A*Ѱ·

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
