using System;
using MSUScripter.ViewModels;

namespace MSUScripter.Events;

public class PyMusicLooperPanelUpdatedArgs(PyMusicLooperResultViewModel? result) : EventArgs
{
    public PyMusicLooperResultViewModel? Result => result;
}