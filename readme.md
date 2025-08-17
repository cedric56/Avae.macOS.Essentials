# Avae.macOS.Essentials

A port of Microsoft.Maui.Essentials tailored for Avalonia.macOS, bringing essential cross-platform APIs to your Avalonia-based macOS applications.

# Features

Cross-Platform Essentials: Leverage APIs from Microsoft.Maui.Essentials adapted for macOS environments.

Lightweight Configuration: Minimal setup required for integration into your Avalonia.macOS project.

MIT Licensed: Freely use, modify, and distribute under the permissive MIT License.

# Getting Started

Follow these steps to integrate Avae.macOS.Essentials into your Avalonia.macOS project.

# Prerequisites

An Avalonia.macOS project set up with .NET.

# Installation

1. Add Microsoft.Maui.Essentials to Your Shared Project

In your shared project’s .csproj file, include the Microsoft.Maui.Essentials package. Use one of the following methods
````
<UseMauiEssentials>true</UseMauiEssentials>
````
OR
````
<PackageReference Include="Microsoft.Maui.Essentials">
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
````

# Configuration

2. Add the following to your info.plist

NSCameraUsageDescription

NSMicrophoneUsageDescription

CFDisplayBundleName

NSLocationWhenInUseUsageDescription

NSLocationAlwaysUsageDescription

# Usage

Once installed, you can use Microsoft.Maui.Essentials APIs within your Avalonia.macOS application. For example, access Geolocation.

# Example: Accessing Geolocation
````
using Microsoft.Maui.Essentials;

var location = await Geolocation.GetLocationAsync();

````
# Built With

This package builds upon the excellent work of:

Microsoft.Maui.Essentials

AvaloniaUI

Avalonia.Essentials

# Note

Camera doesn't works in Debug mode

# License

Avae.macOS.Essentials is licensed under the MIT License.

# Contributing

Contributions are welcome! Please submit issues or pull requests to the GitHub repository. Ensure your code follows the project’s coding standards.

# Acknowledgments

Thanks to the Avalonia team for their robust UI framework.

Gratitude to the MAUI team for providing cross-platform essentials.