using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform.Samples.VrHoops;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MKPlayerController : MonoBehaviour
{
    [SerializeField] private MainCamera mainCamera;
    [SerializeField] private float baseSpeed = 1.2f;
    [SerializeField] private float mouseSensitivity = 8f;
    [SerializeField] private GameObject spotLight;
    public Image light_img;
    public Sprite button_light_on;
    public Sprite button_light_off;

    private float _rotationX;
    private float maxHeight;


    private void Start()
    {
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        maxHeight = mainCamera.WaterHeight + 0.2f;
    }

    void Update()
    {
        if (IGMenuController.isPaused) return;
        
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) *
                            (Time.deltaTime * baseSpeed));

        if (transform.position.y > maxHeight)
            transform.position = new Vector3(transform.position.x, 11.2f, transform.position.z);

        if (Input.GetButtonDown("Toggle Light"))
        {
            spotLight.SetActive(!spotLight.activeSelf);
            if (spotLight.activeSelf)
                light_img.sprite = button_light_on;
            else
                light_img.sprite = button_light_off;
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
        _rotationX = Mathf.Clamp(_rotationX, -89, 89);

        transform.localEulerAngles = new Vector3(-_rotationX, rotationY, 0);
    }
}