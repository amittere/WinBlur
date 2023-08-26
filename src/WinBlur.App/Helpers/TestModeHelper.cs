using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
using Windows.System;

namespace WinBlur.App.Helpers
{
    public class TestModeHelper : INotifyPropertyChanged
    {
        private bool testMode = false;
        public bool TestMode
        {
            get { return testMode; }
            set { testMode = value; NotifyPropertyChanged("TestMode"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddAccelerator(UIElement element)
        {
            var accelerator = new KeyboardAccelerator
            {
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                Key = VirtualKey.F12,
            };
            accelerator.Invoked += Accelerator_Invoked;

            element.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
            element.KeyboardAccelerators.Add(accelerator);
        }

        private void Accelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            TestMode = !TestMode;
            args.Handled = true;
        }
    }
}
