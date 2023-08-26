using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;

namespace WinBlur.App.Helpers
{
    public class SplitterContentPresenter : ContentPresenter
    {
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
        }
    }
}
