using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Э�̹�����, ����Э�̳غ�Э�̶�����ɶ�UnityЭ�̵��ٷ�װ, ʵ����ͣЭ�̺Ͱ�����ִ��Э�̵Ĺ���
/// </summary>
public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager instance;
    public static CoroutineManager Instance => instance;

    //Э�̳� key:Э�̶������� value:Э�̶���
    private Dictionary<string, TaskGroup> taskGroups = new Dictionary<string, TaskGroup>();

    /// <summary>
    /// Э�̶�����
    /// </summary>
    public class TaskGroup
    {
        public List<CoroutineTask> tasks = new List<CoroutineTask>(); //Э�̶���
        public event Action OnComplete; //ִ������¼�, ������ִ����ϵ�һ���Իص�����(�����ڲ��Э�̶���ִ����Ϻ�, �ָ�����ͣ����Э��)

        public void TriggerOnComplete()
        {
            OnComplete?.Invoke();
            OnComplete = null;
        }
    }

    /// <summary>
    /// Э��������
    /// </summary>
    public class CoroutineTask
    {
        public string groupName; //Э�̶�������
        public IEnumerator routine; //Э�̷���
        public Coroutine handle; //Э�̾��, ���ڿ���Э�̵�������ֹͣ
        public bool isPaused; //��ͣ��־, ���ڿ���Э�̵���ͣ�ͻָ�
    }

    private void Awake() => instance = this;

    /// <summary>
    /// ���ݴ����Э�̷�������Э������task, ����task��ӵ�groupName��Ӧ��Э�̶��ж�β, ��󷵻�task
    /// </summary>
    /// <param name="routine"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    public void AddTaskToGroup(IEnumerator routine, string groupName)
    {
        //����Э������
        CoroutineTask task = new CoroutineTask
        {
            routine = routine,
            groupName = groupName
        };
        //�鿴�Ƿ���ڶ�ӦЭ�̶���
        if (!taskGroups.ContainsKey(groupName))
        {
            taskGroups[groupName] = new TaskGroup();
        }
        //��Э��������뵽��ӦЭ�̶�����
        taskGroups[groupName].tasks.Add(task);
    }

    /// <summary>
    /// ΪgroupName��Ӧ��Э�̶��а�ִ����ϻص�
    /// </summary>
    /// <param name="callback"></param>
    public void BindCallback(string groupName, Action callback)
    {
        if (!taskGroups.ContainsKey(groupName))
        {
            return;
        }
        taskGroups[groupName].OnComplete += callback;
    }

    public bool TaskInGroupIsEmpty(string groupName)
    {
        if (!taskGroups.ContainsKey(groupName))
        {
            return true;
        }
        return taskGroups[groupName].tasks.Count == 0;
    }

    /// <summary>
    /// �ṩ���ⲿ����groupName��Ӧ��Э�̶��еķ���
    /// </summary>
    /// <param name="groupName"></param>
    public void StartGroup(string groupName)
    {
        if (!taskGroups.ContainsKey(groupName))
        {
            return;
        }
        StartCoroutine(StartGroupAsync(taskGroups[groupName]));
    }

    /// <summary>
    /// ��˳��ִ��Э�̶����е�Э�̷���
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartGroupAsync(TaskGroup group)
    {
        List<CoroutineTask> tasks = group.tasks;
        while(tasks.Count > 0)
        {
            //Debug.Log($"��ʼִ������: {tasks[0].routine}");
            //tasks[0].handle = StartCoroutine(RunTask(tasks[0]));
            yield return tasks[0].handle = StartCoroutine(RunTask(tasks[0])); //��˳��ִ��groupЭ�̶����е�CoroutineTask����
            //Debug.Log($"�������: {tasks[0].routine}");
            if(tasks.Count > 0)
            {
                tasks.RemoveAt(0); //ִ����Ϻ�, ��task��Э�̶������Ƴ�
            }
        }
        group.TriggerOnComplete(); //������ɻص�
    }

    /// <summary>
    /// ���������߼�
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private IEnumerator RunTask(CoroutineTask task)
    {
        while (true)
        {
            if (task.isPaused) //���Э��������ͣ, ��һֱ�ȴ�
            {
                yield return null;
                continue;
            }

            if (!task.routine.MoveNext()) //���task��Э�̷���routine.MoveNext() == false, ˵��routineִ�����, ����ѭ��
                break;

            yield return task.routine.Current; //����routine�е�yield return, RunTaskҲ��yield return, �ֲ�ִ��routine����
        }
    }

    /// <summary>
    /// ��ͣgroupName��ӦЭ�̶����е�����Э��
    /// </summary>
    /// <param name="groupName"></param>
    public void PauseGroup(string groupName)
    {
        if(taskGroups.TryGetValue(groupName, out TaskGroup group))
        {
            foreach(CoroutineTask task in group.tasks)
            {
                task.isPaused = true;
            }
        }
    }

    /// <summary>
    /// �ָ�groupName��ӦЭ�̶����е�������ͣЭ��
    /// </summary>
    /// <param name="groupName"></param>
    public void ResumeGroup(string groupName)
    {
        if (taskGroups.TryGetValue(groupName, out TaskGroup group))
        {
            foreach (CoroutineTask task in group.tasks)
            {
                task.isPaused = false;
            }
        }
    }

    /// <summary>
    /// ֹͣgroupName��ӦЭ�̶����е�����Э��
    /// </summary>
    /// <param name="groupName"></param>
    public void StopGroup(string groupName)
    {
        if (taskGroups.TryGetValue(groupName, out TaskGroup group))
        {
            foreach (CoroutineTask task in group.tasks)
            {
                if (task != null && task.handle != null)
                {
                    StopCoroutine(task.handle);
                }
            }
            group.tasks.Clear();
        }
    }
}
