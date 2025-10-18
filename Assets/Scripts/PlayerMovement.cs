using System.Linq;
using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;
    private Vector3 moveDirection;

    [SerializeField]
    private float mouseSensitivity = 100f;

    [SerializeField]
    private Transform verticalRotator = null;

    [SerializeField]
    private float rotationSpeed = 0.1f;

    [SerializeField]
    private float rotationCatchUpSpeed = 1f;

    [SerializeField]
    private float maxChestTwist = 45f;

    [SerializeField]
    private float maxCatchUpSpeed = 5f;

    private readonly SyncVar<float> targetYRotation = new SyncVar<float>();
    private float predictedYRotation; // Client-side immediate rotation
    private float smoothedYRotation; // Visual smoothed rotation

    [SerializeField]
    private Transform groundCheck = null;

    [SerializeField]
    private float jumpForce = 5f;

    [SerializeField]
    private float groundRadius = 0.2f;

    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private Transform chestBone = null;

    private Animator animator;
    private NetworkAnimator netAnimator;
    private Rigidbody rb;

    private bool isGrounded = true;

    private float verticalRotation_x = 0;

    [SerializeField]
    private float interpolationSpeed = 15f;

    [SerializeField]
    private float syncInterval = 0.05f;
    private float lastSyncTime = 0f;

    public override void OnStartClient()
    {
        Cursor.lockState = CursorLockMode.Locked;
        verticalRotation_x = transform.rotation.eulerAngles.x;

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();

        if (verticalRotation_x > 180)
        {
            verticalRotation_x -= 360;
        }

        predictedYRotation = transform.rotation.eulerAngles.y;
        smoothedYRotation = predictedYRotation;
        targetYRotation.Value = predictedYRotation;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        moveDirection = new Vector3(horizontal, 0, vertical);
        Debug.Log(moveDirection);

        animator.SetFloat("MoveX", horizontal);
        animator.SetFloat("MoveY", vertical);
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        // transform.position += moveSpeed * Time.deltaTime * moveDirection;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * rotationSpeed;

        if (Mathf.Abs(mouseX) > 0.01f)
        {
            predictedYRotation += mouseX;
        }

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.01f;
        verticalRotation_x += -mouseY;
        verticalRotation_x = Mathf.Clamp(verticalRotation_x, -70, 40);
        verticalRotator.localRotation = Quaternion.Euler(verticalRotation_x, 0f, 0f);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        Vector3 worldMove = transform.rotation * moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + worldMove);
    }

    private void LateUpdate()
    {
        if (IsOwner)
        {
            smoothedYRotation = Mathf.LerpAngle(
                smoothedYRotation,
                predictedYRotation,
                Time.deltaTime * interpolationSpeed
            );

            if (Time.time - lastSyncTime >= syncInterval)
            {
                ServerSetTargetYRotation(smoothedYRotation);
                lastSyncTime = Time.time;
            }
        }
        else
        {
            smoothedYRotation = Mathf.LerpAngle(
                smoothedYRotation,
                targetYRotation.Value,
                Time.deltaTime * interpolationSpeed
            );
        }

        Quaternion bodyRotation = Quaternion.Euler(0, smoothedYRotation, 0);

        Quaternion chestTwist = Quaternion.Inverse(transform.rotation) * bodyRotation;

        float twistAngle = chestTwist.eulerAngles.y;
        if (twistAngle > 180f)
            twistAngle -= 360f;

        float currentCatchUpSpeed = rotationCatchUpSpeed;
        if (Mathf.Abs(twistAngle) > maxChestTwist)
        {
            float excessTwist = Mathf.Abs(twistAngle) - maxChestTwist;
            currentCatchUpSpeed = Mathf.Lerp(
                rotationCatchUpSpeed,
                maxCatchUpSpeed,
                excessTwist / 90f
            );
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            bodyRotation,
            Time.deltaTime * currentCatchUpSpeed
        );

        chestTwist = Quaternion.Inverse(transform.rotation) * bodyRotation;
        chestBone.localRotation = chestTwist;
    }

    [ServerRpc]
    private void ServerSetTargetYRotation(float value)
    {
        targetYRotation.Value = value;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        netAnimator.SetTrigger("jumpTrigger");
    }
}
