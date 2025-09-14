using System.Collections.ObjectModel;

namespace CoralClientMobileApp.Model
{
    public class CommandGroup
    {
        public string CategoryName { get; set; } = string.Empty;
        public ObservableCollection<CustomCommand> Commands { get; set; } = new();
    }
}