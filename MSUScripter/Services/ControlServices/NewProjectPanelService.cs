using System;
using System.IO;
using System.Linq;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Models;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.ViewModels;
using MSUScripter.Views;

namespace MSUScripter.Services.ControlServices;

public class NewProjectPanelService(IMsuTypeService msuTypeService, ProjectService projectService, Settings settings, ILogger<NewProjectPanelService> logger) : ControlService
{
    private NewProjectPanelViewModel _model = new();

    public NewProjectPanelViewModel InitializeModel()
    {
        _model.MsuTypes = msuTypeService.MsuTypes
            .OrderBy(x => x.DisplayName)
            .ToList();
        return _model;
    }

    public void ResetModel()
    {
        _model.SelectedMsuType = null;
        _model.MsuPath = "";
        _model.MsuPcmTracksJsonPath = "";
        _model.MsuPcmWorkingDirectoryPath = "";
        _model.RecentProjects = settings.RecentProjects.Where(x => File.Exists(x.ProjectPath)).OrderByDescending(x => x.Time).ToList();
    }

    public bool CreateNewProject(string path, out MsuProject? newProject, out bool isLegacySmz3, out string? error)
    {
        if (string.IsNullOrEmpty(path) || _model.SelectedMsuType == null ||
            string.IsNullOrEmpty(_model.MsuPath))
        {
            newProject = null;
            isLegacySmz3 = false;
            error = "Missing data. Please enter the project path, msu path, and MSU type.";
            return false;
        }
        
        try
        {
            // newProject = projectService.NewMsuProject(path, _model.SelectedMsuType, _model.MsuPath, _model.MsuPcmTracksJsonPath, _model.MsuPcmWorkingDirectoryPath);
            // isLegacySmz3 = newProject.MsuType == msuTypeService.GetSMZ3LegacyMSUType() &&
            //                msuTypeService.GetSMZ3MsuType() != null;
            newProject = null;
            isLegacySmz3 = false;
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to create new MSU Scripter project");
            newProject = null;
            isLegacySmz3 = false;
            error = exception.Message;
            return false;
        }
    }

    public bool UpdateLegacySmz3Msu(MsuProject project)
    {
        try
        {
            projectService.ConvertProjectMsuType(project, msuTypeService.GetSMZ3MsuType()!, true);
            SaveProject(project);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to update legacy SMZ3 MSU");
            return false;
        }
    }

    public bool LoadProject(string path, out MsuProject? project, out MsuProject? backupProject, out string? error)
    {
        try
        {
            project = projectService.LoadMsuProject(path, false);
            backupProject = null;
            error = null;

            if (project == null)
            {
                error = "MSU Project file not found";
                backupProject = null;
                return false;
            }
            
            if (!string.IsNullOrEmpty(project.BackupFilePath))
            {
                var potentialBackupProject = projectService.LoadMsuProject(project.BackupFilePath, true);
                if (potentialBackupProject != null && potentialBackupProject.LastSaveTime > project.LastSaveTime)
                {
                    backupProject = potentialBackupProject;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error opening project");
            project = null;
            backupProject = null;
            error = "Error opening project. Please contact MattEqualsCoder or post an issue on GitHub";
            return false;
        }
    }

    public void SaveProject(MsuProject project)
    {
        projectService.SaveMsuProject(project, false);
    }
}