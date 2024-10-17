using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MPowerKit.TabView;

public class TabViewItem : ContentView
{
    public TabViewHeaderItem TabViewHeaderItem { get; protected set; } = [];

    public TabViewItem()
    {
        TabViewHeaderItem.PropertyChanged += TabViewHeaderItem_PropertyChanged;

        SetBinding(IsVisibleProperty, new Binding(IsSelectedProperty.PropertyName, source: this));
        TabViewHeaderItem.SetBinding(View.IsEnabledProperty, new Binding(IsEnabledProperty.PropertyName, mode: BindingMode.TwoWay, source: this));
        TabViewHeaderItem.SetBinding(TabViewHeaderItem.HeaderContentProperty, new Binding(HeaderProperty.PropertyName, source: this));
        TabViewHeaderItem.SetBinding(Grid.ColumnProperty, new Binding(ColumnProperty.PropertyName, source: this));
        TabViewHeaderItem.SetBinding(Grid.RowProperty, new Binding(RowProperty.PropertyName, source: this));
    }

    private void TabViewHeaderItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == TabViewHeaderItem.IsSelectedProperty.PropertyName)
        {
            IsSelected = TabViewHeaderItem.IsSelected;
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName
            || propertyName == IsSelectedProperty.PropertyName
            && !IsEnabled && IsSelected)
        {
            IsSelected = false;
        }

        if (propertyName == IsSelectedProperty.PropertyName)
        {
            TabViewHeaderItem.IsSelected = IsSelected;
        }
    }

    #region IsSelected
    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
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

    #region Column
    public int Column
    {
        get { return (int)GetValue(ColumnProperty); }
        set { SetValue(ColumnProperty, value); }
    }

    public static readonly BindableProperty ColumnProperty =
        BindableProperty.Create(
            nameof(Column),
            typeof(int),
            typeof(TabViewItem)
            );
    #endregion

    #region Row
    public int Row
    {
        get { return (int)GetValue(RowProperty); }
        set { SetValue(RowProperty, value); }
    }

    public static readonly BindableProperty RowProperty =
        BindableProperty.Create(
            nameof(Row),
            typeof(int),
            typeof(TabViewItem)
            );
    #endregion
}