using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ���л��ͷ����л�ʱ ��ȡ�ķ���
/// </summary>
public enum JsonType
{
    JsonUtility,
    LitJson
}

/// <summary>
/// ���л��ͷ����л�ʱ ѡ����ļ���
/// </summary>
public enum PathType
{
    Streaming,
    Persistent
}

/// <summary>
/// Json���ݹ����� ��Ҫ���ڽ���Json�����л�(�洢��Ӳ��)�ͷ����л�(��Ӳ���ж�ȡ���ڴ���)
/// </summary>
public class JsonManager
{
    //����ģʽ
    private static JsonManager instance = new JsonManager(); //˽�о�̬����
    public static JsonManager Instance => instance; //������̬����

    private JsonManager() //˽�й��캯��, �����ⲿ���и���ʵ����
    {

    }

    /// <summary>
    /// ���л� �洢Json����
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fileNmae"></param>
    /// <param name="type"></param>
    public void SaveData(object data, string fileName, PathType pathType, JsonType type = JsonType.LitJson)
    {
        //ȷ���洢·��
        string pathStr = "";
        if(pathType == PathType.Streaming)
        {
            pathStr = Application.streamingAssetsPath + "/" + fileName + ".json";
        }
        else if(pathType == PathType.Persistent)
        {
            pathStr = Application.persistentDataPath + "/" + fileName + ".json";
        }
        //���л� �õ�Json�ַ���
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
        //�����л���Json�ַ��� �洢��ָ��·�����ļ���
        File.WriteAllText(pathStr, jsonStr);
    }

    /// <summary>
    /// �����л� ��ָ��Json�ļ��ж�ȡ���ݵ��ڴ���
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fileName"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public T LoadData<T>(string fileName, PathType pathType, JsonType type = JsonType.LitJson) where T : new()
    {
        //�Զ����ж�streamingAssetsPath, ���ж�persistentDataPath
        /*
        //ȷ�����ĸ�·����ȡ
        //�������ж� Ĭ�������ļ������Ƿ���������Ҫ������ ����� �ʹ��л�ȡ
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        //���ж� �Ƿ��������ļ�
        if(!File.Exists(path))
        {
            //���������Ĭ���ļ� �ʹ� ��д�ļ�����ȥѰ��
            path = Application.persistentDataPath + "/" + fileName + ".json";
            if(!File.Exists(path))
            {
                //�����д�ļ��л�û��
                Debug.LogAssertion("�����ڵ�ǰ�ļ�");
                return new T();
            }
        }
        */
        //�ֶ��ж�·��
        string pathStr = "";
        if (pathType == PathType.Streaming)
        {
            pathStr = Application.streamingAssetsPath + "/" + fileName + ".json";
        }
        else if (pathType == PathType.Persistent)
        {
            pathStr = Application.persistentDataPath + "/" + fileName + ".json";
        }

        //���з����л�
        string jsonStr = File.ReadAllText(pathStr);
        //���ݶ���
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
        //�Ѷ��󷵻س�ȥ
        return data;
    }
}
