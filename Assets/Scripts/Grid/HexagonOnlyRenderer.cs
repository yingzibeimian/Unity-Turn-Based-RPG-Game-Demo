#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 该脚本用于为空对象动态创建一个半径为hexSize的正六边形网格, 且仅附加MeshRenderer脚本
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexagonOnlyRenderer : MonoBehaviour
{
    public float hexSize = 0.5f;
    private Mesh mesh;

    // Start is called before the first frame update
    void Awake()
    {
        GenerateHexagonMesh();
    }

#if UNITY_EDITOR
    void OnValidate() //在Inspector修改参数时自动更新网格
    {
        // 只有在编辑器模式下才会触发
        EditorApplication.delayCall += () =>
        {
            if (this != null) // 避免对象被销毁时报错
                GenerateHexagonMesh();
        };
    }
#endif

    private void GenerateHexagonMesh()
    {
        //确保组件存在
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();


        mesh = new Mesh();
        mf.mesh = mesh;

        Vector3[] vertices = new Vector3[7]; //6个顶点 + 1个中心点
        int[] triangles = new int[18]; //6个三角形面, 每个面用3个顶点作为索引

        //计算六边形顶点
        vertices[0] = Vector3.zero; //中心点
        for (int i = 1; i <= 6; i++)
        {
            float angle = (Mathf.PI / 3 * i) + Mathf.PI / 6;
            vertices[i] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * hexSize;
        }

        //生成三角形索引
        for (int i = 0; i < 6; i++)
        {
            triangles[i * 3] = 0; //中心点
            triangles[i * 3 + 1] = (i + 2) % 7 == 0 ? 1 : (i + 2);
            triangles[i * 3 + 2] = i + 1;
        }

        //赋值到mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); //计算法线以便能够正确渲染
        mesh.RecalculateBounds(); //计算包围盒(避免碰撞器问题)
    }
}
