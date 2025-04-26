using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最小堆（Min-Heap）实现，用于高效地获取和删除最小元素。
/// 泛型参数 T 必须实现 IComparable<T> 接口, 以便比较元素大小
/// </summary>
/// <typeparam name="T">堆中存储元素类型</typeparam>
public class MinHeap<T> where T : IComparable<T>
{
    private List<T> heap = new List<T>();

    public int Count => heap.Count;

    /// <summary>
    /// 新元素入队, 时间复杂度O(logn)
    /// </summary>
    /// <param name="item"></param>
    public void Enqueue(T item)
    {
        heap.Add(item); //将新元素插入到末尾
        int i = Count - 1;
        //将新元素上浮到合适位置
        while(i > 0)
        {
            int parent = (i - 1) / 2; //堆中父节点索引
            //如果当前元素大于或等于父节点, 堆性质已满足, 退出循环
            if (heap[i].CompareTo(heap[parent]) >= 0)
            {
                break;
            }
            Swap(i, parent); //否则交换当前元素与父节点
            i = parent; //继续上浮
        }
    }

    /// <summary>
    /// 返回堆顶(最小)元素, 时间复杂度O(logn)
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T Dequeue()
    {
        if(Count == 0)
        {
            throw new InvalidOperationException("Heap is empty");
        }
        T result = heap[0]; //获取堆顶元素(最小元素)
        int last = Count - 1;
        heap[0] = heap[last]; //将堆的最后一个元素移动到堆顶
        heap.RemoveAt(last);
        int i = 0;
        //调整堆结构, 将新的堆顶元素下沉到合适位置
        while(true)
        {
            //左子节点和右子节点的索引
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            if(left >= Count)
            {
                break;
            }
            //找到左右节点更小的一个
            int minChild = (right < Count && heap[right].CompareTo(heap[left]) < 0 ? right : left);
            //如果当前元素小于或等于最小子节点, 堆性质已满足, 退出循环
            if (heap[i].CompareTo(heap[minChild]) <= 0)
            {
                break;
            }
            Swap(i, minChild); //否则交换当前元素与最小子节点
            i = minChild; //继续下沉
        }
        return result;
    }
    
    private void Swap(int i, int j)
    {
        T temp = heap[i];
        heap[i] = heap[j];
        heap[j] = temp;
    }
}
