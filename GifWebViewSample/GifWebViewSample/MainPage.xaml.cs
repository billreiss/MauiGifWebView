namespace GifWebViewSample
{
    public partial class MainPage : ContentPage
    {
        Aspect[] aspectOptions = { Aspect.AspectFill, Aspect.AspectFit, Aspect.Center, Aspect.Fill }; 
        public MainPage()
        {
            InitializeComponent();
            picker.SelectedIndex = 1;
        }

        private void picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            gifImage.Aspect = image.Aspect = aspectOptions[picker.SelectedIndex];
        }
    }

}
