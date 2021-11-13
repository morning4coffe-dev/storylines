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

### Adding a new language (requires Visual Studio 2019 and Multilingual App Toolkit)
- Ensure you have Visual Studio 2019 and the [Multilingual App Toolkit extension](https://marketplace.visualstudio.com/items?itemName=MultilingualAppToolkit.MultilingualAppToolkit-18308).
- Fork and clone this repo.
- Open in VS 2019.
- Right click on the `Storylines` project.
- Select Multilingual App Toolkit > Add translation language.
    - If you get a message saying "Translation Provider Manager Issue," just click Ok and ignore it. It's unrelated to adding a language.
- Select a language. 
- Once you select a language, new `.xlf` files will be created in the `MultilingualResources` folder.
- Now follow the `Improving an existing language` steps below.

### Improving an existing language (can be done with any text editor)
- Inside the `MultilingualResources` folder, open the `.xlf` of the language you want to translate.
    - You can open using any text editor, or you can use the [Multilingual Editor](https://developer.microsoft.com/windows/develop/multilingual-app-toolkit)
- If you're using a text editor, translate the strings inside the `<target>` node. Then change the `state` property to `translated`.
    ![](https://github.com/morning4coffe-dev/storylines/blob/d47dd56aa43dbbd0ce2ba38038ae93fc1e9e5504/Logo%20and%20Screenshots/GitHub/code-translation.png)
- If you're using the Multilingual Editor, translate the strings inside the `Translation` text field. Make sure to save to preserve your changes.
<img src="https://github.com/morning4coffe-dev/storylines/blob/d47dd56aa43dbbd0ce2ba38038ae93fc1e9e5504/Logo%20and%20Screenshots/GitHub/multilingual-app-toolkit.png" width="650">
- Once you're done, commit your changes, push to GitHub, and make a pull request.
