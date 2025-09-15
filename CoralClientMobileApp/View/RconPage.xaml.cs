using CoralClientMobileApp.Model;
using CoralClientMobileApp.ViewModel;
using CoralClientMobileApp.Services;

namespace CoralClientMobileApp.View
{
    public partial class RconPage : ContentPage
    {
        private RconPageViewModel? _vm;
        private readonly Func<ServerProfile, RconPageViewModel> _rconViewModelFactory;

        public RconPage(Func<ServerProfile, RconPageViewModel> rconViewModelFactory)
        {
            InitializeComponent();
            _rconViewModelFactory = rconViewModelFactory;
        }

        public void Initialize(ServerProfile serverProfile)
        {
            _vm = _rconViewModelFactory(serverProfile);
            _vm.OpenEditorRequested += OnOpenEditorRequested;
            BindingContext = _vm;

            // Auto-connect only if RCON password is available
            if (_vm.CurrentState == RconPageViewModel.State.DISCONNECTED && _vm.CanConnect)
            {
                _vm.ToggleConnectionCommand.Execute(null);
            }
        }

        private async void OnOpenEditorRequested(object? sender, CustomCommand? command)
        {
            var editorPage = Handler!.MauiContext!.Services.GetRequiredService<CustomCommandEditorPage>();
            var editorViewModel = Handler!.MauiContext!.Services.GetRequiredService<CustomCommandEditorViewModel>();
            
            // Wire up event handlers for the editor
            editorViewModel.CommandSaved += OnCommandSaved;
            editorViewModel.CommandDeleted += OnCommandDeleted;
            editorViewModel.Cancelled += OnEditorCancelled;
            
            // Initialize the editor with the command (null for new command)
            editorViewModel.Initialize(command, _vm!.ServerProfile.Id);
            editorPage.BindingContext = editorViewModel;
            
            await Navigation.PushModalAsync(editorPage);
        }

        private async void OnCommandSaved(object? sender, CustomCommand command)
        {
            try
            {
                // Save the command using the service
                var customCommandService = Handler!.MauiContext!.Services.GetRequiredService<ICustomCommandService>();
                await customCommandService.SaveCommandAsync(command);
                
                // Refresh the custom commands in the main ViewModel
                await _vm!.LoadCustomCommandsAsync();
                
                // Close the modal
                await Navigation.PopModalAsync();
            }
            catch (Exception)
            {
                await Navigation.PopModalAsync();
            }
            finally
            {
                // Clean up event handlers
                if (sender is CustomCommandEditorViewModel vm)
                {
                    vm.CommandSaved -= OnCommandSaved;
                    vm.CommandDeleted -= OnCommandDeleted;
                    vm.Cancelled -= OnEditorCancelled;
                }
            }
        }

        private async void OnCommandDeleted(object? sender, CustomCommand command)
        {
            try
            {
                // Delete the command using the service
                var customCommandService = Handler!.MauiContext!.Services.GetRequiredService<ICustomCommandService>();
                await customCommandService.DeleteCommandAsync(command);
                
                // Refresh the custom commands in the main ViewModel
                await _vm!.LoadCustomCommandsAsync();
                
                // Close the modal
                await Navigation.PopModalAsync();
            }
            catch (Exception)
            {
                // Error occurred while refreshing commands
            }
            finally
            {
                // Clean up event handlers
                if (sender is CustomCommandEditorViewModel vm)
                {
                    vm.CommandSaved -= OnCommandSaved;
                    vm.CommandDeleted -= OnCommandDeleted;
                    vm.Cancelled -= OnEditorCancelled;
                }
            }
        }

        private async void OnEditorCancelled(object? sender, EventArgs e)
        {
            try
            {
                // Just close the modal without saving
                await Navigation.PopModalAsync();
            }
            finally
            {
                // Clean up event handlers
                if (sender is CustomCommandEditorViewModel vm)
                {
                    vm.CommandSaved -= OnCommandSaved;
                    vm.CommandDeleted -= OnCommandDeleted;
                    vm.Cancelled -= OnEditorCancelled;
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (_vm != null)
            {
                _vm.OpenEditorRequested -= OnOpenEditorRequested;
                _vm.Dispose();
            }
            return base.OnBackButtonPressed();
        }
    }
}
