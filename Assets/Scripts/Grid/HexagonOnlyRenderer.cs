#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ýű�����Ϊ�ն���̬����һ���뾶ΪhexSize��������������, �ҽ�����MeshRenderer�ű�
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
    void OnValidate() //��Inspector�޸Ĳ���ʱ�Զ���������
    {
        // ֻ���ڱ༭��ģʽ�²Żᴥ��
        EditorApplication.delayCall += () =>
        {
            if (this != null) // �����������ʱ����
                GenerateHexagonMesh();
        };
    }
#endif

    private void GenerateHexagonMesh()
    {
        //ȷ���������
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();


        mesh = new Mesh();
        mf.mesh = mesh;

        Vector3[] vertices = new Vector3[7]; //6������ + 1�����ĵ�
        int[] triangles = new int[18]; //6����������, ÿ������3��������Ϊ����

        //���������ζ���
        vertices[0] = Vector3.zero; //���ĵ�
        for (int i = 1; i <= 6; i++)
        {
            float angle = (Mathf.PI / 3 * i) + Mathf.PI / 6;
            vertices[i] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * hexSize;
        }

        //��������������
        for (int i = 0; i < 6; i++)
        {
            triangles[i * 3] = 0; //���ĵ�
            triangles[i * 3 + 1] = (i + 2) % 7 == 0 ? 1 : (i + 2);
            triangles[i * 3 + 2] = i + 1;
        }

        //��ֵ��mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); //���㷨���Ա��ܹ���ȷ��Ⱦ
        mesh.RecalculateBounds(); //�����Χ��(������ײ������)
    }
}
