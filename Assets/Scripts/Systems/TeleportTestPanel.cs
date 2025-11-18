using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeleportTestPanel : MonoBehaviour
{
    public Transform playerRoot;
    public Rigidbody2D playerRigidbody;
    public Transform[] teleportTargets;
    public RectTransform panelRoot;
    public Button buttonPrefab;
    public KeyCode toggleKey = KeyCode.BackQuote;

    private bool panelVisible;

    void Start()
    {
        if (playerRoot != null && playerRigidbody == null)
        {
            playerRigidbody = playerRoot.GetComponent<Rigidbody2D>();
        }

        if (panelRoot != null)
        {
            AnchorPanelBottomLeft(panelRoot);
            panelRoot.gameObject.SetActive(false);
        }

        CreateButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            panelVisible = !panelVisible;
            if (panelRoot != null) panelRoot.gameObject.SetActive(panelVisible);
        }
    }

    private void AnchorPanelBottomLeft(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
    }

    private void CreateButtons()
    {
        if (panelRoot == null) return;
        if (buttonPrefab == null) return;
        if (teleportTargets == null) return;

        for (int i = panelRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = panelRoot.GetChild(i);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < teleportTargets.Length; i++)
        {
            Transform target = teleportTargets[i];
            if (target == null) continue;

            Button btn = Instantiate(buttonPrefab, panelRoot);
            btn.name = "Teleport_" + target.name;

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = target.name;

            int index = i;
            btn.onClick.AddListener(() => TeleportTo(index));
        }
    }

    private void TeleportTo(int index)
    {
        if (teleportTargets == null) return;
        if (index < 0 || index >= teleportTargets.Length) return;

        Transform target = teleportTargets[index];
        if (target == null) return;

        if (playerRoot != null)
        {
            playerRoot.position = target.position;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }
}
