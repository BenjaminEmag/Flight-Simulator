using UnityEngine;

public class OBB : PhysicsCollider
{
    public Vector3 halfExtents;

    public Vector3 ToLocal(Vector3 globalPoint) => transform.InverseTransformPoint(globalPoint);
    public Vector3 ToGlobal(Vector3 localPoint) => transform.TransformPoint(localPoint);

    public override Shape shape => Shape.OBB;

    public Vector3[] GetVertices()
    {
        Vector3[] localCorners = new Vector3[8]
        {
        new Vector3(halfExtents.x, halfExtents.y, halfExtents.z),
        new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z),
        new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z),
        new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z),
        new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z),
        new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z),
        new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z),
        new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z)
        };

        Vector3[] worldCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldCorners[i] = transform.TransformPoint(localCorners[i]);
        }

        return worldCorners;
    }

    public Vector3[] GetAxes()
    {
        return new Vector3[]
        {
            transform.right,
            transform.up,
            transform.forward
        };
    }

    /*
        // Chatgpt did this for debugging purposes :)
        // Gizmo for visualizing the OBB
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;

            // Get the vertices of the OBB in world space
            Vector3[] vertices = GetVertices();

            // Draw the 12 edges of the OBB
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(vertices[i], vertices[(i + 1) % 4]);             // Top face
                Gizmos.DrawLine(vertices[i + 4], vertices[(i + 1) % 4 + 4]);     // Bottom face
                Gizmos.DrawLine(vertices[i], vertices[i + 4]);                   // Connect top and bottom faces
            }

            // Top face edges
            Gizmos.DrawLine(vertices[0], vertices[1]);
            Gizmos.DrawLine(vertices[1], vertices[2]);
            Gizmos.DrawLine(vertices[2], vertices[3]);
            Gizmos.DrawLine(vertices[3], vertices[0]);

            // Bottom face edges
            Gizmos.DrawLine(vertices[4], vertices[5]);
            Gizmos.DrawLine(vertices[5], vertices[6]);
            Gizmos.DrawLine(vertices[6], vertices[7]);
            Gizmos.DrawLine(vertices[7], vertices[4]);

            // Draw axes lines to show orientation
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * halfExtents.x);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * halfExtents.y);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * halfExtents.z);
        }
    */
}
