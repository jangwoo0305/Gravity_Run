using UnityEngine;

/// <summary>
/// World-space rectangular bounds computed from a camera viewport with an inward offset.
/// Used for "screen edge runner" style movement.
/// </summary>
public readonly struct ScreenEdgeBounds
{
    public float MinX { get; }
    public float MaxX { get; }
    public float MinY { get; }
    public float MaxY { get; }

    public ScreenEdgeBounds(float minX, float maxX, float minY, float maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }

    public static ScreenEdgeBounds FromCamera(Camera cam, float edgeOffset)
    {
        float z = cam.nearClipPlane;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float minX = bottomLeft.x + edgeOffset;
        float minY = bottomLeft.y + edgeOffset;
        float maxX = topRight.x - edgeOffset;
        float maxY = topRight.y - edgeOffset;

        return new ScreenEdgeBounds(minX, maxX, minY, maxY);
    }

    public Vector2 SnapToEdge(Edge edge, Vector2 pos)
    {
        switch (edge)
        {
            case Edge.Bottom:
                return new Vector2(pos.x, MinY);
            case Edge.Right:
                return new Vector2(MaxX, pos.y);
            case Edge.Top:
                return new Vector2(pos.x, MaxY);
            case Edge.Left:
                return new Vector2(MinX, pos.y);
            default:
                return pos;
        }
    }

    public bool IsBeyondEdge(Edge edge, Vector2 pos, float epsilon = 0f)
    {
        switch (edge)
        {
            case Edge.Bottom:
                return pos.y <= MinY + epsilon;
            case Edge.Right:
                return pos.x >= MaxX - epsilon;
            case Edge.Top:
                return pos.y >= MaxY - epsilon;
            case Edge.Left:
                return pos.x <= MinX + epsilon;
            default:
                return false;
        }
    }
}

