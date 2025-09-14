using CoralClientMobileApp.Model;
using CoralClientMobileApp.ViewModel;

namespace CoralClientMobileApp.View
{
    public partial class CustomCommandEditorPage : ContentPage
    {
        private readonly CustomCommandEditorViewModel _viewModel;

        public CustomCommandEditorPage(CustomCommandEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        public void Initialize(CustomCommand? command = null, Guid? serverProfileId = null)
        {
            _viewModel.Initialize(command, serverProfileId);
        }
    }
}