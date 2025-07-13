// This script is added to each article's HTML content to detect
// keyboard shortcuts and forward them to the host app as WebView2 host messages.
document.addEventListener('keydown', (event) => {
    console.log("keydown: ${event.key}");
    if (event.key === 'j') {
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'NextArticle' });
    }
    else if (event.key === 'k') {
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'PreviousArticle' });
    }
    else if (event.key === 'J' && event.shiftKey) {
        // Shift + J pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'NextSite' });
    }
    else if (event.key === 'K' && event.shiftKey) {
        // Shift + K pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'PreviousSite' });
    }
    else if (event.key === 'o' && event.ctrlKey) {
        // Ctrl + O pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'OpenInBrowser' });
    }
    else if (event.key === 'r' && event.ctrlKey) {
        // Ctrl + R pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'MarkArticleAsRead' });
    }
    else if (event.key === 'R' && event.ctrlKey && event.shiftKey) {
        // Ctrl + Shift + R pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'MarkArticleAsUnread' });
    }
    else if (event.key === 's' && event.ctrlKey) {
        // Ctrl + S pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'SaveArticle' });
    }
    else if (event.key === 'S' && event.ctrlKey && event.shiftKey) {
        // Ctrl + D pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'UnsaveArticle' });
    }
    else if (event.key === 'h' && event.ctrlKey) {
        // Ctrl + H pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'ShareArticle' });
    }
    else if (event.key === 'F5') {
        if (event.ctrlKey) {
            // Ctrl + F5 pressed
            event.preventDefault();
            window.chrome.webview.postMessage({ 'WinBlur-Action': 'RefreshSubscriptions' });
        }
        else {
            // F5 pressed
            event.preventDefault();
            window.chrome.webview.postMessage({ 'WinBlur-Action': 'RefreshFeed' });
        }
    }
    else if (event.key === 'a' && event.ctrlKey) {
        // Ctrl + A pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'MarkFeedAsRead' });
    }
    else if (event.key === 'A' && event.ctrlKey && event.shiftKey) {
        // Ctrl + Shift + A pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'MarkAllAsRead' });
    }
    else if (event.key === 'n' && event.ctrlKey) {
        // Ctrl + N pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'AddSite' });
    }
    else if (event.key === 'N' && event.ctrlKey && event.shiftKey) {
        // Ctrl + Shift + N pressed
        event.preventDefault();
        window.chrome.webview.postMessage({ 'WinBlur-Action': 'AddFolder' });
    }
});
