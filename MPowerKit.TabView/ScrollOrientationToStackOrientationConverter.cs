using System.Globalization;

namespace MPowerKit.TabView;

public class ScrollOrientationToStackOrientationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ScrollOrientation scrollOrientation)
        {
            return scrollOrientation switch
            {
                ScrollOrientation.Horizontal => StackOrientation.Horizontal,
                ScrollOrientation.Vertical => StackOrientation.Vertical,
                _ => throw new ArgumentOutOfRangeException(nameof(scrollOrientation), scrollOrientation, null)
            };
        }

        return StackOrientation.Horizontal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}