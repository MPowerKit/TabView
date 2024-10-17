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
    protected Grid? ContentContainer;
    protected ScrollView? HeadersScroll;
    protected TabViewItem? PrevSelectedTabItem;
    protected Layout HeadersContainer;

    protected bool UseItemsSource => ItemsSource is not null;

    public ObservableCollection<TabViewItem> Tabs { get; } = [];

    protected Layout StackItemsLayout
    {
        get
        {
            var stack = new StackLayout()
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
                Spacing = 0,
                Orientation = StackOrientation.Horizontal
            };

            stack.SetBinding(StackLayout.OrientationProperty, new Binding(ScrollView.OrientationProperty.PropertyName,
                source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(ScrollView)),
                converter: new ScrollOrientationToStackOrientationConverter()));

            return stack;
        }
    }

    public TabView()
    {
        Tabs.CollectionChanged += Tabs_CollectionChanged;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ContentContainer = GetTemplateChild("PART_ContentContainer") as Grid;
        HeadersScroll = GetTemplateChild("PART_HeadersScrollView") as ScrollView;

        InitHeaderBarLayout();
    }

    protected virtual void InitHeaderBarLayout()
    {
        if (HeadersScroll == null || HeadersScroll.Content is not ItemsPresenter presenter) return;

        var newLayout = HeaderItemsLayout ?? StackItemsLayout;

        if (HeadersContainer != null)
        {
            foreach (var item in HeadersContainer.Children)
            {
                newLayout.Children.Add(item);
            }

            HeadersContainer.Children.Clear();
        }

        HeadersContainer = newLayout;

        presenter.Content = HeadersContainer;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is not null && !UseItemsSource)
        {
            foreach (var tab in Tabs)
            {
                if (tab.BindingContext != BindingContext)
                    tab.BindingContext = BindingContext;
            }
        }
    }

    protected override void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanging(propertyName);

        if (propertyName == ItemsSourceProperty.PropertyName && ItemsSource is not null)
        {
            if (ItemsSource is INotifyCollectionChanged itemsSource)
                itemsSource.CollectionChanged -= ItemsSource_CollectionChanged;

            Tabs.Clear();
        }
        else if (propertyName == SelectedTabIndexProperty.PropertyName)
        {
            PrevSelectedTabItem = Tabs.ElementAtOrDefault(SelectedTabIndex);
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == ItemsSourceProperty.PropertyName && ItemsSource is not null)
        {
            if (ItemsSource is INotifyCollectionChanged itemsSource)
                itemsSource.CollectionChanged += ItemsSource_CollectionChanged;

            InitItems(ItemsSource);
        }
        else if (propertyName == SelectedTabIndexProperty.PropertyName)
        {
            if (PrevSelectedTabItem is not null) PrevSelectedTabItem.IsSelected = false;

            var newTab = Tabs.ElementAtOrDefault(SelectedTabIndex);
            if (newTab is not null)
            {
                if (newTab.IsEnabled)
                {
                    newTab.IsSelected = true;
                }
                else
                {
                    SelectClosestEnabledTab(newTab);
                    return;
                }
            }

            if (SelectedTabChangedCommand?.CanExecute(SelectedTabChangedCommandParameter) is true)
                SelectedTabChangedCommand.Execute(SelectedTabChangedCommandParameter);
        }
        else if (propertyName == ContentTemplateProperty.PropertyName && UseItemsSource)
        {
            foreach (var tab in Tabs)
            {
                var index = Tabs.IndexOf(tab);
                InitTabContentTemplate(tab.BindingContext, tab);
            }
        }
        else if (propertyName == HeaderItemsLayoutProperty.PropertyName)
        {
            InitHeaderBarLayout();
        }
    }

    protected virtual void InitItems(IEnumerable source, int index = 0, bool useIndex = false)
    {
        if (source is null) return;

        foreach (var item in source)
        {
            var tabItem = new TabViewItem();
            InitTabContentTemplate(item, tabItem);

            tabItem.BindingContext = item;
            tabItem.Header = item;

            if (useIndex) Tabs.Insert(index, tabItem);
            else Tabs.Add(tabItem);
        }
    }

    protected virtual void InitTabContentTemplate(object item, TabViewItem tabItem)
    {
        if (ContentTemplate is not null)
        {
            var template = ContentTemplate;
            if (ContentTemplate is DataTemplateSelector selector) template = selector.SelectTemplate(item, this);

            var tabContent = template.CreateContent() as View;
            tabContent!.BindingContext ??= item;

            tabItem.Content = tabContent;
            return;
        }

        tabItem.Content = new Label
        {
            Text = item.ToString()
        };
    }

    protected virtual void ItemsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                ItemsAdd(e);
                break;
            case NotifyCollectionChangedAction.Move:
                ItemsMove(e);
                break;
            case NotifyCollectionChangedAction.Remove:
                ItemsRemove(e);
                break;
            case NotifyCollectionChangedAction.Replace:
                ItemsReplace(e);
                break;
            case NotifyCollectionChangedAction.Reset:
                ItemsReset(e);
                break;
        }
    }

    protected virtual void ItemsAdd(NotifyCollectionChangedEventArgs e)
    {
        InitItems(e.NewItems!, e.NewStartingIndex, true);
    }

    protected virtual void ItemsMove(NotifyCollectionChangedEventArgs e)
    {
        Tabs.Move(e.OldStartingIndex, e.NewStartingIndex);
    }

    protected virtual void ItemsRemove(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is null) return;

        if (e.OldStartingIndex + e.OldItems.Count < SelectedTabIndex)
        {
            SelectedTabIndex -= e.OldItems.Count;
        }
        else if (SelectedTabIndex >= e.OldStartingIndex
            && SelectedTabIndex <= e.OldStartingIndex + e.OldItems.Count)
        {
            var newIndex = e.OldStartingIndex + e.OldItems.Count + 1;
            if (newIndex > Tabs.Count) newIndex = -1;
            SelectedTabIndex = newIndex;
        }

        foreach (var item in e.OldItems)
        {
            Tabs.RemoveAt(e.OldStartingIndex);
        }
    }

    protected virtual void ItemsReplace(NotifyCollectionChangedEventArgs e)
    {
        ItemsRemove(e);
        ItemsAdd(e);
    }

    protected virtual void ItemsReset(NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in Tabs)
        {
            item.PropertyChanged -= TabItem_PropertyChanged;
        }

        Tabs.Clear();

        InitItems(ItemsSource);
    }

    protected virtual void Tabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    foreach (TabViewItem item in e.NewItems!)
                    {
                        item!.PropertyChanged += TabItem_PropertyChanged;

                        HeadersContainer?.Children.Insert(e.NewStartingIndex, item.TabViewHeaderItem);
                        ContentContainer?.Children.Insert(e.NewStartingIndex, item);
                    }

                    TabsInitialSelection(e);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                {
                    if (HeadersContainer is not null)
                    {
                        var header = HeadersContainer.Children.ElementAt(e.OldStartingIndex);
                        HeadersContainer.Children.Remove(header);
                        HeadersContainer.Children.Insert(e.NewStartingIndex, header);
                    }

                    if (ContentContainer is not null)
                    {
                        var content = ContentContainer.Children.ElementAt(e.OldStartingIndex);
                        ContentContainer.Children.Remove(content);
                        ContentContainer.Children.Insert(e.NewStartingIndex, content);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    foreach (TabViewItem item in e.OldItems!)
                    {
                        item!.PropertyChanged -= TabItem_PropertyChanged;

                        HeadersContainer?.Children.RemoveAt(e.OldStartingIndex);
                        ContentContainer?.Children.RemoveAt(e.OldStartingIndex);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                {
                    HeadersContainer?.Children.Clear();
                    ContentContainer?.Children.Clear();
                    SelectedTabIndex = -1;
                }
                break;
        }
    }

    protected virtual void TabsInitialSelection(NotifyCollectionChangedEventArgs e)
    {
        if (SelectedTabIndex == -1)
        {
            if (Tabs.Any(t => t.IsSelected))
            {
                SelectedTabIndex = Tabs.IndexOf(Tabs.First(t => t.IsSelected));
            }
            else
            {
                var tab = Tabs.First();
                if (!tab.IsEnabled) SelectClosestEnabledTab(tab);
                else SelectedTabIndex = 0;
            }
        }
        else
        {
            if (!Tabs.Any(t => t.IsSelected))
            {
                var tab = Tabs.ElementAtOrDefault(SelectedTabIndex);
                if (tab is null)
                {
                    if (Tabs.Count == 0) SelectedTabIndex = -1;
                    else SelectedTabIndex = 0;
                    return;
                }
                if (!tab.IsEnabled) SelectClosestEnabledTab(tab);
                else tab.IsSelected = true;
            }
            else if (e.NewStartingIndex <= SelectedTabIndex)
                SelectedTabIndex += (e.NewItems?.Count ?? 0) - (e.OldItems?.Count ?? 0);
        }
    }

    protected virtual void TabItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var tab = (sender as TabViewItem)!;

        if (e.PropertyName == TabViewItem.IsSelectedProperty.PropertyName && tab.IsSelected)
        {
            if (!tab.IsEnabled)
            {
                SelectClosestEnabledTab(tab);
                return;
            }

            SelectedTabIndex = Tabs.IndexOf(tab);

            if (HeadersScroll is null || !ScrollToSelectedTab) return;

            if (HeaderBarAlignment is Alignment.Top or Alignment.Bottom
                && HeadersContainer.Width > HeadersScroll.Width)
            {
                var max = HeadersContainer.Width - HeadersScroll.Width;
                var scrollTo = tab.TabViewHeaderItem.X - (HeadersScroll.Width - tab.TabViewHeaderItem.Width) / 2.0;
                scrollTo = Math.Max(0d, Math.Min(max, scrollTo));

                HeadersScroll.ScrollToAsync(scrollTo, 0d, true);
            }
            else if (HeaderBarAlignment is Alignment.Left or Alignment.Right
                && HeadersContainer.Height > HeadersScroll.Height)
            {
                var max = HeadersContainer.Height - HeadersScroll.Height;
                var scrollTo = tab.TabViewHeaderItem.Y - (HeadersScroll.Height - tab.TabViewHeaderItem.Height) / 2.0;
                scrollTo = Math.Max(0d, Math.Min(max, scrollTo));

                HeadersScroll.ScrollToAsync(0d, scrollTo, true);
            }
        }
        else if (e.PropertyName == TabViewItem.IsEnabledProperty.PropertyName)
        {
            if (!tab.IsEnabled && tab.IsSelected) SelectClosestEnabledTab(tab);
            else if (tab.IsEnabled && !Tabs.Any(t => t.IsSelected))
            {
                var index = Tabs.IndexOf(tab);
                SelectedTabIndex = index;
            }
        }
    }

    protected virtual void SelectClosestEnabledTab(TabViewItem tab)
    {
        var tabs = Tabs.Where(t => t.IsEnabled || t == tab).ToList();

        if (tabs.Count == 1)
        {
            SelectedTabIndex = -1;
            return;
        }

        var index = tabs.IndexOf(tab);

        tabs[index + (index == tabs.Count - 1 ? -1 : 1)].IsSelected = true;
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