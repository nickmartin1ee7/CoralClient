using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoralClient
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewServerPage : ContentPage
    {
        public NewServerPage()
        {
            InitializeComponent();
        }

        private void CarouselPositionChanged(object sender, PositionChangedEventArgs e)
        {
            var carousel = sender as CarouselView;
            var views = carousel.VisibleViews;

            if (views.Count > 0)
            {
                foreach (var view in views)
                {
                    var img = view.FindByName<Image>("ServerImage");
                    img.CancelAnimations();

                    Task.Run(async () => await img.RelRotateTo(360, 5000, Easing.BounceOut));
                }
            }
        }
    }
}