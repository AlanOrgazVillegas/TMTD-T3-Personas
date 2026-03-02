using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace TMTD_T3_Personas
{
    internal class Program : MauiApplication
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }
    }
}
