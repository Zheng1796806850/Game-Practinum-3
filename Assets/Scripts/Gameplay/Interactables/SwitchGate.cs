using UnityEngine;

public class SwitchGate : MonoBehaviour
{
    [SerializeField] private BulletSwitch[] bulletSwitches;
    [SerializeField] private PressurePlateSwitch[] pressurePlates;
    [SerializeField] private ElementalGenerator[] elementalGenerators;
    [SerializeField] private DoorLinearOpener door;
    [SerializeField] private bool autoClose = true;

    private bool lastAllActive = false;

    void Update()
    {
        if (door == null) return;

        bool allActive = true;

        if (bulletSwitches != null && bulletSwitches.Length > 0)
        {
            for (int i = 0; i < bulletSwitches.Length; i++)
            {
                if (bulletSwitches[i] == null || !bulletSwitches[i].IsActivated)
                {
                    allActive = false;
                    break;
                }
            }
        }

        if (allActive && pressurePlates != null && pressurePlates.Length > 0)
        {
            for (int i = 0; i < pressurePlates.Length; i++)
            {
                if (pressurePlates[i] == null || !pressurePlates[i].IsActivated)
                {
                    allActive = false;
                    break;
                }
            }
        }

        if (allActive && elementalGenerators != null && elementalGenerators.Length > 0)
        {
            for (int i = 0; i < elementalGenerators.Length; i++)
            {
                if (elementalGenerators[i] == null || !elementalGenerators[i].IsActivated)
                {
                    allActive = false;
                    break;
                }
            }
        }

        if (allActive != lastAllActive)
        {
            if (allActive) door.Open();
            else if (autoClose) door.Close();
            lastAllActive = allActive;
        }
    }
}
