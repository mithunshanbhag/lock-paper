using LockPaper.Ui.Models;
using LockPaper.Ui.PageModels;

namespace LockPaper.Ui.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}