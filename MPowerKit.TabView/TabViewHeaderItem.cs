using System.Runtime.CompilerServices;

namespace MPowerKit.TabView;

internal class TabViewHeaderItem : Grid
{
    private readonly DataTemplate _stringTemplate = new(() =>
    {
        var lbl = new Label()
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        lbl.SetBinding(Label.TextProperty, ".");
        lbl.SetBinding(Label.TextColorProperty, new Binding("HeaderTextColor", source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontFamilyProperty, new Binding("HeaderFontFamily", source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontSizeProperty, new Binding("HeaderFontSize", source: RelativeBindingSource.TemplatedParent));
        lbl.SetBinding(Label.FontAttributesProperty, new Binding("HeaderFontAttributes", source: RelativeBindingSource.TemplatedParent));
        return new ContentView()
        {
            Content = lbl,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(20, 0)
        };
    });

    private View _selectedView;
    private View _unselectedView;

    public TabViewHeaderItem()
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += Tap_Tapped;
        this.GestureRecognizers.Add(tap);

        this.SetBinding(SelectedContentTemplateProperty, new Binding("SelectedHeaderTemplate", source: RelativeBindingSource.TemplatedParent));
        this.SetBinding(ContentTemplateProperty, new Binding("HeaderTemplate", source: RelativeBindingSource.TemplatedParent));
    }

    private void Tap_Tapped(object? sender, EventArgs e)
    {
        if (!this.IsSelected) this.IsSelected = true;
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == HeaderContentProperty.PropertyName && HeaderContent is View content)
        {
            this.Children.Clear();
            this.Children.Add(content);
        }

        if (HeaderContent is View) return;

        else if (propertyName == IsSelectedProperty.PropertyName)
        {
            if (IsSelected) this.SetBinding(View.BackgroundColorProperty, new Binding("SelectedHeaderBackgroundColor", source: RelativeBindingSource.TemplatedParent));
            else this.SetBinding(View.BackgroundColorProperty, new Binding("HeaderBackgroundColor", source: RelativeBindingSource.TemplatedParent));

            _selectedView.IsVisible = this.IsSelected;
            _unselectedView.IsVisible = !this.IsSelected;
        }
        else if (propertyName == HeaderContentProperty.PropertyName
            || propertyName == SelectedContentTemplateProperty.PropertyName
            || propertyName == ContentTemplateProperty.PropertyName)
        {
            InitContent();
        }
    }

    internal void InitContent()
    {
        var unselectedTemplate = ContentTemplate != null
                                 ? (ContentTemplate is DataTemplateSelector selectorU ? selectorU.SelectTemplate(HeaderContent, null) : ContentTemplate)
                                 : _stringTemplate;

        var selectedTemplate = SelectedContentTemplate != null
                               ? (SelectedContentTemplate is DataTemplateSelector selectorS ? selectorS.SelectTemplate(HeaderContent, null) : SelectedContentTemplate)
                               : unselectedTemplate;

        var context = HeaderContent;
        context ??= "Empty Header";

        _selectedView = selectedTemplate.CreateContent() as View;
        _selectedView.BindingContext = selectedTemplate == _stringTemplate ? context.ToString() : context;

        _unselectedView = unselectedTemplate.CreateContent() as View;
        _unselectedView.BindingContext = unselectedTemplate == _stringTemplate ? context.ToString() : context;

        _selectedView.IsVisible = this.IsSelected;
        _unselectedView.IsVisible = !this.IsSelected;

        this.Children.Clear();
        this.Children.Add(_selectedView);
        this.Children.Add(_unselectedView);
    }

    #region IsSelected
    internal bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    internal static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region HeaderContent
    internal object HeaderContent
    {
        get { return (object)GetValue(HeaderContentProperty); }
        set { SetValue(HeaderContentProperty, value); }
    }

    internal static readonly BindableProperty HeaderContentProperty =
        BindableProperty.Create(
            nameof(HeaderContent),
            typeof(object),
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region SelectedContentTemplate
    internal DataTemplate SelectedContentTemplate
    {
        get { return (DataTemplate)GetValue(SelectedContentTemplateProperty); }
        set { SetValue(SelectedContentTemplateProperty, value); }
    }

    internal static readonly BindableProperty SelectedContentTemplateProperty =
        BindableProperty.Create(
            nameof(SelectedContentTemplate),
            typeof(DataTemplate),
            typeof(TabViewHeaderItem)
            );
    #endregion

    #region ContentTemplate
    internal DataTemplate ContentTemplate
    {
        get { return (DataTemplate)GetValue(ContentTemplateProperty); }
        set { SetValue(ContentTemplateProperty, value); }
    }

    internal static readonly BindableProperty ContentTemplateProperty =
        BindableProperty.Create(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(TabViewHeaderItem)
            );
    #endregion
}