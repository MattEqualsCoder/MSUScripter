using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace MSUScripter.Controls;

public class ScalableWindow : Window
{
    private static decimal _globalScaleFactor = 1;
    private static decimal _previousScaleFactor = 1;
    private static decimal _changeScaleFactor;

    public static decimal GlobalScaleFactor
    {
        get => _globalScaleFactor;
        set
        {
            _previousScaleFactor = _globalScaleFactor;
            _globalScaleFactor = Math.Clamp(value, 1.0m, 3.0m);
            _changeScaleFactor = _globalScaleFactor / _previousScaleFactor;

            if (_previousScaleFactor != _globalScaleFactor)
            {
                GlobalScaleFactorChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    private static event EventHandler? GlobalScaleFactorChanged;

    private LayoutTransformControl? _layoutTransformControl;

    private double _defaultMinWidth;
    private double _defaultMinHeight;
    private double _defaultMaxWidth;
    private double _defaultMaxHeight;
    
    public ScalableWindow()
    {
        Loaded += (sender, args) =>
        {
            _layoutTransformControl = this.Find<LayoutTransformControl>("MainLayout");
            if (_layoutTransformControl == null)
            {
                return;
            }

            _defaultMinWidth = MinWidth;
            _defaultMinHeight = MinHeight;
            _defaultMaxWidth = MaxWidth;
            _defaultMaxHeight = MaxHeight;
            GlobalScaleFactorChanged += (_, _) =>
            {
                OnScaleChanged(false);
            };

            if (GlobalScaleFactor != 1)
            {
                OnScaleChanged(true);
            }
        };

    }

    private void OnScaleChanged(bool init)
    {
        if (_layoutTransformControl == null) return;
        _layoutTransformControl.LayoutTransform = new ScaleTransform((double)_globalScaleFactor, (double)_globalScaleFactor);
        UpdateWindowSize(init);
    }

    private void UpdateWindowSize(bool init)
    {
        if (!init)
        {
            var changeScale = init ? _globalScaleFactor : _changeScaleFactor;
            Width *= (double)changeScale;
            Height *= (double)changeScale;    
        }
        
        if (_defaultMinWidth > 0)
        {
            MinWidth = _defaultMinWidth * (double)GlobalScaleFactor;
        }
        if (_defaultMinHeight > 0)
        {
            MinHeight = _defaultMinHeight * (double)GlobalScaleFactor;
        }
        if (_defaultMaxWidth > 0)
        {
            MaxWidth = _defaultMaxWidth * (double)GlobalScaleFactor;
        }
        if (_defaultMaxHeight > 0)
        {
            MaxHeight = _defaultMaxHeight * (double)GlobalScaleFactor;
        }
    }
}