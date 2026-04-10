using UnityEngine;
using Mirror;

public class MouseLook : NetworkBehaviour
{
    public float sensitivity = 100f;
    private float xRot = 0f;
    private Transform playerBody;
    private InputActions controls;

    void Awake()
    {
        playerBody = transform.parent;
        controls = new InputActions();
    }
    
    /*public override void OnStartLocalPlayer()
    {
        if (!isLocalPlayer)
        {
            
        }
    }*/

    void Update()
    {
        if (isLocalPlayer)
        {
            Vector2 look = controls.Player.Look.ReadValue<Vector2>();
            float mouseX = look.x * sensitivity * Time.deltaTime;
            float mouseY = look.y * sensitivity * Time.deltaTime;

            xRot -= mouseY;
            xRot = Mathf.Clamp(xRot, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            GetComponent<Camera>().enabled = false;
        }
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
