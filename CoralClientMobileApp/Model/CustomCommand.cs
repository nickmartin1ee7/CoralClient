using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.Model
{
    public enum CommandTarget
    {
        Server,
        Player
    }

    public partial class CustomCommand : ObservableObject
    {
        [Key]
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _command = string.Empty;

        [ObservableProperty]
        private bool _requiresPlayerName;

        [ObservableProperty]
        private string _category = "Default";

        [ObservableProperty]
        private CommandTarget _target = CommandTarget.Server;

        [ObservableProperty]
        private Guid _serverProfileId;

        public CustomCommand()
        {
        }

        public CustomCommand(string name, string description, string command, bool requiresPlayerName = false, string category = "Default", CommandTarget target = CommandTarget.Server)
        {
            Name = name;
            Description = description;
            Command = command;
            RequiresPlayerName = requiresPlayerName;
            Category = category;
            Target = target;
        }

        public CustomCommand(Guid id, string name, string description, string command, bool requiresPlayerName, string category, CommandTarget target, Guid serverProfileId)
        {
            Id = id;
            Name = name;
            Description = description;
            Command = command;
            RequiresPlayerName = requiresPlayerName;
            Category = category;
            Target = target;
            ServerProfileId = serverProfileId;
        }
    }
}