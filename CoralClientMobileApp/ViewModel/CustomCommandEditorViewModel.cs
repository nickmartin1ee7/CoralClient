using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoralClientMobileApp.Model;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.ViewModel
{
    public partial class CustomCommandEditorViewModel : BaseObservableViewModel
    {
        private readonly ILogger<CustomCommandEditorViewModel> _logger;
        private CustomCommand? _originalCommand;
        private Guid? _serverProfileId;

        [ObservableProperty]
        private string _commandName = string.Empty;

        [ObservableProperty]
        private string _commandDescription = string.Empty;

        [ObservableProperty]
        private string _commandText = string.Empty;

        [ObservableProperty]
        private string _commandCategory = "Default";

        [ObservableProperty]
        private bool _requiresPlayerName;

        [ObservableProperty]
        private CommandTarget _selectedTarget = CommandTarget.Server;

        [ObservableProperty]
        private bool _isEditMode;

        public ObservableCollection<CommandTarget> TargetOptions { get; } = new()
        {
            CommandTarget.Server,
            CommandTarget.Player
        };

        public event EventHandler<CustomCommand>? CommandSaved;
        public event EventHandler<CustomCommand>? CommandDeleted;
        public event EventHandler? Cancelled;

        public CustomCommandEditorViewModel(ILogger<CustomCommandEditorViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize(CustomCommand? command = null, Guid? serverProfileId = null)
        {
            _originalCommand = command;
            _serverProfileId = serverProfileId;
            IsEditMode = command != null;

            if (command != null)
            {
                CommandName = command.Name;
                CommandDescription = command.Description;
                CommandText = command.Command;
                CommandCategory = command.Category;
                RequiresPlayerName = command.RequiresPlayerName;
                SelectedTarget = command.Target;
            }
            else
            {
                // Reset for new command
                CommandName = string.Empty;
                CommandDescription = string.Empty;
                CommandText = string.Empty;
                CommandCategory = "Default";
                RequiresPlayerName = false;
                SelectedTarget = CommandTarget.Server;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _logger.LogInformation("Custom command editor cancelled");
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(CommandName) || string.IsNullOrWhiteSpace(CommandText))
            {
                _logger.LogWarning("Cannot save command - name and command text are required");
                return;
            }

            CustomCommand command;
            
            if (IsEditMode && _originalCommand != null)
            {
                // Update existing command
                command = _originalCommand;
                command.Name = CommandName;
                command.Description = CommandDescription;
                command.Command = CommandText;
                command.Category = CommandCategory;
                command.RequiresPlayerName = RequiresPlayerName;
                command.Target = SelectedTarget;
            }
            else
            {
                // Create new command
                command = new CustomCommand(
                    CommandName,
                    CommandDescription,
                    CommandText,
                    RequiresPlayerName,
                    CommandCategory,
                    SelectedTarget
                );
                
                if (_serverProfileId.HasValue)
                {
                    command.ServerProfileId = _serverProfileId.Value;
                }
            }

            _logger.LogInformation("Saving custom command: {CommandName}", CommandName);
            CommandSaved?.Invoke(this, command);
        }

        [RelayCommand]
        private void Delete()
        {
            if (_originalCommand != null)
            {
                _logger.LogInformation("Deleting custom command: {CommandName}", _originalCommand.Name);
                CommandDeleted?.Invoke(this, _originalCommand);
            }
        }
    }
}