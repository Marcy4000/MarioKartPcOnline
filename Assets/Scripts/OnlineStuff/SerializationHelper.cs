using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SerializationHelper
{
    public static byte[] SerializeVector3(Vector3 vector)
    {
        SerVector3 vec = new SerVector3{
            x = vector.x, y = vector.y, z = vector.z
        };

        using (MemoryStream memoryStream = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, vec);
            return memoryStream.ToArray();
        }
    }

    public static Vector3 DeserializeVector3(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
            IFormatter formatter = new BinaryFormatter();

            var deserialized = (SerVector3)formatter.Deserialize(memoryStream);
            Vector3 result = new Vector3 { 
                x = deserialized.x,
                y = deserialized.y,
                z = deserialized.z
            };
            
            return result;
        }
    }

    public static byte[] SerializeQuaternion(Quaternion quaternion)
    {
        SerQuaternion quat = new SerQuaternion
        {
            x = quaternion.x,
            y = quaternion.y,
            z = quaternion.z,
            w = quaternion.w
        };

        using (MemoryStream memoryStream = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, quat);
            return memoryStream.ToArray();
        }
    }

    public static Quaternion DeserializeQuaternion(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
            IFormatter formatter = new BinaryFormatter();

            var deserialized = (SerQuaternion)formatter.Deserialize(memoryStream);
            Quaternion result = new Quaternion
            {
                x = deserialized.x,
                y = deserialized.y,
                z = deserialized.z,
                w = deserialized.w
            };

            return result;
        }
    }
}

[Serializable]
public class SerVector3
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class SerQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
}
