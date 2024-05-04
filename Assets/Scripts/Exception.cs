using System;

public class Exception : System.Exception
{
    public readonly Cell cell;

    public Exception(Cell cell)
    {
        this.cell = cell;
    }
}