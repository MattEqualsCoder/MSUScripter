using System;
using System.Threading;
using System.Threading.Tasks;

namespace MSUScripter.Tools;

public static class ActionExtensions
{
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }
    
    public static Action Debounce(this Action func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func();
                    }
                }, TaskScheduler.Default);
        };
    }

}