using System.Windows;
using System.Windows.Controls;

namespace AndroidDesktop.Controls;

/// <summary>Keeps native viewport content attached while static shell tabs change.</summary>
public sealed class PersistentTabContentHost : Grid
{
    private TabControl? _owner;
    private readonly Dictionary<TabItem, ContentPresenter> _pages = [];
    public PersistentTabContentHost()
    {
        Loaded += OnLoaded;
        Unloaded += (_, _) => { if (_owner is not null) _owner.SelectionChanged -= SelectionChanged; };
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (TemplatedParent is not TabControl owner) return;
        _owner = owner;
        owner.SelectionChanged -= SelectionChanged;
        owner.SelectionChanged += SelectionChanged;
        foreach (var tab in owner.Items.OfType<TabItem>())
        {
            if (_pages.ContainsKey(tab)) continue;
            var page = new ContentPresenter { Content = tab.Content, ContentTemplate = tab.ContentTemplate,
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            _pages.Add(tab, page); Children.Add(page);
        }
        UpdateVisibility();
    }
    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, _owner)) UpdateVisibility();
    }
    private void UpdateVisibility()
    {
        foreach (var (tab, page) in _pages)
            page.Visibility = tab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
    }
}
