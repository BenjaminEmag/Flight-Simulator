using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AABB : PhysicsCollider
{
    public Vector3 min => position - (0.5f * transform.lossyScale);
    public Vector3 max => position + (0.5f * transform.lossyScale);
    public override Shape shape => Shape.AABB;
}
