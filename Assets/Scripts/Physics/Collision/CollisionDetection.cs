using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static PhysicsCollider;

public static class CollisionDetection
{
    public static int CollisionChecks;
    public static float threshold = 0.01f;
    public struct VectorDeltas
    {
        public Vector3 s1;
        public Vector3 s2;
        public static VectorDeltas zero
        {
            get
            {
                return new VectorDeltas { s1 = Vector3.zero, s2 = Vector3.zero };
            }
        }

        public void ApplyToPosition(PhysicsCollider s1, PhysicsCollider s2)
        {
            s1.position += this.s1;
            s2.position += this.s2;
        }

        public void ApplyToVelocity(PhysicsCollider s1, PhysicsCollider s2)
        {
            s1.velocity += this.s1;
            s2.velocity += this.s2;
        }
    };

    public class CollisionInfo
    {
        public bool shouldPropogate = false;
        public Vector3 normal = Vector3.zero;
        public float penetration = 0;
        public float pctToMoveS1 = 0;
        public float pctToMoveS2 = 0;
        public float separatingVelocity = 0;
        public bool IsColliding => penetration > 0;
        public bool HasInfiniteMass => pctToMoveS1 + pctToMoveS2 == 0;

        public Vector3 torqueS1 = Vector3.zero;
        public Vector3 torqueS2 = Vector3.zero;
        public Vector3 angularVelocityS1 = Vector3.zero;
        public Vector3 angularVelocityS2 = Vector3.zero;
    }

    public delegate void NormalAndPenCalculation(PhysicsCollider s1, PhysicsCollider s2, out Vector3 normal, out float penetration);

    public static NormalAndPenCalculation[,] collisionFns = new NormalAndPenCalculation[(int)Shape.Count, (int)Shape.Count];

    static CollisionDetection()
    {
        collisionFns = new NormalAndPenCalculation[(int)Shape.Count, (int)Shape.Count];
        for (int i = 0; i < (int)Shape.Count; i++)
        {
            for (int j = 0; j < (int)Shape.Count; j++)
            {
                collisionFns[i, j] = (PhysicsCollider _, PhysicsCollider _, out Vector3 _, out float _) => throw new NotImplementedException();
            }
        }

        collisionFns[(int)Shape.Sphere, (int)Shape.Sphere] = TestSphereSphere;
        AddCollisionFns(Shape.Sphere, Shape.Plane, TestSpherePlane);

        AddCollisionFns(Shape.Sphere, Shape.AABB, TestSphereAABB);
        AddCollisionFns(Shape.Sphere, Shape.OBB, TestSphereOBB);
        AddCollisionFns(Shape.OBB, Shape.OBB, TestOBBOBB);

        NormalAndPenCalculation nop = (PhysicsCollider _, PhysicsCollider _, out Vector3 n, out float p) => { n = Vector3.zero; p = -1; };
        AddCollisionFns(Shape.AABB, Shape.Plane, nop);
        AddCollisionFns(Shape.AABB, Shape.OBB, nop);
        AddCollisionFns(Shape.Plane, Shape.Plane, nop);
        AddCollisionFns(Shape.AABB, Shape.AABB, nop);
    }

    static void AddCollisionFns(Shape s1, Shape s2, NormalAndPenCalculation fn)
    {
        NormalAndPenCalculation backwardsFn =
            (PhysicsCollider a, PhysicsCollider b, out Vector3 c, out float d) =>
            {
                fn(b, a, out c, out d);
                c = -c;
            };

        collisionFns[(int)s1, (int)s2] = fn;
        collisionFns[(int)s2, (int)s1] = backwardsFn;
    }

    public static void TestSphereSphere(PhysicsCollider shape1, PhysicsCollider shape2, out Vector3 normal, out float penetration)
    {
        Sphere s1 = shape1 as Sphere;
        Sphere s2 = shape2 as Sphere;

        Vector3 s2ToS1 = s1.Center - s2.Center;
        float dist = s2ToS1.magnitude;
        float sumOfRadii = (s1.Radius + s2.Radius);
        penetration = sumOfRadii - dist;
        normal = dist == 0 ? Vector3.zero : (s2ToS1 / dist);
    }

    public static void TestSpherePlane(PhysicsCollider s1, PhysicsCollider s2, out Vector3 normal, out float penetration)
    {
        Sphere s = s1 as Sphere;
        PlaneCollider p = s2 as PlaneCollider;

        float offset = Vector3.Dot(s.Center, p.Normal) - p.Offset;
        float dist = Mathf.Abs(offset);
        penetration = s.Radius - dist;
        normal = offset >= 0 ? p.Normal : -p.Normal;
    }

