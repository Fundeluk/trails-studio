using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class MathHelper
{
    /// <summary>
    /// via https://discussions.unity.com/t/miss-using-vector-projection/687499/3
    /// </summary>    
    /// <returns></returns>
    public static Vector3 GetNearestPointOnLine(Vector3 start, Vector3 direction, Vector3 point)
    {
        direction.Normalize();
        var v = point - start;
        var d = Vector3.Dot(v, direction);
        return start + direction * d;
    }
}
