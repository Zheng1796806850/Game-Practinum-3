using UnityEngine;

public class SwitchGate : MonoBehaviour
{
    [SerializeField] private BulletSwitch[] switches;
    [SerializeField] private DoorLinearOpener door;
    private bool opened;

    void Update()
    {
        if (opened) return;
        if (switches == null || switches.Length == 0 || door == null) return;

        for (int i = 0; i < switches.Length; i++)
        {
            if (switches[i] == null || !switches[i].IsActivated) return;
        }

        opened = true;
        door.Open();
    }
}
