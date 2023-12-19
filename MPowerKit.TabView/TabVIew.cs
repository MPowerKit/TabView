using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MPowerKit.TabView;

[ContentProperty("Tabs")]
public class TabView : ContentView
{
    private Grid _contentContainer;
    private ScrollView _headersScroll;
    private TabViewItem _prevSelectedTabItem;
    private bool _useItemsSource;
    private Layout _headersContainer;

    private Layout _stackItemsLayout => new StackLayout()
    {
        VerticalOptions = LayoutOptions.Fill,
        HorizontalOptions = LayoutOptions.Fill,
        Spacing = 0,
        Orientation = StackOrientation.Horizontal
    };

    public TabView()
    {
        Tabs.CollectionChanged += Tabs_CollectionChanged;
    }

    public ObservableCollection<TabViewItem> Tabs { get; } = new ObservableCollection<TabViewItem>();

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentContainer = this.GetTemplateChild("PART_ContentContainer") as Grid;
        _headersScroll = this.GetTemplateChild("PART_HeadersScrollView") as ScrollView;

        InitHeaderBarLayout();
    }

    private void InitHeaderBarLayout()
    {
        if (_headersScroll == null || !(_headersScroll.Content is ItemsPresenter)) return;

        var newLayout = HeaderItemsLayout ?? _stackItemsLayout;

        if (_headersContainer != null)
        {
            foreach (var item in _headersContainer.Children)
            {
                newLayout.Children.Add(item);
            }

            _headersContainer.Children.Clear();
        }

        _headersContainer = newLayout;

        (_headersScroll.Content as ItemsPresenter).Content = _headersContainer;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (this.BindingContext != null && !_useItemsSource)
        {
            foreach (var tab in Tabs)
            {
                if (tab.BindingContext != this.BindingContext) tab.BindingContext = this.BindingContext;
            }
        }
    }

    protected override void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanging(propertyName);

        if (propertyName == ItemsSourceProperty.PropertyName)
        {
            if (ItemsSource != null)
            {
                if (ItemsSource is INotifyCollectionChanged itemsSource) itemsSource.CollectionChanged -= ItemsSource_CollectionChanged;

                _useItemsSource = false;

                Tabs.Clear();
            }
        }
        else if (propertyName == SelectedTabIndexProperty.PropertyName)
        {
            if (SelectedTabIndex == -1) _prevSelectedTabItem = null;
            else _prevSelectedTabItem = Tabs.ElementAtOrDefault(SelectedTabIndex);
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == ItemsSourceProperty.PropertyName && ItemsSource != null)
        {
            if (ItemsSource is INotifyCollectionChanged itemsSource) itemsSource.CollectionChanged += ItemsSource_CollectionChanged;

            _useItemsSource = true;

            InitItems(ItemsSource);
        }
        else if (propertyName == SelectedTabIndexProperty.PropertyName)
        {
            if (SelectedTabIndex == -1)
            {
                if (_prevSelectedTabItem != null) _prevSelectedTabItem.IsSelected = false;
            }
            else if (_contentContainer != null)
            {
                if (_prevSelectedTabItem != null) _prevSelectedTabItem.IsSelected = false;

                var newTab = Tabs.ElementAtOrDefault(SelectedTabIndex);
                if (newTab != null) newTab.IsSelected = true;
            }

            SelectedTabChangedCommand?.Execute(SelectedTabChangedCommandParameter);
        }
        else if (propertyName == ContentTemplateProperty.PropertyName && _useItemsSource)
        {
            foreach (var tab in Tabs)
            {
                var index = Tabs.IndexOf(tab);
                InitContentTemplate(tab.BindingContext, tab);
            }
        }
        else if (propertyName == HeaderItemsLayoutProperty.PropertyName)
        {
            InitHeaderBarLayout();
        }
        else if (propertyName == HideTabsWhenDisabledProperty.PropertyName)
        {
            foreach (var tab in Tabs)
            {
                tab.HideWhenDisabled = HideTabsWhenDisabled;
            }
        }
    }

    private void InitItems(IEnumerable source, bool useIndex = false, int index = 0)
    {
        if (source == null) return;

        foreach (var item in source)
        {
            var tabItem = new TabViewItem();
            InitContentTemplate(item, tabItem);

            tabItem.BindingContext = item;
            tabItem.Header = item;

            if (useIndex) Tabs.Insert(index, tabItem);
            else Tabs.Add(tabItem);
        }
    }

    private void InitContentTemplate(object item, TabViewItem tabItem)
    {
        if (ContentTemplate != null)
        {
            var template = ContentTemplate;
            if (ContentTemplate is DataTemplateSelector selector) template = selector.SelectTemplate(item, this);

            var tabContent = template.CreateContent() as View;
            tabContent.BindingContext ??= item;

            tabItem.Content = tabContent;
        }
        else
        {
            var tabContent = new Label();
            tabContent.Text = item.ToString();

            tabItem.Content = tabContent;
        }
    }

    private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    InitItems(e.NewItems, true, e.NewStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                {
                    Tabs.Move(e.OldStartingIndex, e.NewStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    if (Tabs.Count == 1)
                    {
                        Tabs.Clear();
                        return;
                    }

                    if (e.OldStartingIndex < SelectedTabIndex)
                    {
                        Tabs.RemoveAt(e.OldStartingIndex);
                        SelectedTabIndex--;
                    }
                    else if (e.OldStartingIndex == SelectedTabIndex && e.OldStartingIndex == Tabs.Count - 1)
                    {
                        SelectedTabIndex--;
                        Tabs.RemoveAt(e.OldStartingIndex);
                    }
                    else if (e.OldStartingIndex == SelectedTabIndex && e.OldStartingIndex != Tabs.Count - 1)
                    {
                        Tabs.RemoveAt(e.OldStartingIndex);
                        var tab = Tabs.ElementAtOrDefault(SelectedTabIndex);
                        tab.IsSelected = true;
                    }
                    else Tabs.RemoveAt(e.OldStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    var tab = Tabs.ElementAt(e.OldStartingIndex);
                    var newItem = e.NewItems[0];

                    InitContentTemplate(newItem, tab);

                    tab.Header = newItem;
                    tab.BindingContext = newItem;
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                {
                    foreach (var item in Tabs)
                    {
                        item.PropertyChanged -= TabItem_PropertyChanged;
                    }

                    Tabs.Clear();
                }
                break;
        }
    }

    private void Tabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    foreach (var item in e.NewItems)
                    {
                        var tabItem = item as TabViewItem;
                        tabItem.HideWhenDisabled = this.HideTabsWhenDisabled;
                        tabItem.PropertyChanged += TabItem_PropertyChanged;

                        if (!(tabItem.Header is View))
                        {
                            tabItem.TabViewHeaderItem.InitContent();
                        }

                        if (_headersContainer != null) _headersContainer.Children.Insert(e.NewStartingIndex, tabItem.TabViewHeaderItem);
                        if (_contentContainer != null) _contentContainer.Children.Insert(e.NewStartingIndex, tabItem);
                    }

                    InitialTabSelect(e);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                {
                    if (_headersContainer != null)
                    {
                        var header = _headersContainer.Children.ElementAt(e.OldStartingIndex);
                        _headersContainer.Children.Remove(header);
                        _headersContainer.Children.Insert(e.NewStartingIndex, header);
                    }

                    if (_contentContainer != null)
                    {
                        var content = _contentContainer.Children.ElementAt(e.OldStartingIndex);
                        _contentContainer.Children.Remove(content);
                        _contentContainer.Children.Insert(e.NewStartingIndex, content);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    _headersContainer?.Children.RemoveAt(e.OldStartingIndex);
                    _contentContainer?.Children.RemoveAt(e.OldStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                {
                    _headersContainer?.Children.Clear();
                    _contentContainer?.Children.Clear();
                    SelectedTabIndex = -1;
                }
                break;
        }
    }

    private void InitialTabSelect(NotifyCollectionChangedEventArgs e)
    {
        if (SelectedTabIndex == -1)
        {
            if (Tabs.Any(t => t.IsSelected))
            {
                SelectedTabIndex = Tabs.IndexOf(Tabs.FirstOrDefault(t => t.IsSelected));
            }
            else
            {
                var tab = Tabs.First();
                if (!tab.IsEnabled) SelectClosestTab(tab, Tabs.Where(t => t.IsEnabled || t == tab).ToList());
                else SelectedTabIndex = 0;
            }
        }
        else
        {
            if (!Tabs.Any(t => t.IsSelected))
            {
                var tab = Tabs.ElementAtOrDefault(SelectedTabIndex);
                if (!tab.IsEnabled) SelectClosestTab(tab, Tabs.Where(t => t.IsEnabled || t == tab).ToList());
                else tab.IsSelected = true;
            }
            else if (e.NewStartingIndex <= SelectedTabIndex) SelectedTabIndex++;
        }
    }

    private void TabItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var tab = sender as TabViewItem;

        if (e.PropertyName == TabViewItem.IsSelectedProperty.PropertyName && tab.IsSelected)
        {
            if (tab.IsEnabled)
            {
                SelectedTabIndex = Tabs.IndexOf(tab);
                if (_headersScroll != null && ScrollToSelectedTab && _headersContainer.Width > _headersScroll.Width)
                {
                    var max = _headersContainer.Width - _headersScroll.Width;
                    var scrollTo = tab.TabViewHeaderItem.X - (_headersScroll.Width - tab.TabViewHeaderItem.Width) / 2.0;
                    if (scrollTo < 0) scrollTo = 0;
                    else if (scrollTo > max) scrollTo = max;

                    _headersScroll.ScrollToAsync(scrollTo, 0, true);
                }
            }
            else tab.IsSelected = false;
        }
        else if (e.PropertyName == TabViewItem.IsEnabledProperty.PropertyName)
        {
            if (!tab.IsEnabled && tab.IsSelected) SelectClosestTab(tab, Tabs.Where(t => t.IsEnabled || t == tab).ToList());
            else if (tab.IsEnabled && !Tabs.Any(t => t.IsSelected))
            {
                var index = Tabs.IndexOf(tab);
                SelectedTabIndex = index;
            }
        }
    }

    private void SelectClosestTab(TabViewItem tab, List<TabViewItem> tabs)
    {
        if (tabs.Count == 1)
        {
            SelectedTabIndex = -1;
        }
        else
        {
            var index = tabs.IndexOf(tab);

            if (index == tabs.Count - 1)
            {
                var tabToSelect = tabs.ElementAtOrDefault(index - 1);
                SelectedTabIndex = Tabs.IndexOf(tabToSelect);
            }
            else if (index >= 0)
            {
                var tabToSelect = tabs.ElementAtOrDefault(index + 1);
                SelectedTabIndex = Tabs.IndexOf(tabToSelect);
            }
        }
    }

    #region HideTabsWhenDisabled
    public bool HideTabsWhenDisabled
    {
        get { return (bool)GetValue(HideTabsWhenDisabledProperty); }
        set { SetValue(HideTabsWhenDisabledProperty, value); }
    }

    public static readonly BindableProperty HideTabsWhenDisabledProperty =
        BindableProperty.Create(
            nameof(HideTabsWhenDisabled),
            typeof(bool),
            typeof(TabView),
            true
            );
    #endregion

    #region HeaderItemsLayout
    public Layout HeaderItemsLayout
    {
        get { return (Layout)GetValue(HeaderItemsLayoutProperty); }
        set { SetValue(HeaderItemsLayoutProperty, value); }
    }

    public static readonly BindableProperty HeaderItemsLayoutProperty =
        BindableProperty.Create(
            nameof(HeaderItemsLayout),
            typeof(Layout),
            typeof(TabView)
            );
    #endregion

    #region ScrollToSelectedTab
    public bool ScrollToSelectedTab
    {
        get { return (bool)GetValue(ScrollToSelectedTabProperty); }
        set { SetValue(ScrollToSelectedTabProperty, value); }
    }

    public static readonly BindableProperty ScrollToSelectedTabProperty =
        BindableProperty.Create(
            nameof(ScrollToSelectedTab),
            typeof(bool),
            typeof(TabView),
            true
            );
    #endregion

    #region ContentBackgroundColor
    public Color ContentBackgroundColor
    {
        get { return (Color)GetValue(ContentBackgroundColorProperty); }
        set { SetValue(ContentBackgroundColorProperty, value); }
    }

    public static readonly BindableProperty ContentBackgroundColorProperty =
        BindableProperty.Create(
            nameof(ContentBackgroundColor),
            typeof(Color),
            typeof(TabView),
            Colors.Transparent
            );
    #endregion

    #region HeaderBarBackgroundColor
    public Color HeaderBarBackgroundColor
    {
        get { return (Color)GetValue(HeaderBarBackgroundColorProperty); }
        set { SetValue(HeaderBarBackgroundColorProperty, value); }
    }

    public static readonly BindableProperty HeaderBarBackgroundColorProperty =
        BindableProperty.Create(
            nameof(HeaderBarBackgroundColor),
            typeof(Color),
            typeof(TabView),
            Colors.Transparent
            );
    #endregion

    #region SelectedHeaderBackgroundColor
    public Color SelectedHeaderBackgroundColor
    {
        get { return (Color)GetValue(SelectedHeaderBackgroundColorProperty); }
        set { SetValue(SelectedHeaderBackgroundColorProperty, value); }
    }

    public static readonly BindableProperty SelectedHeaderBackgroundColorProperty =
        BindableProperty.Create(
            nameof(SelectedHeaderBackgroundColor),
            typeof(Color),
            typeof(TabView),
            Colors.Blue
            );
    #endregion

    #region HeaderBackgroundColor
    public Color HeaderBackgroundColor
    {
        get { return (Color)GetValue(HeaderBackgroundColorProperty); }
        set { SetValue(HeaderBackgroundColorProperty, value); }
    }

    public static readonly BindableProperty HeaderBackgroundColorProperty =
        BindableProperty.Create(
            nameof(HeaderBackgroundColor),
            typeof(Color),
            typeof(TabView),
            Colors.Transparent
            );
    #endregion

    #region HeaderHeightRequest
    public double HeaderHeightRequest
    {
        get { return (double)GetValue(HeaderHeightRequestProperty); }
        set { SetValue(HeaderHeightRequestProperty, value); }
    }

    public static readonly BindableProperty HeaderHeightRequestProperty =
        BindableProperty.Create(
            nameof(HeaderHeightRequest),
            typeof(double),
            typeof(TabView),
            50.0
            );
    #endregion

    #region HeaderFontFamily
    public string HeaderFontFamily
    {
        get { return (string)GetValue(HeaderFontFamilyProperty); }
        set { SetValue(HeaderFontFamilyProperty, value); }
    }

    public static readonly BindableProperty HeaderFontFamilyProperty =
        BindableProperty.Create(
            nameof(HeaderFontFamily),
            typeof(string),
            typeof(TabView)
            );
    #endregion

    #region HeaderFontSize
    public double HeaderFontSize
    {
        get { return (double)GetValue(HeaderFontSizeProperty); }
        set { SetValue(HeaderFontSizeProperty, value); }
    }

    public static readonly BindableProperty HeaderFontSizeProperty =
        BindableProperty.Create(
            nameof(HeaderFontSize),
            typeof(double),
            typeof(TabView),
            14.0
            );
    #endregion

    #region HeaderFontAttributes
    public FontAttributes HeaderFontAttributes
    {
        get { return (FontAttributes)GetValue(HeaderFontAttributesProperty); }
        set { SetValue(HeaderFontAttributesProperty, value); }
    }

    public static readonly BindableProperty HeaderFontAttributesProperty =
        BindableProperty.Create(
            nameof(HeaderFontAttributes),
            typeof(FontAttributes),
            typeof(TabView),
            FontAttributes.None
            );
    #endregion

    #region HeaderTextColor
    public Color HeaderTextColor
    {
        get { return (Color)GetValue(HeaderTextColorProperty); }
        set { SetValue(HeaderTextColorProperty, value); }
    }

    public static readonly BindableProperty HeaderTextColorProperty =
        BindableProperty.Create(
            nameof(HeaderTextColor),
            typeof(Color),
            typeof(TabView),
            Colors.Black
            );
    #endregion

    #region SelectedTabIndex
    public int SelectedTabIndex
    {
        get { return (int)GetValue(SelectedTabIndexProperty); }
        set { SetValue(SelectedTabIndexProperty, value); }
    }

    public static readonly BindableProperty SelectedTabIndexProperty =
        BindableProperty.Create(
            nameof(SelectedTabIndex),
            typeof(int),
            typeof(TabView),
            -1
            );
    #endregion

    #region SelectedHeaderTemplate
    public DataTemplate SelectedHeaderTemplate
    {
        get { return (DataTemplate)GetValue(SelectedHeaderTemplateProperty); }
        set { SetValue(SelectedHeaderTemplateProperty, value); }
    }

    public static readonly BindableProperty SelectedHeaderTemplateProperty =
        BindableProperty.Create(
            nameof(SelectedHeaderTemplate),
            typeof(DataTemplate),
            typeof(TabView)
            );
    #endregion

    #region HeaderTemplate
    public DataTemplate HeaderTemplate
    {
        get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
        set { SetValue(HeaderTemplateProperty, value); }
    }

    public static readonly BindableProperty HeaderTemplateProperty =
        BindableProperty.Create(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(TabView)
            );
    #endregion

    #region ContentTemplate
    public DataTemplate ContentTemplate
    {
        get { return (DataTemplate)GetValue(ContentTemplateProperty); }
        set { SetValue(ContentTemplateProperty, value); }
    }

    public static readonly BindableProperty ContentTemplateProperty =
        BindableProperty.Create(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(TabView)
            );
    #endregion

    #region ItemsSource
    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(TabView)
            );
    #endregion

    #region SelectedTabChangedCommand
    public ICommand SelectedTabChangedCommand
    {
        get { return (ICommand)GetValue(SelectedTabChangedCommandProperty); }
        set { SetValue(SelectedTabChangedCommandProperty, value); }
    }

    public static readonly BindableProperty SelectedTabChangedCommandProperty =
        BindableProperty.Create(
            nameof(SelectedTabChangedCommand),
            typeof(ICommand),
            typeof(TabView)
            );
    #endregion

    #region SelectedTabChangedCommandParameter
    public object SelectedTabChangedCommandParameter
    {
        get { return (object)GetValue(SelectedTabChangedCommandParameterProperty); }
        set { SetValue(SelectedTabChangedCommandParameterProperty, value); }
    }

    public static readonly BindableProperty SelectedTabChangedCommandParameterProperty =
        BindableProperty.Create(
            nameof(SelectedTabChangedCommandParameter),
            typeof(object),
            typeof(TabView)
            );
    #endregion

    #region HeaderBarAlignment
    public Alignment HeaderBarAlignment
    {
        get { return (Alignment)GetValue(HeaderBarAlignmentProperty); }
        set { SetValue(HeaderBarAlignmentProperty, value); }
    }

    public static readonly BindableProperty HeaderBarAlignmentProperty =
        BindableProperty.Create(
            nameof(HeaderBarAlignment),
            typeof(Alignment),
            typeof(TabView),
            Alignment.Top
            );
    #endregion


}
