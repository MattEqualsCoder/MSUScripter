using System;

namespace MSUScripter.Events;

public class ValueEventArgs<T>(T data) : EventArgs
{
    public T Data => data;
}