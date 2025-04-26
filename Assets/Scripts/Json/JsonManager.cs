using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 序列化和反序列化时 采取的方案
/// </summary>
public enum JsonType
{
    JsonUtility,
    LitJson
}

/// <summary>
/// 序列化和反序列化时 选择的文件夹
/// </summary>
public enum PathType
{
    Streaming,
    Persistent
}

/// <summary>
/// Json数据管理类 主要用于进行Json的序列化(存储到硬盘)和反序列化(从硬盘中读取到内存中)
/// </summary>
public class JsonManager
{
    //单例模式
    private static JsonManager instance = new JsonManager(); //私有静态变量
    public static JsonManager Instance => instance; //公共静态属性

    private JsonManager() //私有构造函数, 避免外部进行更多实例化
    {

    }

    /// <summary>
    /// 序列化 存储Json数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fileNmae"></param>
    /// <param name="type"></param>
    public void SaveData(object data, string fileName, PathType pathType, JsonType type = JsonType.LitJson)
    {
        //确定存储路径
        string pathStr = "";
        if(pathType == PathType.Streaming)
        {
            pathStr = Application.streamingAssetsPath + "/" + fileName + ".json";
        }
        else if(pathType == PathType.Persistent)
        {
            pathStr = Application.persistentDataPath + "/" + fileName + ".json";
        }
        //序列化 得到Json字符串
        string jsonStr = "";
        switch(type)
        {
            case JsonType.JsonUtility:
                jsonStr = JsonUtility.ToJson(data, true);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        //把序列化的Json字符串 存储到指定路径的文件中
        File.WriteAllText(pathStr, jsonStr);
    }

    /// <summary>
    /// 反序列化 从指定Json文件中读取数据到内存中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fileName"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public T LoadData<T>(string fileName, PathType pathType, JsonType type = JsonType.LitJson) where T : new()
    {
        //自动先判断streamingAssetsPath, 再判断persistentDataPath
        /*
        //确定从哪个路径读取
        //首先先判断 默认数据文件夹中是否有我们想要的数据 如果有 就从中获取
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        //先判断 是否存在这个文件
        if(!File.Exists(path))
        {
            //如果不存在默认文件 就从 读写文件夹中去寻找
            path = Application.persistentDataPath + "/" + fileName + ".json";
            if(!File.Exists(path))
            {
                //如果读写文件中还没有
                Debug.LogAssertion("不存在当前文件");
                return new T();
            }
        }
        */
        //手动判断路径
        string pathStr = "";
        if (pathType == PathType.Streaming)
        {
            pathStr = Application.streamingAssetsPath + "/" + fileName + ".json";
        }
        else if (pathType == PathType.Persistent)
        {
            pathStr = Application.persistentDataPath + "/" + fileName + ".json";
        }

        //进行反序列化
        string jsonStr = File.ReadAllText(pathStr);
        //数据对象
        T data = default(T);
        switch (type)
        {
            case JsonType.JsonUtility:
                data = JsonUtility.FromJson<T>(jsonStr);
                break;
            case JsonType.LitJson:
                data = JsonMapper.ToObject<T>(jsonStr);
                break;
        }
        //把对象返回出去
        return data;
    }
}
