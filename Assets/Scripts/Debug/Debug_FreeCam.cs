using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_FreeCam : MonoBehaviour
{
    public float baseSpeed = 2f;
    public float scrollSpeedMultiplier = 0.2f;
    public float mousePressSpeedMultiplier = 10f;
    public float mouseSensitivity = 8f;
    public KeyCode speedReset;
    public CursorLockMode cursorLockMode;

    private float currentSpeed;

    private float _rotationX;

    void Start()
    {
        currentSpeed = baseSpeed;
        Cursor.lockState = cursorLockMode;
        GetComponent<Rigidbody>().isKinematic = true;
    }

    void Update()
    {
        Vector2 scrollDelta = Input.mouseScrollDelta;

        currentSpeed = Mathf.Max(currentSpeed + scrollDelta.y * scrollSpeedMultiplier, 0);

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        bool mousePress = Input.GetMouseButton(0);

        transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) *
                            (Time.deltaTime * currentSpeed * (mousePress ? mousePressSpeedMultiplier : 1)));

        if (Input.GetKeyDown(speedReset))
        {
            currentSpeed = baseSpeed;
        }


        MouseLook();
    }


    private void MouseLook()
    {
        var rotationHorizontal = mouseSensitivity * Input.GetAxis("Mouse X");
        var rotationVertical = mouseSensitivity * Input.GetAxis("Mouse Y");
        
        transform.Rotate(Vector3.up * rotationHorizontal, Space.World);

        var rotationY = transform.localEulerAngles.y;

        _rotationX += rotationVertical;
        _rotationX = Mathf.Clamp(_rotationX, -60, 60);

        transform.localEulerAngles = new Vector3(-_rotationX, rotationY, 0);
    }
}