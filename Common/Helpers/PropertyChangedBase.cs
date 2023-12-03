﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ciribob.SRS.Common.Helpers;

public class PropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        try
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        catch (Exception ex)
        {
            //todo
        }
        
    }
}