# How to publish app to the Microsoft Store
Visual Studio has a bug in its "Create App Packages" wizard for WinUI 3 apps where it overwrites previous architecture's packages as it builds. So you end up with only a single MSIX for a single architecture instead of three MSIX's bundled together. To work around this:

1. Go through the "Create App Packages" flow N times, once for each architecture, making sure to save the MSIX in a different location.
2. Copy only the .msix files for each architecture into a shared location.
3. Run the following command: `& 'C:\path\to\makeappx.exe' bundle /bv <version, e.g. 2.0.0.0> /d <package location> /p WinBlur.App_<version>_x86_x64_arm64_bundle.msixbundle`

At this point you have a bundle that can be uploaded to the store. To include the symbol files for debug info, create a .msixupload file by doing the following:

1. Copy the .msixsym files for each architecture next to the .msixbundle created in the previous step.
2. Rename each .msixsym -> .appxsym (Partner Center doesn't understand .msixsym for some reason).
3. Select all the files and create a ZIP file
4. Rename the ZIP file to .msixupload

# Resources
https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool

https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps

https://stackoverflow.com/questions/73940053/how-do-i-publish-a-winui3-msix-bundle-to-the-microsoft-store