using System.Windows.Controls;
using AvalonDock;

namespace ArtStudio.WPF.Views
{
    public partial class WorkspaceContentControl : UserControl
    {
        public WorkspaceContentControl()
        {
            InitializeComponent();
        }

        // Expose the WorkspaceAnchorable for the MainWindow to access
        public AvalonDock.Layout.LayoutAnchorable? GetWorkspaceAnchorable() =>
            WorkspaceAnchorable;

        // Expose the DockingManager for layout manager initialization
        public DockingManager? GetDockingManager() => dockManager;
    }
}