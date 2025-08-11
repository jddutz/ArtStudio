using System;
using System.Threading;
using System.Windows;

namespace ArtStudio.WPF;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Ensure we're running on STA thread
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
        }

        var app = new App();
        app.Run();
    }
}
