[System.Serializable]

//BoundaryConstraint class represents a constraint applied to the boundaries of a map.
public class BoundaryConstraint
{
    // Enum specifying how the constraint is applied
    public enum ConstraintMode
    {
        EnforceConnector, // Enforces the presence of a connector
        ExcludeConnector  // Excludes the presence of a connector
    }

    // Enum specifying the direction of the constraint
    public enum ConstraintDirection
    {
        Horizontal  // Currently only supports horizontal constraints
    }

    public int relativeY = 0;
    public ConstraintDirection direction;
    public ConstraintMode mode;
    public int connector;
}