using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Constraints
{
    public enum ConstraintMode
    {
        EnforceConnector,
        ExcludeConnector
    }

    public enum ConstraintDirection
    {
        Up,
        Down,
        Horizontal
    }

    public int yLocal = 0;
    public ConstraintDirection direction;
    public ConstraintMode mode;
    public int connector;
}