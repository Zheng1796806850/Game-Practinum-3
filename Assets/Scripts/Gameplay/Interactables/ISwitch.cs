using System;
using UnityEngine;

public interface ISwitch
{
    bool IsActivated { get; }
    event Action<bool> OnActivatedChanged;
}
