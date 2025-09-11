using System;
using MSUScripter.Events;

namespace MSUScripter.Services;

public class StatusBarService
{
    public event EventHandler<ValueEventArgs<string>>? StatusBarTextUpdated;

    public StatusBarService(IAudioPlayerService audioPlayerService)
    {
        audioPlayerService.PlayStarted += (_, _) =>
        {
            UpdateStatusBar("Playing Song");
        };
        
        audioPlayerService.PlayStopped += (_, _) =>
        {
            UpdateStatusBar("Stopped Song");
        };
    }

    public void UpdateStatusBar(string text)
    {
        StatusBarTextUpdated?.Invoke(this, new ValueEventArgs<string>(text));
    }
}