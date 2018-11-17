using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary> 
/// Used to transfer int with event
/// </summary> 
public class IntDataEventArgs : EventArgs
{
    public int dataInt;
    public IntDataEventArgs(int dataInt)
    {
        this.dataInt = dataInt;
    }
}

/// <summary> 
/// Used to transfer bool with event
/// </summary> 
public class BoolDataEventArgs : EventArgs
{
    public bool dataBool;
    public BoolDataEventArgs(bool dataBool)
    {
        this.dataBool = dataBool;
    }
}

/// <summary> 
/// Used to transfer string with event
/// </summary> 
public class StringDataEventArgs : EventArgs
{
    public string dataString;
    public StringDataEventArgs(string dataString)
    {
        this.dataString = dataString;
    }
}