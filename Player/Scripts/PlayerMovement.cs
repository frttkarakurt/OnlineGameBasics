using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;

    [Header("Audio")]
    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public float footstepInterval = 0.4f;

    private CharacterController controller;
    private InputActions controls;
    private Vector3 velocity;
    private bool isGrounded;
    private float footstepTimer;

    // Prediction / Sync
    [SyncVar(hook = nameof(OnPositionSync))]
    private Vector3 syncPosition;
    private Vector3 targetPosition;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new InputActions();
    }

    public override void OnStartLocalPlayer()
    {
        controls.Enable();
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            HandleInput();
        }
        else
        {
            // Remote players: pozisyonu lerp ile takip et
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        }
    }

    void HandleInput()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool jumpPressed = controls.Player.Jump.triggered;

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);

        // Move vector
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        // Footstep client-side
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayLocalSound("footstep");        // hemen local olarak çal
                CmdPlaySound("footstep");          // diğer clientlara gönder
                footstepTimer = footstepInterval;
            }
        }

        // Jump
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayLocalSound("jump");                 // anında kendi sesi
            CmdPlaySound("jump");                   // diğer clientlar için RPC
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);

        // Send server authoritative position for others
        CmdSendPosition(transform.position);
    }

    // === Server Commands / ClientRPCs ===

    [Command]
    void CmdSendPosition(Vector3 pos)
    {
        // Server authoritative pozisyon
        syncPosition = pos;
    }

    [Command]
    void CmdPlaySound(string soundName)
    {
        RpcPlaySound(soundName);
    }

    [ClientRpc]
    void RpcPlaySound(string soundName)
    {
        if (isLocalPlayer) return; // local player sesi zaten çaldı
        PlayLocalSound(soundName);
    }

    void PlayLocalSound(string soundName)
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) return;

        switch (soundName)
        {
            case "footstep":
                if (footstepClip != null) audio.PlayOneShot(footstepClip);
                break;
            case "jump":
                if (jumpClip != null) audio.PlayOneShot(jumpClip);
                break;
        }
    }

    // SyncVar hook
    void OnPositionSync(Vector3 oldPos, Vector3 newPos)
    {
        targetPosition = newPos;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
