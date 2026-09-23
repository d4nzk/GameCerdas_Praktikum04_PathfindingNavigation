using System.Collections.Generic;
using UnityEngine;

public class AgentPathFollower : MonoBehaviour
{
    public AStarPathFinder pathfinder;

    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float waypointThreshold = 0.1f;

    private List<GridNode> path;
    private int currentIndex;

    private void Start()
    {
        if (pathfinder == null)
        {
            return;
        }

        RestartFromStart();
    }

    public void RestartFromStart()
    {
        if (pathfinder == null ||
            pathfinder.startMarker == null)
        {
            return;
        }

        transform.position =
            pathfinder.startMarker.position;
        path = pathfinder.currentPath;
        currentIndex = 0;
    }

    private void Update()
    {
        if (pathfinder != null &&
            path != pathfinder.currentPath)
        {
            path = pathfinder.currentPath;
            currentIndex = 0;
        }

        if (path == null ||
            path.Count == 0 ||
            currentIndex >= path.Count)
        {
            return;
        }

        Vector3 target =
            path[currentIndex].worldPosition;

        target.y = transform.position.y;

        Vector3 direction =
            target - transform.position;

        if (direction.magnitude <=
            waypointThreshold)
        {
            currentIndex++;
            return;
        }

        Vector3 moveDirection =
            direction.normalized;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

        if (moveDirection.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }
}