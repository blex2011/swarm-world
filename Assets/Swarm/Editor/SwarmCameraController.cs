using UnityEngine;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Camera controller optimized for viewing and following swarms
    /// </summary>
    public class SwarmCameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        public SwarmManager targetSwarm;
        public bool autoFindSwarm = true;
        public bool followSwarmCenter = false;
        public float followSmoothness = 2f;
        public Vector3 followOffset = new Vector3(0, 10f, -15f);
        
        [Header("Camera Control")]
        public float mouseSensitivity = 2f;
        public float scrollSensitivity = 2f;
        public float moveSpeed = 10f;
        public float fastMoveMultiplier = 3f;
        
        [Header("Bounds")]
        public bool useBounds = true;
        public Vector3 boundsMin = new Vector3(-50, 1, -50);
        public Vector3 boundsMax = new Vector3(50, 50, 50);
        
        [Header("Auto Framing")]
        public bool autoFrameSwarm = false;
        public float framingMargin = 5f;
        public float minDistance = 5f;
        public float maxDistance = 100f;
        
        private Camera cam;
        private Vector3 lastMousePosition;
        private bool isDragging;
        private Vector3 targetPosition;
        private Vector3 velocity;
        
        void Start()
        {
            cam = GetComponent<Camera>();
            targetPosition = transform.position;
            
            if (autoFindSwarm && targetSwarm == null)
            {
                targetSwarm = FindObjectOfType<SwarmManager>();
            }
        }
        
        void Update()
        {
            HandleInput();
            
            if (followSwarmCenter && targetSwarm != null)
            {
                FollowSwarm();
            }
            
            if (autoFrameSwarm && targetSwarm != null)
            {
                AutoFrameSwarm();
            }
            
            ApplyMovement();
        }
        
        void HandleInput()
        {
            // Mouse look (right mouse button)
            if (Input.GetMouseButtonDown(1))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }
            
            if (isDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                // Rotate around target
                Vector3 targetPoint = transform.position + transform.forward * 10f;
                transform.RotateAround(targetPoint, Vector3.up, mouseDelta.x * mouseSensitivity);
                transform.RotateAround(targetPoint, transform.right, -mouseDelta.y * mouseSensitivity);
                
                lastMousePosition = Input.mousePosition;
            }
            
            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                Vector3 forward = transform.forward * scroll * scrollSensitivity;
                targetPosition += forward;
                
                // Clamp distance
                if (targetSwarm != null)
                {
                    Vector3 swarmCenter = targetSwarm.GetSwarmCenter();
                    float distance = Vector3.Distance(targetPosition, swarmCenter);
                    
                    if (distance < minDistance)
                    {
                        targetPosition = swarmCenter - transform.forward * minDistance;
                    }
                    else if (distance > maxDistance)
                    {
                        targetPosition = swarmCenter - transform.forward * maxDistance;
                    }
                }
            }
            
            // WASD movement
            Vector3 moveInput = Vector3.zero;
            
            if (Input.GetKey(KeyCode.W)) moveInput += transform.forward;
            if (Input.GetKey(KeyCode.S)) moveInput -= transform.forward;
            if (Input.GetKey(KeyCode.A)) moveInput -= transform.right;
            if (Input.GetKey(KeyCode.D)) moveInput += transform.right;
            if (Input.GetKey(KeyCode.Q)) moveInput -= transform.up;
            if (Input.GetKey(KeyCode.E)) moveInput += transform.up;
            
            float currentMoveSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentMoveSpeed *= fastMoveMultiplier;
            }
            
            targetPosition += moveInput.normalized * currentMoveSpeed * Time.deltaTime;
            
            // Focus on swarm (F key)
            if (Input.GetKeyDown(KeyCode.F) && targetSwarm != null)
            {
                FocusOnSwarm();
            }
            
            // Toggle follow mode (T key)
            if (Input.GetKeyDown(KeyCode.T))
            {
                followSwarmCenter = !followSwarmCenter;
                Debug.Log($"Swarm follow mode: {(followSwarmCenter ? "ON" : "OFF")}");
            }
        }
        
        void FollowSwarm()
        {
            if (targetSwarm == null) return;
            
            Vector3 swarmCenter = targetSwarm.GetSwarmCenter();
            Vector3 desiredPosition = swarmCenter + followOffset;
            
            targetPosition = Vector3.Lerp(targetPosition, desiredPosition, Time.deltaTime * followSmoothness);
            
            // Look at swarm center
            Vector3 lookDirection = (swarmCenter - transform.position).normalized;
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSmoothness);
            }
        }
        
        void AutoFrameSwarm()
        {
            if (targetSwarm == null) return;
            
            Bounds swarmBounds = targetSwarm.GetSwarmBounds();
            
            if (swarmBounds.size.magnitude > 0.1f)
            {
                // Calculate required distance to frame the swarm
                float maxExtent = Mathf.Max(swarmBounds.size.x, swarmBounds.size.y, swarmBounds.size.z);
                float distance = (maxExtent + framingMargin) / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
                
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
                
                Vector3 swarmCenter = swarmBounds.center;
                Vector3 cameraDirection = (transform.position - swarmCenter).normalized;
                targetPosition = swarmCenter + cameraDirection * distance;
            }
        }
        
        void ApplyMovement()
        {
            // Apply bounds
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, boundsMin.x, boundsMax.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, boundsMin.y, boundsMax.y);
                targetPosition.z = Mathf.Clamp(targetPosition.z, boundsMin.z, boundsMax.z);
            }
            
            // Smooth movement
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.1f);
        }
        
        public void FocusOnSwarm()
        {
            if (targetSwarm == null) return;
            
            Vector3 swarmCenter = targetSwarm.GetSwarmCenter();
            Bounds swarmBounds = targetSwarm.GetSwarmBounds();
            
            // Calculate optimal viewing distance
            float maxExtent = Mathf.Max(swarmBounds.size.x, swarmBounds.size.y, swarmBounds.size.z);
            float distance = (maxExtent + framingMargin) / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            
            // Position camera at a good angle
            Vector3 direction = new Vector3(0.5f, 0.7f, -1f).normalized;
            targetPosition = swarmCenter + direction * distance;
            
            // Look at swarm
            transform.LookAt(swarmCenter);
            
            Debug.Log($"Focused on swarm: {targetSwarm.name}");
        }
        
        public void SetTargetSwarm(SwarmManager swarm)
        {
            targetSwarm = swarm;
            if (swarm != null)
            {
                FocusOnSwarm();
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw bounds
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = (boundsMin + boundsMax) * 0.5f;
                Vector3 size = boundsMax - boundsMin;
                Gizmos.DrawWireCube(center, size);
            }
            
            // Draw follow offset
            if (targetSwarm != null && followSwarmCenter)
            {
                Vector3 swarmCenter = targetSwarm.GetSwarmCenter();
                Vector3 targetPos = swarmCenter + followOffset;
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(swarmCenter, targetPos);
                Gizmos.DrawWireSphere(targetPos, 1f);
            }
        }
    }
}