using System;

public class CollapseFailedException : Exception
{
    public readonly Slot slot;

    public CollapseFailedException(Slot slot)
    {
        this.slot = slot;
    }
}