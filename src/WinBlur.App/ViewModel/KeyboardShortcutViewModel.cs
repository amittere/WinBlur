using System;
using System.Collections.Generic;

namespace WinBlur.App.ViewModel
{
    public class KeyboardShortcutGroup
    {
        public string Category { get; set; }
        public List<KeyboardShortcutViewModel> Shortcuts { get; set; }
    }

    /// <summary>
    /// ViewModel representing a keyboard shortcut and its description.
    /// </summary>
    public class KeyboardShortcutViewModel
    {
        /// <summary>
        /// The keyboard shortcut (e.g. "Ctrl + N").
        /// </summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// Description of what the shortcut does (e.g. "Add site").
        /// </summary>
        public string Description { get; set; }
    }
}
