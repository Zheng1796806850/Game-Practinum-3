using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DoorLinearOpener))]
public class DoorLinearOpenerEditor : Editor
{
    private DoorLinearOpener opener;
    private bool isPreviewPlaying;
    private bool hasReachedEnd;
    private float previewTime;
    private float holdTime;
    private double lastEditorTime;
    private Vector3 previewStartPos;
    private Vector3 previewTargetPos;

    private const float HoldDuration = 2f;

    void OnEnable()
    {
        opener = (DoorLinearOpener)target;
        EditorApplication.update += EditorUpdate;
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
        if (isPreviewPlaying)
        {
            StopPreview();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Preview Open Once"))
            {
                StartPreview();
            }

            if (isPreviewPlaying)
            {
                if (GUILayout.Button("Stop Preview"))
                {
                    StopPreview();
                }
            }
        }
    }

    private void StartPreview()
    {
        if (isPreviewPlaying)
            return;

        isPreviewPlaying = true;
        hasReachedEnd = false;
        previewTime = 0f;
        holdTime = 0f;

        previewStartPos = opener.transform.position;
        previewTargetPos = previewStartPos + opener.DirectionVector * opener.OpenDistance;

        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    private void StopPreview()
    {
        isPreviewPlaying = false;
        hasReachedEnd = false;
        opener.transform.position = previewStartPos;
        EditorUtility.SetDirty(opener);
    }

    private void EditorUpdate()
    {
        if (!isPreviewPlaying || Application.isPlaying)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - lastEditorTime);
        lastEditorTime = currentTime;

        if (!hasReachedEnd)
        {
            float duration = Mathf.Max(0.01f, opener.PreviewDuration);
            previewTime += deltaTime;
            float t = Mathf.Clamp01(previewTime / duration);

            opener.transform.position = Vector3.Lerp(previewStartPos, previewTargetPos, t);
            EditorUtility.SetDirty(opener);

            if (t >= 1f)
            {
                hasReachedEnd = true;
                holdTime = 0f;
            }
        }
        else
        {
            holdTime += deltaTime;
            if (holdTime >= opener.HoldDuration)
            {
                StopPreview();
            }
        }
    }
}
