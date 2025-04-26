using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

/// <summary>
/// 对LitJson的扩展
/// </summary>
public static class LitJsonExtensions
{
    public static void Vector3RegisterCustomConverters()
    {
        //Vector3 序列化
        JsonMapper.RegisterExporter<Vector3>((v, writer) =>
        {
            writer.Write($"[{v.x}, {v.y}, {v.z}]");
        });

        //Vector3 反序列化
        JsonMapper.RegisterImporter<string, Vector3>(s =>
        {
            string[] values = s.Trim('[', ']').Split(',');
            return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
        });
    }
}
