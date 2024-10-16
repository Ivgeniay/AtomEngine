using Microsoft.AspNetCore.Components.WebView.WindowsForms; 
using RazorAEFrontendLib;

namespace WinAtomEngineFrontend
{
    public partial class Form1 : Form
    {
        public Form1(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            workSpace.HostPage = "wwwroot/index.html";
            workSpace.Services = serviceProvider;
            workSpace.RootComponents.Add<App>("#app");
        }

        private void blazorWebView1_Click(object sender, EventArgs e)
        {

        }
    }
}
