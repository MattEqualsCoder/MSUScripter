using System;
using MSUScripter.Events;

namespace MSUScripter.Services;

public class StatusBarService
{
    public event EventHandler<ValueEventArgs<string>>? StatusBarTextUpdated;

    public StatusBarService(IAudioPlayerService audioPlayerService)
    {
        audioPlayerService.PlayStarted += (sender, args) =>
        {
            UpdateStatusBar("Playing Song");
        };
        
        audioPlayerService.PlayStopped += (sender, args) =>
        {
            UpdateStatusBar("Stopped Song");
        };
    }

    public void UpdateStatusBar(string text)
    {
        StatusBarTextUpdated?.Invoke(this, new ValueEventArgs<string>(text));
    }
}