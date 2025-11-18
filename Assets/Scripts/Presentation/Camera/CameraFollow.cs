using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [Header("Mouse Look Offset (Follow Mode)")]
    public bool useMouseLookOffset = true;
    public float mouseOffsetDistance = 2f;
    [Range(0f, 1f)] public float mouseOffsetLerp = 0.2f;

    [Header("Scroll Zoom (Pixel Perfect, Follow Mode)")]
    public bool enableScrollZoom = true;
    public PixelPerfectCamera pixelPerfectCamera;
    public int minRefResolutionY = 270;
    public int maxRefResolutionY = 1080;
    public int zoomStepPerScroll = 30;

    public CameraTriggerPair[] cameraPairs;

    private Transform currentPoint;
    private bool currentSmoothMove = true;
    private Vector3 velocity = Vector3.zero;

    private bool hasLocalOverride = false;
    private CameraMode localMode = CameraMode.FollowTarget;

    private Vector3 currentMouseOffset = Vector3.zero;
    private Camera cam;

    private int currentRefResolutionY;
    private float zoomAspectRatio = 16f / 9f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (pixelPerfectCamera == null)
        {
            pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        }

        if (pixelPerfectCamera != null)
        {
            if (pixelPerfectCamera.refResolutionY <= 0)
                pixelPerfectCamera.refResolutionY = 540;
            if (pixelPerfectCamera.refResolutionX <= 0)
                pixelPerfectCamera.refResolutionX = 960;

            currentRefResolutionY = pixelPerfectCamera.refResolutionY;
            zoomAspectRatio = (float)pixelPerfectCamera.refResolutionX / Mathf.Max(1, pixelPerfectCamera.refResolutionY);

            if (minRefResolutionY > maxRefResolutionY)
            {
                int tmp = minRefResolutionY;
                minRefResolutionY = maxRefResolutionY;
                maxRefResolutionY = tmp;
            }

            currentRefResolutionY = Mathf.Clamp(currentRefResolutionY, minRefResolutionY, maxRefResolutionY);
            pixelPerfectCamera.refResolutionY = currentRefResolutionY;
            pixelPerfectCamera.refResolutionX = Mathf.RoundToInt(currentRefResolutionY * zoomAspectRatio);
        }
    }

    private void Update()
    {
        HandleScrollZoom();
    }

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

        if (useMouseLookOffset)
        {
            Camera usedCam = cam != null ? cam : Camera.main;
            if (usedCam != null)
            {
                Vector3 mouseWorld = usedCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = target.position.z;

                Vector3 dir = mouseWorld - target.position;
                dir.z = 0f;

                Vector3 targetOffset = Vector3.zero;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    targetOffset = dir * mouseOffsetDistance;
                }

                currentMouseOffset = Vector3.Lerp(currentMouseOffset, targetOffset, mouseOffsetLerp);
            }
        }
        else
        {
            currentMouseOffset = Vector3.Lerp(currentMouseOffset, Vector3.zero, mouseOffsetLerp);
        }

        desiredPosition += currentMouseOffset;
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

    private void HandleScrollZoom()
    {
        if (!enableScrollZoom) return;
        if (pixelPerfectCamera == null) return;
        if (mode != CameraMode.FollowTarget && (!hasLocalOverride || localMode != CameraMode.FollowTarget)) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) <= 0.0001f) return;

        int step = zoomStepPerScroll;
        if (step <= 0) step = 1;

        int newY = currentRefResolutionY - Mathf.RoundToInt(scroll * step);
        newY = Mathf.Clamp(newY, minRefResolutionY, maxRefResolutionY);

        if (newY == currentRefResolutionY) return;

        currentRefResolutionY = newY;
        pixelPerfectCamera.refResolutionY = currentRefResolutionY;
        pixelPerfectCamera.refResolutionX = Mathf.RoundToInt(currentRefResolutionY * zoomAspectRatio);
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
