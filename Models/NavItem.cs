using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace qiyana.Models;

public partial class NavItem : ObservableObject
{
    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string _iconText;

    [ObservableProperty]
    private string? _profileIconPath;

    public Type ViewModelType { get; init; } = null!;

    public NavItem(string label, string iconText, Type viewModelType)
    {
        _label = label;
        _iconText = iconText;
        ViewModelType = viewModelType;
    }
}
