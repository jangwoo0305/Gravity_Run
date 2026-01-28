using UnityEngine;
using Random = UnityEngine.Random;

public class Fireball : MonoBehaviour
{
    enum Edge
    {
        Bottom,
        Right,
        Top,
        Left
    }

    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 4f;

    private Edge currentEdge;

    private float speed;
    private float heightLevel;     // 🔥 fireball 고유 높이

    private float minX, maxX, minY, maxY;
    private int edgeChangeCount;

    private const int EDGES_PER_LAP = 4;
    private const int MAX_LAP = 1;

    // 🔹 Pool에서 호출
    public void Init(float height)
    {
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);

        minX = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
        maxX = cam.ViewportToWorldPoint(new Vector3(1, 0, z)).x;
        minY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y;
        maxY = cam.ViewportToWorldPoint(new Vector3(0, 1, z)).y;

        heightLevel = height;
        speed = Random.Range(minSpeed, maxSpeed);

        edgeChangeCount = 0;
        currentEdge = Edge.Left;

        ApplyPositionByEdge();
        UpdateRotation();
    }

    void Update()
    {
        MoveClockwise();
        CheckEdgeChange();
    }

    void MoveClockwise()
    {
        Vector3 pos = transform.position;
        float delta = speed * Time.deltaTime;

        switch (currentEdge)
        {
            case Edge.Bottom:
                pos.x -= delta;
                pos.y = minY + heightLevel;
                break;

            case Edge.Right:
                pos.y -= delta;
                pos.x = maxX - heightLevel;
                break;

            case Edge.Top:
                pos.x += delta;
                pos.y = maxY - heightLevel;
                break;

            case Edge.Left:
                pos.y += delta;
                pos.x = minX + heightLevel;
                break;
        }

        transform.position = pos;
    }

    void CheckEdgeChange()
    {
        Vector3 pos = transform.position;

        switch (currentEdge)
        {
            case Edge.Bottom:
                if (pos.x <= minX)
                    ChangeEdge(Edge.Left);
                break;

            case Edge.Right:
                if (pos.y <= minY)
                    ChangeEdge(Edge.Bottom);
                break;

            case Edge.Top:
                if (pos.x >= maxX)
                    ChangeEdge(Edge.Right);
                break;

            case Edge.Left:
                if (pos.y >= maxY)
                    ChangeEdge(Edge.Top);
                break;
        }
    }

    void ChangeEdge(Edge next)
    {
        edgeChangeCount++;

        if (edgeChangeCount >= EDGES_PER_LAP * MAX_LAP)
        {
            gameObject.SetActive(false);
            return;
        }

        currentEdge = next;
        ApplyPositionByEdge();
        UpdateRotation();
    }

    // 🔥 edge에 따라 높이를 올바른 축에 투영
    void ApplyPositionByEdge()
    {
        Vector3 pos = transform.position;

        switch (currentEdge)
        {
            case Edge.Bottom:
                pos.y = minY + heightLevel;
                break;

            case Edge.Right:
                pos.x = maxX - heightLevel;
                break;

            case Edge.Top:
                pos.y = maxY - heightLevel;
                break;

            case Edge.Left:
                pos.x = minX + heightLevel;
                break;
        }

        transform.position = pos;
    }

    void UpdateRotation()
    {
        float angle = 0f;

        switch (currentEdge)
        {
            case Edge.Bottom: angle = 180f; break;
            case Edge.Right:  angle = -90f; break;
            case Edge.Top:    angle = 0f; break;
            case Edge.Left:   angle = 90f; break;
        }

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}