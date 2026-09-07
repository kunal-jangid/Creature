using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using Window = System.Windows.Window;

namespace Creature.UI;

public class PetNameDialog : Window
{
    private readonly TextBox _inputBox;
    public string PetName => _inputBox.Text.Trim();

    public PetNameDialog(string title, string prompt, string defaultName)
    {
        Title = title;
        Width = 360;
        Height = 170;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 35));
        Foreground = Brushes.White;

        var panel = new StackPanel
        {
            Margin = new Thickness(20)
        };

        var label = new TextBlock
        {
            Text = prompt,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(label);

        _inputBox = new TextBox
        {
            Text = defaultName,
            FontSize = 14,
            Padding = new Thickness(6, 4, 6, 4),
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 52)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 85)),
            Margin = new Thickness(0, 0, 0, 15)
        };
        panel.Children.Add(_inputBox);

        var btnPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "Adopt",
            IsDefault = true,
            Width = 75,
            Height = 28,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(70, 120, 220)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(_inputBox.Text))
            {
                DialogResult = true;
                Close();
            }
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            Width = 75,
            Height = 28,
            Background = new SolidColorBrush(Color.FromRgb(55, 55, 65)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        cancelButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        btnPanel.Children.Add(okButton);
        btnPanel.Children.Add(cancelButton);
        panel.Children.Add(btnPanel);

        Content = panel;

        Loaded += (s, e) =>
        {
            _inputBox.Focus();
            _inputBox.SelectAll();
        };
    }

    public static string? Prompt(string species, string defaultName, string title = "Adopt a New Pet", string? customPrompt = null)
    {
        var promptText = customPrompt ?? $"Enter a name for your new {species}:";
        var dialog = new PetNameDialog(title, promptText, defaultName);
        return dialog.ShowDialog() == true ? dialog.PetName : null;
    }
}
