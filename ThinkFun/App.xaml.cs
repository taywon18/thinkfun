using Microsoft.Maui.Controls;

namespace ThinkFun
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
}