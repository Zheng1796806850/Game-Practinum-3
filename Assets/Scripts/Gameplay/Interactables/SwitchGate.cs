using UnityEngine;

public class SwitchGate : MonoBehaviour
{
    [SerializeField] private BulletSwitch[] bulletSwitches;
    [SerializeField] private PressurePlateSwitch[] pressurePlates;
    [SerializeField] private ElementalGenerator[] elementalGenerators;

    [Header("Controlled Objects")]
    [SerializeField] private bool controlDoor = true;
    [SerializeField] private DoorLinearOpener door;
    [SerializeField] private bool controlFountain = false;
    [SerializeField] private WaterPlatform[] fountains;

    [Header("Behavior")]
    [SerializeField] private bool autoClose = true;

    private bool lastAllActive = false;

    void Update()
    {
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
            if (allActive)
            {
                if (controlDoor && door != null) door.Open();
                if (controlFountain && fountains != null)
                {
                    foreach (var f in fountains)
                    {
                        if (f != null) f.Activate();
                    }
                }
            }
            else if (autoClose)
            {
                if (controlDoor && door != null) door.Close();
                if (controlFountain && fountains != null)
                {
                    foreach (var f in fountains)
                    {
                        if (f != null) f.Deactivate();
                    }
                }
            }

            lastAllActive = allActive;
        }
    }
}