    public static void TestSphereAABB(PhysicsCollider s1, PhysicsCollider s2, out Vector3 normal, out float penetration)
    {
        Sphere s = s1 as Sphere;
        AABB b = s2 as AABB;
        Vector3 closestPoint = s.Center;
        closestPoint = Vector3.Min(closestPoint, b.max);
        closestPoint = Vector3.Max(closestPoint, b.min);
        Vector3 n = s.Center - closestPoint;
        float dist = n.magnitude;
        normal = dist == 0 ? Vector3.zero : (n / dist);
        penetration = s.Radius - dist;
    }
    public static void TestSphereOBB(PhysicsCollider s1, PhysicsCollider s2, out Vector3 normal, out float penetration)
    {
        Sphere s = s1 as Sphere;
        OBB b = s2 as OBB;
        Vector3 localCenter = b.ToLocal(s.Center);
        Vector3 maxes = b.halfExtents;
        Vector3 mins = -b.halfExtents;
        Vector3 localClosestPoint = localCenter;
        localClosestPoint = Vector3.Min(localClosestPoint, maxes);
        localClosestPoint = Vector3.Max(localClosestPoint, mins);
        Vector3 localN = localCenter - localClosestPoint;
        Vector3 n = b.transform.TransformVector(localN);
        float dist = n.magnitude;
        normal = dist == 0 ? Vector3.zero : (n / dist);
        penetration = s.Radius - dist;
    }

    public static void TestOBBOBB(PhysicsCollider s1, PhysicsCollider s2, out Vector3 normal, out float penetration)
    {
        OBB o1 = s1 as OBB;
        OBB o2 = s2 as OBB;

        if (o1 == null || o2 == null)
        {
            normal = Vector3.zero;
            penetration = 0;
            return;
        }

        Vector3[] vertices1 = o1.GetVertices();
        Vector3[] vertices2 = o2.GetVertices();

        List<Vector3> axes = GetSeparatingAxes(o1, o2);

        float minPenetration = float.MaxValue;
        Vector3 bestNormal = Vector3.zero;

        foreach (Vector3 axis in axes)
        {
            ProjectVerticesOntoAxis(vertices1, axis, out float min1, out float max1);
            ProjectVerticesOntoAxis(vertices2, axis, out float min2, out float max2);

            if (max1 < min2 || max2 < min1)
            {
                normal = Vector3.zero;
                penetration = 0;
                return;
            }

            float overlap = Mathf.Min(max1, max2) - Mathf.Max(min1, min2);

            if (overlap < minPenetration)
            {
                minPenetration = overlap;
                bestNormal = axis;
            }
        }
        normal = bestNormal.normalized;
        penetration = minPenetration;
    }

    private static List<Vector3> GetSeparatingAxes(OBB o1, OBB o2)
    {
        List<Vector3> axes = new List<Vector3>();

        axes.AddRange(o1.GetAxes());
        axes.AddRange(o2.GetAxes());

        Vector3[] o1Axes = o1.GetAxes();
        Vector3[] o2Axes = o2.GetAxes();

        foreach (Vector3 axis1 in o1Axes)
        {
            foreach (Vector3 axis2 in o2Axes)
            {
                Vector3 crossAxis = Vector3.Cross(axis1, axis2);
                if (crossAxis.sqrMagnitude > 1e-6f) // Avoid near-zero axes
                {
                    axes.Add(crossAxis.normalized);
                }
            }
        }

        return axes;
    }

    private static void ProjectVerticesOntoAxis(Vector3[] vertices, Vector3 axis, out float min, out float max)
    {
        axis.Normalize();
        min = max = Vector3.Dot(vertices[0], axis);

        for (int i = 1; i < vertices.Length; i++)
        {
            float projection = Vector3.Dot(vertices[i], axis);
            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }
    }


    public static CollisionInfo GetCollisionInfo(PhysicsCollider s1, PhysicsCollider s2)
    {
        CollisionInfo info = new CollisionInfo();
        NormalAndPenCalculation calc = collisionFns[(int)s1.shape, (int)s2.shape];

        try
        {
            calc(s1, s2, out info.normal, out info.penetration);
        }
        catch (NotImplementedException e)
        {
            Debug.Log($"Tried to test collision between {s1.shape} and {s2.shape}, but no collision detection function was found.");
            throw e;
        }

        {
            float sumOfInvMasses = s1.invMass + s2.invMass;
            if (sumOfInvMasses == 0) return info;
            info.pctToMoveS1 = s1.invMass / sumOfInvMasses;
            info.pctToMoveS2 = s2.invMass / sumOfInvMasses;

            info.separatingVelocity = Vector3.Dot(s1.velocity - s2.velocity, info.normal);

            Vector3 relativePositionS1 = s1.position - s1.centerOfMass;
            Vector3 relativePositionS2 = s2.position - s2.centerOfMass;

            info.torqueS1 = Vector3.Cross(relativePositionS1, info.normal * info.penetration);
            info.torqueS2 = Vector3.Cross(relativePositionS2, -info.normal * info.penetration);
        }

        if (s1.CompareTag("PlanePart") || s2.CompareTag("PlanePart"))
        {
            info.shouldPropogate = true;
        }

        return info;
    }

    public static void ApplyCollisionResolution(PhysicsCollider collider, Vector3 normal, Vector3 torque, float penetration)
    {
        if (penetration <= 0) return;

        Vector3 positionAdjustment = normal * penetration;
        collider.position += positionAdjustment;

        float separatingVelocity = Vector3.Dot(collider.velocity, normal);

        if (separatingVelocity < 0)
        {
            collider.velocity -= separatingVelocity * normal;
        }

        if (torque != Vector3.zero)
        {
            var particle = collider.GetComponent<Particle3D>();
            if (particle != null)
            {
                Vector3 angularAcceleration = torque * particle.inverseInertia;

                particle.AddTorque(angularAcceleration);
            }
        }
    }
}
