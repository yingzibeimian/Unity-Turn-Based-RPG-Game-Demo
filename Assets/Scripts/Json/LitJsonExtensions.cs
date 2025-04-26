using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

/// <summary>
/// ��LitJson����չ
/// </summary>
public static class LitJsonExtensions
{
    public static void Vector3RegisterCustomConverters()
    {
        //Vector3 ���л�
        JsonMapper.RegisterExporter<Vector3>((v, writer) =>
        {
            writer.Write($"[{v.x}, {v.y}, {v.z}]");
        });

        //Vector3 �����л�
        JsonMapper.RegisterImporter<string, Vector3>(s =>
        {
            string[] values = s.Trim('[', ']').Split(',');
            return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
        });
    }
}
