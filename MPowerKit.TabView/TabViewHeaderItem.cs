using System.Runtime.CompilerServices;

namespace MPowerKit.TabView;

public class TabViewHeaderItem : Grid
{
    protected readonly DataTemplate StringTemplate = new(() =>
    {
        var lbl = new Label()
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        lbl.SetBinding(Label.TextProperty, ".");
        lbl.SetBinding(Label.TextColorProperty, new Binding(TabView.HeaderTextColorProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontFamilyProperty, new Binding(TabView.HeaderFontFamilyProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontSizeProperty, new Binding(TabView.HeaderFontSizeProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontAttributesProperty, new Binding(TabView.HeaderFontAttributesProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        return new ContentView()
        {
            Content = lbl,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(20, 10)
        };
    });

    protected View? SelectedView;
    protected View? UnselectedView;

    public TabViewHeaderItem()
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += Tap_Tapped;
        GestureRecognizers.Add(tap);

        SetBinding(SelectedContentTemplateProperty, new Binding(TabView.SelectedHeaderTemplateProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        SetBinding(ContentTemplateProperty, new Binding(TabView.HeaderTemplateProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        SetBinding(HideWhenDisabledProperty, new Binding(TabView.HideTabsWhenDisabledProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
    }

    private void Tap_Tapped(object? sender, EventArgs e)
    {
        if (!IsEnabled) return;

        if (!IsSelected) IsSelected = true;
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == HideWhenDisabledProperty.PropertyName)
        {
            IsVisible = !HideWhenDisabled || IsEnabled;
            return;
        }

        if (propertyName == HeaderContentProperty.PropertyName && HeaderContent is View content)
        {
            Children.Clear();
            Children.Add(content);
        }

        if (HeaderContent is View) return;

        else if (propertyName == IsSelectedProperty.PropertyName)
        {
            SetBinding(View.BackgroundColorProperty, new Binding(IsSelected
                ? TabView.SelectedHeaderBackgroundColorProperty.PropertyName
                : TabView.HeaderBackgroundColorProperty.PropertyName, source: RelativeBindingSource.TemplatedParent));
        }
        else if (propertyName == HeaderContentProperty.PropertyName
            || propertyName == SelectedContentTemplateProperty.PropertyName
            || propertyName == ContentTemplateProperty.PropertyName)
        {
            InitContent();
        }
    }

    public virtual void InitContent()
    {
        var unselectedTemplate = ContentTemplate != null
            ? (ContentTemplate is DataTemplateSelector selectorU ? selectorU.SelectTemplate(HeaderContent, null) : ContentTemplate)
            : StringTemplate;

        var selectedTemplate = SelectedContentTemplate != null
            ? (SelectedContentTemplate is DataTemplateSelector selectorS ? selectorS.SelectTemplate(HeaderContent, null) : SelectedContentTemplate)
            : unselectedTemplate;

        var context = HeaderContent;
        context ??= "Empty Header";

        SelectedView = selectedTemplate.CreateContent() as View;
        SelectedView!.BindingContext = selectedTemplate == StringTemplate ? context.ToString() : context;

        UnselectedView = unselectedTemplate.CreateContent() as View;
        UnselectedView!.BindingContext = unselectedTemplate == StringTemplate ? context.ToString() : context;

        SelectedView.SetBinding(View.IsVisibleProperty, new Binding(IsSelectedProperty.PropertyName, source: this));
        UnselectedView.SetBinding(View.IsVisibleProperty, new Binding(IsSelectedProperty.PropertyName, source: this, converter: new InverseBooleanConverter()));

        Children.Clear();
        Children.Add(SelectedView);
        Children.Add(UnselectedView);
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
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region HeaderContent
    public object HeaderContent
    {
        get { return (object)GetValue(HeaderContentProperty); }
        set { SetValue(HeaderContentProperty, value); }
    }

    public static readonly BindableProperty HeaderContentProperty =
        BindableProperty.Create(
            nameof(HeaderContent),
            typeof(object),
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region SelectedContentTemplate
    public DataTemplate SelectedContentTemplate
    {
        get { return (DataTemplate)GetValue(SelectedContentTemplateProperty); }
        set { SetValue(SelectedContentTemplateProperty, value); }
    }

    public static readonly BindableProperty SelectedContentTemplateProperty =
        BindableProperty.Create(
            nameof(SelectedContentTemplate),
            typeof(DataTemplate),
            typeof(TabViewHeaderItem)
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
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region HideWhenDisabled
    public bool HideWhenDisabled
    {
        get { return (bool)GetValue(HideWhenDisabledProperty); }
        set { SetValue(HideWhenDisabledProperty, value); }
    }

    public static readonly BindableProperty HideWhenDisabledProperty =
        BindableProperty.Create(
            nameof(HideWhenDisabled),
            typeof(bool),
            typeof(TabViewHeaderItem),
            true
            );
    #endregion
}