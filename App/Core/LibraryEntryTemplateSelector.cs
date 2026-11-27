using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FilmField.Services;

public sealed partial class LibraryEntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LibraryTemplate { get; set; }

    public DataTemplate? PosterTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is LibraryEntry { IsLibrary: true } ? LibraryTemplate : PosterTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
