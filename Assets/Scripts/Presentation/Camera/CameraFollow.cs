using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        FollowTarget,
        FixedPoints
    }

    public enum PairMode
    {
        Inherit,
        FollowTarget,
        FixedPoints
    }

    [System.Serializable]
    public class CameraTriggerPair
    {
        public Transform triggerObject;
        public Transform cameraPoint;
        public bool smoothTransition = true;
        public PairMode modeOverride = PairMode.Inherit;
    }

    public CameraMode mode = CameraMode.FollowTarget;
    [Range(0.01f, 1f)] public float smoothSpeed = 0.125f;

    public Transform target;
    public Vector3 offset = Vector3.zero;

    public CameraTriggerPair[] cameraPairs;

    private Transform currentPoint;
    private bool currentSmoothMove = true;
    private Vector3 velocity = Vector3.zero;

    private bool hasLocalOverride = false;
    private CameraMode localMode = CameraMode.FollowTarget;

    private void FixedUpdate()
    {
        var effectiveMode = hasLocalOverride ? localMode : mode;
        switch (effectiveMode)
        {
            case CameraMode.FollowTarget:
                FollowTargetMode();
                break;
            case CameraMode.FixedPoints:
                FixedPointsMode();
                break;
        }
    }

    private void FollowTargetMode()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = transform.position.z;
        if (currentSmoothMove)
        {
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
            transform.position = smoothedPosition;
        }
        else
        {
            transform.position = desiredPosition;
        }
    }

    private void FixedPointsMode()
    {
        if (currentPoint == null) return;
        Vector3 desiredPosition = currentPoint.position;
        desiredPosition.z = transform.position.z;
        if (currentSmoothMove)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        }
        else
        {
            transform.position = desiredPosition;
        }
    }

    public void OnTriggerEntered(Transform trigger)
    {
        for (int i = 0; i < (cameraPairs != null ? cameraPairs.Length : 0); i++)
        {
            var p = cameraPairs[i];
            if (p != null && p.triggerObject == trigger)
            {
                if (p.cameraPoint != null)
                {
                    currentPoint = p.cameraPoint;
                }
                currentSmoothMove = p.smoothTransition;
                velocity = Vector3.zero;

                if (p.modeOverride == PairMode.Inherit)
                {
                    hasLocalOverride = false;
                }
                else
                {
                    hasLocalOverride = true;
                    localMode = (p.modeOverride == PairMode.FollowTarget) ? CameraMode.FollowTarget : CameraMode.FixedPoints;
                }
                return;
            }
        }
    }

    public void ClearLocalOverride()
    {
        hasLocalOverride = false;
    }
}
