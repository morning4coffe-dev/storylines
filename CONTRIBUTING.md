# How to Contribute:

You can contribute to Storylines by:
- Report issues and bugs [here](https://github.com/morning4coffe-dev/Storylines/issues/new?template=bug_report.md)
- Submit feature requests [here](https://github.com/morning4coffe-dev/Storylines/issues/new?template=feature_request.md)
- Creating a pull request.
- Internationalization and localization:
    * See instructions below.

# How to Build and Run Storylines from source

* Make sure your machine is running on Windows 10 1903+.
* Make sure you have Visual Studio 2019 16.2+ installed.
* Make sure you have the "Universal Windows Platform development" component installed for Visual Studio.
* Open src/Storylines.sln with Visual Studio and set Solution Platform to x64(amd64).
* Once opened, right-click on the solution and click on "Restore NuGet Packages".
* Now you should be able to build and run Storylines on your machine. If it fails, try to close the solution and reopen it again.
**If x64 doesn't work, use the architecture of your system*

# Internationalization and localization
  
 * Since Storylines is still preview - there will be changes to strings and new ones will be added over time. Whenever that happens, you will be notified.
 * Here are the steps you need to follow if you want to contribute:
  1. Make sure you can build and run Storylines on your machine so that you can test during your work.
  2. Click [here](https://github.com/morning4coffe-dev/storylines/discussions/9) and provide some information about the chosen language.
  3. Continuously test your app while translating to make sure you are not breaking any existing layout.
  4. When you are finished, create a new PR.
