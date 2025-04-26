using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 协程管理器, 利用协程池和协程队列完成对Unity协程的再封装, 实现暂停协程和按队列执行协程的功能
/// </summary>
public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager instance;
    public static CoroutineManager Instance => instance;

    //协程池 key:协程队列名称 value:协程队列
    private Dictionary<string, TaskGroup> taskGroups = new Dictionary<string, TaskGroup>();

    /// <summary>
    /// 协程队列类
    /// </summary>
    public class TaskGroup
    {
        public List<CoroutineTask> tasks = new List<CoroutineTask>(); //协程队列
        public event Action OnComplete; //执行完毕事件, 用来绑定执行完毕的一次性回调函数(用来在插队协程队列执行完毕后, 恢复被暂停的主协程)

        public void TriggerOnComplete()
        {
            OnComplete?.Invoke();
            OnComplete = null;
        }
    }

    /// <summary>
    /// 协程任务类
    /// </summary>
    public class CoroutineTask
    {
        public string groupName; //协程队列名称
        public IEnumerator routine; //协程方法
        public Coroutine handle; //协程句柄, 用于控制协程的启动和停止
        public bool isPaused; //暂停标志, 用于控制协程的暂停和恢复
    }

    private void Awake() => instance = this;

    /// <summary>
    /// 根据传入的协程方法创建协程任务task, 并将task添加到groupName对应的协程队列队尾, 最后返回task
    /// </summary>
    /// <param name="routine"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    public void AddTaskToGroup(IEnumerator routine, string groupName)
    {
        //创建协程任务
        CoroutineTask task = new CoroutineTask
        {
            routine = routine,
            groupName = groupName
        };
        //查看是否存在对应协程队列
        if (!taskGroups.ContainsKey(groupName))
        {
            taskGroups[groupName] = new TaskGroup();
        }
        //将协程任务加入到对应协程队列中
        taskGroups[groupName].tasks.Add(task);
    }

    /// <summary>
    /// 为groupName对应的协程队列绑定执行完毕回调
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
    /// 提供给外部开启groupName对应的协程队列的方法
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
    /// 按顺序执行协程队列中的协程方法
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartGroupAsync(TaskGroup group)
    {
        List<CoroutineTask> tasks = group.tasks;
        while(tasks.Count > 0)
        {
            //Debug.Log($"开始执行任务: {tasks[0].routine}");
            //tasks[0].handle = StartCoroutine(RunTask(tasks[0]));
            yield return tasks[0].handle = StartCoroutine(RunTask(tasks[0])); //按顺序执行group协程队列中的CoroutineTask任务
            //Debug.Log($"任务完成: {tasks[0].routine}");
            if(tasks.Count > 0)
            {
                tasks.RemoveAt(0); //执行完毕后, 将task从协程队列中移除
            }
        }
        group.TriggerOnComplete(); //触发完成回调
    }

    /// <summary>
    /// 核心运行逻辑
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private IEnumerator RunTask(CoroutineTask task)
    {
        while (true)
        {
            if (task.isPaused) //如果协程任务被暂停, 就一直等待
            {
                yield return null;
                continue;
            }

            if (!task.routine.MoveNext()) //如果task中协程方法routine.MoveNext() == false, 说明routine执行完毕, 跳出循环
                break;

            yield return task.routine.Current; //遇到routine中的yield return, RunTask也就yield return, 分步执行routine方法
        }
    }

    /// <summary>
    /// 暂停groupName对应协程队列中的所有协程
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
    /// 恢复groupName对应协程队列中的所有暂停协程
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
    /// 停止groupName对应协程队列中的所有协程
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
