using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private Vector2 moveDelta;
    private bool isMoving, isRotating;
    private float xRot;
    private Camera thisCam;

    [Header("Camera tuning")]
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float rotSpeed = .5f;

    // For clamping that has not been implimented yet
    [SerializeField] private float maxZoom = 15;
    [SerializeField] private float minZoom = 2;
    [SerializeField] private float startZoom = 15;


    private void Awake()
    {
        xRot = transform.rotation.eulerAngles.x;
        thisCam = GetComponent<Camera>();
    }

    private void Start()
    {
        thisCam.orthographicSize = Mathf.Clamp(startZoom, minZoom, maxZoom);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        moveDelta = context.ReadValue<Vector2>();
        //Debug.Log("Cam delta: " + delta);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // As long as we are clicking or holding button, we are moving
        isMoving = context.started || context.performed;
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        isRotating = context.started || context.performed;
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        Debug.Log("OnZoom context Y: " + context.ReadValue<Vector2>().y);
        float scroll = context.ReadValue<Vector2>().y;

        if (Mathf.Abs(scroll) < 0.01f) return;

        thisCam.orthographicSize -= scroll * zoomSpeed * 0.01f;
    }

    private void LateUpdate()
    {
        if(isMoving)
        {
            Vector3 pos = transform.right * (moveDelta.x * -moveSpeed);
            pos += transform.up * (moveDelta.y * -moveSpeed);
            transform.position += pos * Time.deltaTime; 
        }

        if(isRotating)
        {
            transform.Rotate(new Vector3(xRot, -moveDelta.x * rotSpeed, 0.0f));
            transform.rotation = Quaternion.Euler(xRot, transform.rotation.eulerAngles.y, 0.0f);
        }

    }
}
