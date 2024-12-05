using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtensions
{
    public static Vector3 New(Vector2 xy, float z)
    {
        return new Vector3(xy.x, xy.y, z);
    }

    public static Vector3 ToVector3(this Vector2 xy, float z)
    {
        return new Vector3(xy.x, xy.y, z);
    }
}
