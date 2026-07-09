using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using qiyana.Models.MatchData;

namespace qiyana.Views;

public partial class ScoreboardPanel : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private Participant? _selectedBlue;
    public Participant? SelectedBlue
    {
        get => _selectedBlue;
        set
        {
            if (_selectedBlue != value)
            {
                _selectedBlue = value;
                OnPropertyChanged();
            }
        }
    }

    private Participant? _selectedRed;
    public Participant? SelectedRed
    {
        get => _selectedRed;
        set
        {
            if (_selectedRed != value)
            {
                _selectedRed = value;
                OnPropertyChanged();
            }
        }
    }

    public event EventHandler<Participant>? PlayerNameClicked;

    public ScoreboardPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GameDetail detail)
        {
            SelectedBlue = detail.TeamBlue.FirstOrDefault();
            SelectedRed = detail.TeamRed.FirstOrDefault();
        }
    }

    private void OnScoreboardRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled) return;

        if (sender is Border border && border.DataContext is Participant participant)
        {
            if (participant.TeamId == 100)
                SelectedBlue = SelectedBlue == participant ? null : participant;
            else
                SelectedRed = SelectedRed == participant ? null : participant;
        }
    }

    private void OnPlayerNamePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;

        if (sender is Border { DataContext: Participant p } && !string.IsNullOrEmpty(p.SummonerFullName))
            PlayerNameClicked?.Invoke(this, p);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
