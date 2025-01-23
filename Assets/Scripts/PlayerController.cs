using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask gravityLayer;
    [SerializeField] private float gravityStrength = -9.81f;
    [HideInInspector] public GameObject gravityObject;
    [SerializeField] private float gravityDetectionDistance = 5f;
    [SerializeField] private float gravityTransitionSpeed = 2f; // Speed of gravity transition
    [SerializeField] private Transform cameraJoint;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f; // Maximum movement speed
    private Rigidbody rb;
    private GameObject lastGravityObject;
    private bool isGrounded;
    private Vector3 currentGravityDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        UpdateGravity();
        Movement();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
        CheckGround();
        CamControl(); // Call CamControl to handle camera movement
    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        // Move where the player is facing
        Vector3 move = transform.right * x + transform.forward * z;
        rb.AddForce(move * moveSpeed, ForceMode.Force);

        // Clamp the velocity to the maximum speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddRelativeForce(0f, jumpForce, 0f, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void UpdateGravity()
    {
        gravityObject = GetClosestGravityObject();
        if (gravityObject != null)
        {
            Vector3 newGravityDirection = gravityObject.transform.position - transform.position;
            if (gravityObject.CompareTag("Plane"))
            {
                newGravityDirection = -gravityObject.transform.forward;
            }
            else if (gravityObject.CompareTag("Sphere"))
            {
                newGravityDirection = gravityObject.transform.position - transform.position;
            }

            if (lastGravityObject != null && lastGravityObject != gravityObject)
            {
                // Interpolate between the previous and current gravity directions
                currentGravityDirection = Vector3.Lerp(currentGravityDirection, newGravityDirection, gravityTransitionSpeed * Time.deltaTime);
            }
            else
            {
                currentGravityDirection = newGravityDirection;
            }

            rb.AddForce(currentGravityDirection.normalized * gravityStrength * rb.mass, ForceMode.Force);
            AutoOrient(-currentGravityDirection.normalized);

            lastGravityObject = gravityObject;
        }
    }

    float mouseX, mouseY;

    void CamControl()
    {
        mouseX = Input.GetAxis("Mouse X") * 10f;
        mouseY = Input.GetAxis("Mouse Y") * 10f;

        // Rotate the player body around the Y axis
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera around the X axis
        cameraJoint.Rotate(Vector3.right * -mouseY);
    }

    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void AutoOrient(Vector3 down)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, down) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }

    private GameObject GetClosestGravityObject()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, gravityDetectionDistance, gravityLayer);
        GameObject closestObject = null;
        float closestDistance = Mathf.Infinity;
        foreach (Collider collider in colliders)
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = collider.gameObject;
            }
        }
        return closestObject;
    }
}
