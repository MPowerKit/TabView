# MPowerKit.TabView
Fully customizable .NET MAUI TabView

It is written using .NET MAUI without native code and it is fully compatible with all platforms MAUI supports

[![NuGet](https://img.shields.io/nuget/v/MPowerKit.TabView.svg?maxAge=2592000)](https://www.nuget.org/packages/MPowerKit.TabView)

### Initialize:
Make sure to initialize the UI of the TabView inside your App.xaml file:

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                
                <tabview:TabViewStyle />
                
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>

### Usage:

You can create tabs from markup or from ViewModel specifying ItemsSource and describing DataTemplate and header templates in xaml.
Note: ItemsSource has higher priority than directly markup tab creation.

### Example of usage:

Here described two types of usage:
https://github.com/MPowerKit/TabView/tree/main/Sample/MainPage.xaml

### Note:
You cannot use typeof Page as tab template.
