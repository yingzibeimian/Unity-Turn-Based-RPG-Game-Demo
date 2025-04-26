using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��С�ѣ�Min-Heap��ʵ�֣����ڸ�Ч�ػ�ȡ��ɾ����СԪ�ء�
/// ���Ͳ��� T ����ʵ�� IComparable<T> �ӿ�, �Ա�Ƚ�Ԫ�ش�С
/// </summary>
/// <typeparam name="T">���д洢Ԫ������</typeparam>
public class MinHeap<T> where T : IComparable<T>
{
    private List<T> heap = new List<T>();

    public int Count => heap.Count;

    /// <summary>
    /// ��Ԫ�����, ʱ�临�Ӷ�O(logn)
    /// </summary>
    /// <param name="item"></param>
    public void Enqueue(T item)
    {
        heap.Add(item); //����Ԫ�ز��뵽ĩβ
        int i = Count - 1;
        //����Ԫ���ϸ�������λ��
        while(i > 0)
        {
            int parent = (i - 1) / 2; //���и��ڵ�����
            //�����ǰԪ�ش��ڻ���ڸ��ڵ�, ������������, �˳�ѭ��
            if (heap[i].CompareTo(heap[parent]) >= 0)
            {
                break;
            }
            Swap(i, parent); //���򽻻���ǰԪ���븸�ڵ�
            i = parent; //�����ϸ�
        }
    }

    /// <summary>
    /// ���ضѶ�(��С)Ԫ��, ʱ�临�Ӷ�O(logn)
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T Dequeue()
    {
        if(Count == 0)
        {
            throw new InvalidOperationException("Heap is empty");
        }
        T result = heap[0]; //��ȡ�Ѷ�Ԫ��(��СԪ��)
        int last = Count - 1;
        heap[0] = heap[last]; //���ѵ����һ��Ԫ���ƶ����Ѷ�
        heap.RemoveAt(last);
        int i = 0;
        //�����ѽṹ, ���µĶѶ�Ԫ���³�������λ��
        while(true)
        {
            //���ӽڵ�����ӽڵ������
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            if(left >= Count)
            {
                break;
            }
            //�ҵ����ҽڵ��С��һ��
            int minChild = (right < Count && heap[right].CompareTo(heap[left]) < 0 ? right : left);
            //�����ǰԪ��С�ڻ������С�ӽڵ�, ������������, �˳�ѭ��
            if (heap[i].CompareTo(heap[minChild]) <= 0)
            {
                break;
            }
            Swap(i, minChild); //���򽻻���ǰԪ������С�ӽڵ�
            i = minChild; //�����³�
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
