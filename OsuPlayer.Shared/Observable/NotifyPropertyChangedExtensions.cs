using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Milki.OsuPlayer.Shared.Observable;

public static class NotifyPropertyChangedExtensions
{
    public static TRet RaiseAndSetIfChanged<TObj, TRet>(
        this TObj reactiveObject,
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null, params string[] additionalChangedMembers)
        where TObj : IInvokableVm
    {
        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        reactiveObject?.RaisePropertyChanging(propertyName);
        backingField = newValue;
        reactiveObject?.RaisePropertyChanged(propertyName);
        foreach (var member in additionalChangedMembers)
            reactiveObject?.RaisePropertyChanged(member);
        return newValue;
    }
}