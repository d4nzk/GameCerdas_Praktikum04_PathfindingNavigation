using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTargetMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        if (Keyboard.current == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }
}