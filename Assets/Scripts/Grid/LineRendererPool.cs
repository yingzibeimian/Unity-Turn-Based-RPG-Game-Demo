using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LineRenderer的对象池脚本, 负责LineRenderer对象的实例化、复用、回收
/// 注意场景上只能挂载一个该脚本, 否则会引发pool管理的混乱
/// </summary>
[ExecuteInEditMode]
public class LineRendererPool : MonoBehaviour
{
    public static LineRendererPool Instance;
    public GameObject lineRendererPrefab; //预设体
    private Queue<LineRenderer> pool = new Queue<LineRenderer>(); //对象池

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.Log("Ensure this script is attached to only one object.");
        }
    }

    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    public LineRenderer GetLineRenderer()
    {
        if(pool.Count > 0)
        {
            LineRenderer lineRenderer = pool.Dequeue();
            lineRenderer.gameObject.SetActive(true);
            return lineRenderer;
        }
        else
        {
            GameObject obj = Instantiate(lineRendererPrefab);
            return obj.GetComponent<LineRenderer>();
        }
    }

    public void ReturnLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.gameObject.SetActive(false);
        pool.Enqueue(lineRenderer);
    }

    [Button("Clear Pool")]
    public void ClearAllLineRenderers()
    {
        if (pool.Count == 0)
        {
            Debug.Log("No LineRenderer in the pool.\n" +
                "You may not have created any (never enabled showGrid/showEdge), " +
                "or all created ones are in use (both showGrid and showEdge are true).");
            return;
        }
        while (pool.Count > 0)
        {
            LineRenderer lr = pool.Dequeue();
            if (lr != null)
            {
                DestroyImmediate(lr.gameObject);
            }
        }
        pool.Clear();
        Debug.Log("Clear All Grids LineRenderers Finished!");
    }
}
