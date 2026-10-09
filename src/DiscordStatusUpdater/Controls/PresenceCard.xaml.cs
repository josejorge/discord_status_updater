// File: PresenceCard.xaml.cs
// Description: Dependency properties for the PresenceCard control (text lines, initials tile and button labels).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Windows;
using System.Windows.Controls;

namespace DiscordStatusUpdater.Controls;

public partial class PresenceCard : UserControl
{
    public PresenceCard() => InitializeComponent();

    // Each property is a plain string DP so the card can be bound to either the live detection or the editor.
    public static readonly DependencyProperty HeadingProperty = Register(nameof(Heading), "PLAYING");
    public static readonly DependencyProperty AppNameProperty = Register(nameof(AppName));
    public static readonly DependencyProperty DetailsProperty = Register(nameof(Details));
    public static readonly DependencyProperty StateProperty = Register(nameof(State));
    public static readonly DependencyProperty ElapsedProperty = Register(nameof(Elapsed));
    public static readonly DependencyProperty InitialsProperty = Register(nameof(Initials), "?");
    public static readonly DependencyProperty Button1Property = Register(nameof(Button1));
    public static readonly DependencyProperty Button2Property = Register(nameof(Button2));

    public string Heading { get => (string)GetValue(HeadingProperty); set => SetValue(HeadingProperty, value); }
    public string AppName { get => (string)GetValue(AppNameProperty); set => SetValue(AppNameProperty, value); }
    public string Details { get => (string)GetValue(DetailsProperty); set => SetValue(DetailsProperty, value); }
    public string State { get => (string)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public string Elapsed { get => (string)GetValue(ElapsedProperty); set => SetValue(ElapsedProperty, value); }
    public string Initials { get => (string)GetValue(InitialsProperty); set => SetValue(InitialsProperty, value); }
    public string Button1 { get => (string)GetValue(Button1Property); set => SetValue(Button1Property, value); }
    public string Button2 { get => (string)GetValue(Button2Property); set => SetValue(Button2Property, value); }

    private static DependencyProperty Register(string name, string defaultValue = "") =>
        DependencyProperty.Register(name, typeof(string), typeof(PresenceCard), new PropertyMetadata(defaultValue));
}
