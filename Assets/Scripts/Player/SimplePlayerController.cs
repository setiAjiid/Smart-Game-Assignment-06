using UnityEngine;
using UnityEngine.InputSystem;

// Versi Input System baru. Project ini memakai "Active Input Handling = Input System Package (New)",
// sehingga Input.GetKey dari modul akan melempar InvalidOperationException.
public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
            horizontal = -1f;

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
            horizontal = 1f;

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
            vertical = 1f;

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
            vertical = -1f;

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical);

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        transform.position +=
            direction * moveSpeed * Time.deltaTime;
    }
}
