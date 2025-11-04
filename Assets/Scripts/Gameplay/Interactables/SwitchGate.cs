using System;
using UnityEngine;

public class SwitchGate : MonoBehaviour
{
    public enum GateMode { Simple, Sequential }

    [SerializeField] private GateMode mode = GateMode.Simple;

    [SerializeField] private BulletSwitch[] bulletSwitches;
    [SerializeField] private PressurePlateSwitch[] pressurePlates;
    [SerializeField] private ElementalGenerator[] elementalGenerators;

    [Serializable]
    public class SequenceGroup
    {
        public MonoBehaviour[] steps;
    }

    [Header("Sequential Groups")]
    [SerializeField] private SequenceGroup[] sequenceGroups;

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
        bool allActive = EvaluateAllActive();

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

    private bool EvaluateAllActive()
    {
        if (mode == GateMode.Simple)
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

            return allActive;
        }
        else
        {
            if (sequenceGroups == null || sequenceGroups.Length == 0) return false;
            for (int g = 0; g < sequenceGroups.Length; g++)
            {
                var grp = sequenceGroups[g];
                if (grp == null || grp.steps == null || grp.steps.Length == 0) return false;
                bool groupActive = true;
                for (int i = 0; i < grp.steps.Length; i++)
                {
                    var mb = grp.steps[i];
                    if (mb == null) { groupActive = false; break; }
                    if (mb is ISwitch sw)
                    {
                        if (!sw.IsActivated) { groupActive = false; break; }
                    }
                    else
                    {
                        groupActive = false;
                        break;
                    }
                }
                if (!groupActive) return false;
            }
            return true;
        }
    }
}
