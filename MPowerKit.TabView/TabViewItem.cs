using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MPowerKit.TabView;

public class TabViewItem : ContentView
{
    internal TabViewHeaderItem TabViewHeaderItem { get; set; } = new();

    public TabViewItem()
    {
        TabViewHeaderItem.PropertyChanged += TabViewHeaderItem_PropertyChanged;
        IsVisible = false;
    }

    private void TabViewHeaderItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == TabViewHeaderItem.IsSelectedProperty.PropertyName)
        {
            this.IsSelected = TabViewHeaderItem.IsSelected;
            this.IsVisible = this.IsSelected;
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsSelectedProperty.PropertyName)
        {
            TabViewHeaderItem.IsSelected = IsSelected;
        }
        else if (propertyName == HeaderProperty.PropertyName)
        {
            TabViewHeaderItem.HeaderContent = Header;
        }
        else if (propertyName == IsEnabledProperty.PropertyName)
        {
            if (HideWhenDisabled && !this.IsEnabled) TabViewHeaderItem.IsVisible = false;
            else
            {
                TabViewHeaderItem.IsVisible = true;
                TabViewHeaderItem.IsEnabled = this.IsEnabled;
            }
            if (!this.IsEnabled && this.IsSelected) this.IsSelected = false;
        }
        else if (propertyName == HideWhenDisabledProperty.PropertyName)
        {
            if (HideWhenDisabled && !this.IsEnabled) TabViewHeaderItem.IsVisible = false;
            else
            {
                TabViewHeaderItem.IsVisible = true;
                TabViewHeaderItem.IsEnabled = this.IsEnabled;
            }
        }
    }

    #region HideWhenDisabled
    internal bool HideWhenDisabled
    {
        get { return (bool)GetValue(HideWhenDisabledProperty); }
        set { SetValue(HideWhenDisabledProperty, value); }
    }

    internal static readonly BindableProperty HideWhenDisabledProperty =
        BindableProperty.Create(
            nameof(HideWhenDisabled),
            typeof(bool),
            typeof(TabViewItem),
            true
            );
    #endregion

    #region IsSelected
    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        internal set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(TabViewItem)
            );
    #endregion

    #region Header
    public object Header
    {
        get { return (object)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly BindableProperty HeaderProperty =
        BindableProperty.Create(
            nameof(Header),
            typeof(object),
            typeof(TabViewItem)
            );
    #endregion
}