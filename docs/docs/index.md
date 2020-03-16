---
title: Welcome
summary: Application daemon written in .NET core.
authors:
    - Tomas Hellström (@helto4real)
---
# NetDaemon

This is the application daemon project for Home Assistant. This project makes it possible to make automatons using the .NET Core (3.1) framework. The API uses the new nullable types and C# 8.0. Please make your self familiar with these concepts.

Why a new application daemon for Home Assistant? There already exists one!? The existing appdaemon is a great software and are using python as language and ecosystem. This is for people who loves to code in the .NET core ecosystem and c#. The daemon will be supported by all supported platforms of .NET core.

## Pre-Alpha - Expect things to change

!!! warning
    This is in pre-alpha experimental phase and expect API:s to change over time. Please use and contribute ideas for improvement or better yet PR:s.

Only tested on amd64 based architectures like PC or NUC and Raspberry PI 3. Probably works on other architectures too. Will update the docs when I get confirmations from other platforms.

The daemon is currently only distributed through Hassio add-on but a docker container and instruction to run locally will be provided in time.

Please see [the getting started](/netdaemon/Getting%20started/1_getting%20started/) documentation for setup.

!!! info
    You need to restart the add-on every time you change a file. C# needs to compile the changes.

## Issues

If you have issues or suggestions of improvements, please [add an issue](https://github.com/net-daemon/netdaemon/issues)

## Discuss the NetDaemon

Please [join the Discord server](https://discord.gg/K3xwfcX) to get support or if you want to contribute and help others.

## Async model

The application daemon are built entirely on the async model of .NET. This requires some knowledge of async/await/Tasks to use it properly. The docs will give you tips with do and don´ts around this but I strongly suggest you read the official docs.  [Here is a good start to read about async model.](https://docs.microsoft.com/en-us/dotnet/csharp/async)
