using System.Windows;
using System.Windows.Controls;
using ArtStudio.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ArtStudio.WPF.Views;

/// <summary>
/// Interaction logic for WorkspaceView.xaml
/// </summary>
public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private void AddWorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        // Simple input dialog for workspace name
        var dialog = new WorkspaceNameDialog();
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.WorkspaceName))
        {
            if (DataContext is WorkspaceViewModel viewModel)
            {
                if (viewModel.CreateWorkspaceCommand.CanExecute(dialog.WorkspaceName))
                {
                    viewModel.CreateWorkspaceCommand.Execute(dialog.WorkspaceName);
                }
            }
        }
    }
}

/// <summary>
/// Simple dialog for entering workspace name
/// </summary>
public partial class WorkspaceNameDialog : Window
{
    public string WorkspaceName { get; private set; } = string.Empty;

    public WorkspaceNameDialog()
    {
        Title = "Create New Workspace";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Margin = new Thickness(20);

        var label = new TextBlock
        {
            Text = "Enter workspace name:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);

        var textBox = new TextBox
        {
            Name = "NameTextBox",
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(textBox, 1);
        grid.Children.Add(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "OK",
            IsDefault = true,
            Margin = new Thickness(0, 0, 10, 0),
            MinWidth = 75
        };
        okButton.Click += (s, e) =>
        {
            WorkspaceName = textBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(WorkspaceName))
            {
                DialogResult = true;
            }
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 75
        };
        cancelButton.Click += (s, e) => DialogResult = false;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        Content = grid;

        textBox.Focus();
        textBox.SelectAll();
    }
}
