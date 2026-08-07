using UnityEngine;

public static class EdgeMath
{
    // Player moves clockwise along the screen edges.
    public static Vector2 GetClockwiseMoveDir(Edge edge)
    {
        switch (edge)
        {
            case Edge.Bottom:
                return Vector2.right;
            case Edge.Right:
                return Vector2.up;
            case Edge.Top:
                return Vector2.left;
            case Edge.Left:
                return Vector2.down;
            default:
                return Vector2.zero;
        }
    }

    // Fireball moves counter-clockwise along the screen edges.
    public static Vector2 GetCounterClockwiseMoveDir(Edge edge)
    {
        switch (edge)
        {
            case Edge.Bottom:
                return Vector2.left;
            case Edge.Left:
                return Vector2.up;
            case Edge.Top:
                return Vector2.right;
            case Edge.Right:
                return Vector2.down;
            default:
                return Vector2.zero;
        }
    }

    public static Vector2 GetGravityDir(Edge edge)
    {
        switch (edge)
        {
            case Edge.Bottom:
                return Vector2.down;
            case Edge.Right:
                return Vector2.right;
            case Edge.Top:
                return Vector2.up;
            case Edge.Left:
                return Vector2.left;
            default:
                return Vector2.down;
        }
    }

    public static Edge GetPreviousEdgeClockwise(Edge current)
    {
        // Top -> Right, Right -> Bottom, Bottom -> Left, Left -> Top.
        switch (current)
        {
            case Edge.Top:
                return Edge.Right;
            case Edge.Right:
                return Edge.Bottom;
            case Edge.Bottom:
                return Edge.Left;
            case Edge.Left:
                return Edge.Top;
            default:
                return Edge.Bottom;
        }
    }

    public static Edge GetNextEdgeCounterClockwise(Edge current)
    {
        // Bottom -> Left -> Top -> Right -> Bottom.
        switch (current)
        {
            case Edge.Bottom:
                return Edge.Left;
            case Edge.Left:
                return Edge.Top;
            case Edge.Top:
                return Edge.Right;
            case Edge.Right:
                return Edge.Bottom;
            default:
                return Edge.Bottom;
        }
    }

    public static Edge GetOppositeEdge(Edge current)
    {
        switch (current)
        {
            case Edge.Bottom:
                return Edge.Top;
            case Edge.Right:
                return Edge.Left;
            case Edge.Top:
                return Edge.Bottom;
            case Edge.Left:
                return Edge.Right;
            default:
                return Edge.Top;
        }
    }

    public static float GetAngleForCounterClockwiseMotion(Edge edge)
    {
        // Matches GetCounterClockwiseMoveDir:
        // Bottom: left (180), Right: down (-90), Top: right (0), Left: up (90)
        switch (edge)
        {
            case Edge.Bottom:
                return 180f;
            case Edge.Right:
                return -90f;
            case Edge.Top:
                return 0f;
            case Edge.Left:
                return 90f;
            default:
                return 0f;
        }
    }

    public static Vector2 GetStartCornerForCounterClockwiseMotion(ScreenEdgeBounds b, Edge edge)
    {
        // Bottom: start at bottom-right -> move left
        // Left: start at bottom-left -> move up
        // Top: start at top-left -> move right
        // Right: start at top-right -> move down
        switch (edge)
        {
            case Edge.Bottom:
                return new Vector2(b.MaxX, b.MinY);
            case Edge.Left:
                return new Vector2(b.MinX, b.MinY);
            case Edge.Top:
                return new Vector2(b.MinX, b.MaxY);
            case Edge.Right:
                return new Vector2(b.MaxX, b.MaxY);
            default:
                return new Vector2(b.MaxX, b.MinY);
        }
    }

    public static Vector2 GetBlendedGravityDir(ScreenEdgeBounds b, Edge currentEdge, float cornerBlendDistance, Vector2 pos)
    {
        // Smoothly rotate gravity near corners to avoid abrupt velocity changes.
        switch (currentEdge)
        {
            case Edge.Bottom:
                if (pos.x > b.MaxX - cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(b.MaxX - cornerBlendDistance, b.MaxX, pos.x);
                    return Vector2.Lerp(Vector2.down, Vector2.right, t).normalized;
                }
                return Vector2.down;

            case Edge.Right:
                if (pos.y > b.MaxY - cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(b.MaxY - cornerBlendDistance, b.MaxY, pos.y);
                    return Vector2.Lerp(Vector2.right, Vector2.up, t).normalized;
                }
                return Vector2.right;

            case Edge.Top:
                if (pos.x < b.MinX + cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(b.MinX + cornerBlendDistance, b.MinX, pos.x);
                    return Vector2.Lerp(Vector2.up, Vector2.left, t).normalized;
                }
                return Vector2.up;

            case Edge.Left:
                if (pos.y < b.MinY + cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(b.MinY + cornerBlendDistance, b.MinY, pos.y);
                    return Vector2.Lerp(Vector2.left, Vector2.down, t).normalized;
                }
                return Vector2.left;
        }

        return Vector2.down;
    }
}
