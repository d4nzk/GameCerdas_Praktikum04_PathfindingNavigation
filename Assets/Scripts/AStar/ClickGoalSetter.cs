using UnityEngine;
using UnityEngine.InputSystem;

public class ClickGoalSetter : MonoBehaviour
{
    public Camera mainCamera;
    public Transform goalMarker;
    public AStarPathFinder pathfinder;
    public AgentPathFollower agentFollower;

    public LayerMask groundMask;

    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f,
            groundMask))
        {
            goalMarker.position =
                hit.point + Vector3.up * 0.2f;

            pathfinder.FindPath();

            if (agentFollower != null)
            {
                agentFollower.RestartFromStart();
            }
        }
    }
}