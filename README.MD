# Font Registry Manager

**Font Registry Manager** is a lightweight and user-friendly Windows application for managing font files under
`%LOCALAPPDATA%\Microsoft\Windows\Fonts\` and their corresponding registry entries at `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts`.

It provides a visual tree-based interface that allows you to:

- Organize fonts using folders and subfolders.
- Register or unregister fonts with a single click.
- Automatically synchronize font file structure with registry entries.

## Captures
![FontRegistryManager](asserts/main.png|height=200)
![Font Files in Explorer](asserts/tree.png|height=200)
![Font Registry](asserts/registry.png|height=200)


## Requirements

- **Windows 10 1803 or higher**, fonts for the current user is only support since [Windows 10 1803](https://superuser.com/questions/1658678/detect-path-of-font-on-windows).
- **.NET Framework 4.0 or higher**, pre-installed on Windows 10.

## Getting Started

### 1. Prepare Font Files

- Place your `.ttf` / `.otf`/`.ttc` font files in `%LOCALAPPDATA%\Microsoft\Windows\Fonts\`  organized by folders (Grouped by Font Family or Font Usage).or any directory you like.
- You can rename the font files and group them as needed.

### 2. Launch the Application

- Check the fonts you wish to install.
- The program will create corresponding registry entries to activate the fonts.

### 3. Uninstall Fonts

- Uncheck the fonts you no longer need.
- The program will automatically remove the associated registry entries.

## For Developers

### Debug

1. Use [Visual Studio Code](https://code.visualstudio.com/) with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension (will auto-install the required [.NET Install Tool](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime)).
2. Open the project folder and press `F5` to start debugging.
3. The correct `.NET` runtime will be automatically acquired based on `.csproj` configuration.

### Build

Make sure you have the [.NET SDK](https://dotnet.microsoft.com/download/dotnet) installed.
Then run:

```batch
dotnet publish -c Release
```

## Acknowledgements

[ContextMenuManager](https://github.com/BluePointLilac/ContextMenuManager)
[FontBase](https://forum.fontba.se/t/ability-to-keep-font-activated-even-if-fonbase-is-exited/1672)
[Best font manager for 2025 for windows 11?](https://www.reddit.com/r/typography/comments/1i3h9u5/comment/m7mzztu/)

## Donate

This program is completely free. If you find it helpful, you can make a donation by scanning the QR code below (WeChat) to support the project.

![Buy me a coffee](asserts/donate-wechat.png=250x250)
