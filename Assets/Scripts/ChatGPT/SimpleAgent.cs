using UnityEngine;

namespace ChatGPT
{
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleAgent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float backOffSpeed = 4f;
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float backOffDistance = 5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float rotationSpeed = 10f;
        
        // Ground check removed - agent can jump anytime
        
        [Header("Follow Target")]
        [SerializeField] private Transform followTarget;
        
        private Rigidbody rb;
        private bool isFollowing = false;
        private bool isBackingOff = false;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            // If no follow target is assigned, try to find the main camera or player
            if (followTarget == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    followTarget = mainCam.transform;
                }
            }
        }
        
        private void Update()
        {
            if (isFollowing && followTarget != null)
            {
                FollowTarget();
            }
            else if (isBackingOff && followTarget != null)
            {
                BackOffFromTarget();
            }
        }
        
        private void FollowTarget()
        {
            Vector3 targetPosition = followTarget.position;
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // Only move if we're further than the follow distance
            if (distance > followDistance)
            {
                // Move towards target
                Vector3 moveDirection = new Vector3(direction.x, 0, direction.z);
                rb.linearVelocity = new Vector3(moveDirection.x * followSpeed, rb.linearVelocity.y, moveDirection.z * followSpeed);
                
                // Rotate to face target
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Stop horizontal movement when close enough
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }
        
        private void BackOffFromTarget()
        {
            Vector3 targetPosition = followTarget.position;
            Vector3 direction = (transform.position - targetPosition).normalized; // Reversed direction compared to follow
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // Keep backing off until we reach the desired distance
            if (distance < backOffDistance)
            {
                // Move away from target
                Vector3 moveDirection = new Vector3(direction.x, 0, direction.z);
                rb.linearVelocity = new Vector3(moveDirection.x * backOffSpeed, rb.linearVelocity.y, moveDirection.z * backOffSpeed);
                
                // Rotate to face away from target
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // We've reached the desired distance, stop backing off
                isBackingOff = false;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                Debug.Log("Agent: Backed off to safe distance");
            }
        }
        
        #region Agent Commands (Wire these to AgentCommandInterpreter)
        
        /// <summary>
        /// Start following the target
        /// </summary>
        public void StartFollowing()
        {
            isFollowing = true;
            Debug.Log("Agent: Started following");
        }
        
        /// <summary>
        /// Stop all movement and actions
        /// </summary>
        public void Stop()
        {
            isFollowing = false;
            isBackingOff = false;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            Debug.Log("Agent: Stopped");
        }
        
        /// <summary>
        /// Make the agent jump
        /// </summary>
        public void Jump()
        {
            // Jump anytime without ground check
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("Agent: Jumped");
        }
        
        /// <summary>
        /// Make the agent idle (same as stop for this simple implementation)
        /// </summary>
        public void Idle()
        {
            Stop();
            Debug.Log("Agent: Idling");
        }
        
        /// <summary>
        /// Make the agent back off from the target
        /// </summary>
        public void BackOff()
        {
            isFollowing = false;
            isBackingOff = true;
            Debug.Log("Agent: Backing off");
        }
        
        #endregion
        
        #region Public Setters
        
        /// <summary>
        /// Set the target to follow
        /// </summary>
        /// <param name="target">Transform to follow</param>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }
        
        /// <summary>
        /// Set the follow speed
        /// </summary>
        /// <param name="speed">Speed value</param>
        public void SetFollowSpeed(float speed)
        {
            followSpeed = speed;
        }
        
        /// <summary>
        /// Set the follow distance
        /// </summary>
        /// <param name="distance">Distance to maintain from target</param>
        public void SetFollowDistance(float distance)
        {
            followDistance = distance;
        }
        
        /// <summary>
        /// Set the back off distance
        /// </summary>
        /// <param name="distance">Distance to maintain when backing off</param>
        public void SetBackOffDistance(float distance)
        {
            backOffDistance = distance;
        }
        

        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw follow and back off distances
            if (followTarget != null)
            {
                // Draw follow distance
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(followTarget.position, followDistance);
                
                // Draw back off distance
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange with transparency
                Gizmos.DrawWireSphere(followTarget.position, backOffDistance);
                
                // Draw line to target
                Gizmos.color = isBackingOff ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, followTarget.position);
            }
            // Ground check visualization removed
        }
        
        #endregion
    }
}
