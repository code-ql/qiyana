using Avalonia;
using Avalonia.Controls;

namespace qiyana.Views;

public class EqualHeightPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
            return new Size();

        var childHeight = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height / Children.Count;
        var maxW = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, childHeight));
            maxW = System.Math.Max(maxW, child.DesiredSize.Width);
        }
        return new Size(maxW, availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
            return finalSize;

        var childHeight = finalSize.Height / Children.Count;
        for (int i = 0; i < Children.Count; i++)
            Children[i].Arrange(new Rect(0, i * childHeight, finalSize.Width, childHeight));
        return finalSize;
    }
}
