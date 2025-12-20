using System;
using System.Collections.Generic;
using System.Reflection;
using AvaloniaControls.Extensions;
using MSUScripter.Models;
using MSUScripter.Tools;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public abstract partial class ViewModelBase : ReactiveObject
{
    protected ViewModelBase()
    {
        this.LinkProperties();
        SkipLastModifiedProperties = this.GetSkipLastModifiedPropertyNames();
        if (GetType().GetCustomAttribute<SkipLastModifiedAttribute>() != null)
        {
            return;
        }
        
        PropertyChanged += (_, args) =>
        {
            try
            {
                if (args.PropertyName != null && !SkipLastModifiedProperties.Contains(args.PropertyName))
                {
                    LastModifiedDate = DateTime.Now;
                    HasBeenModified = true;
                }
            }
            catch (Exception)
            {
                // TODO: Log
            }
        };
    }

    public abstract ViewModelBase DesignerExample();
    
    [Reactive, SkipConvert, SkipLastModified] public partial DateTime LastModifiedDate { get; set; }
    [Reactive, SkipConvert, SkipLastModified] public partial bool HasBeenModified { get; set; }
    private HashSet<string> SkipLastModifiedProperties { get; }


}
