# TextRPG

A text-based role-playing game written in C#. You can also use [this game's engine](https://github.com/sprintingkiwi/TextRPG/blob/main/TextRPG.cs) to build your own Text RPG.
I might write a tutorial in the wiki.

## Release
The official release of my game can be found here: https://sprintingkiwi.itch.io/hood-and-horns

## Instructions for developers
In this repositories there are two Visual Studio (2019) solutions.
* TextRPG is the library
* hood_and_horns is the actual game

For the game solution to be successfully compiled, the TextRPG library must be compiled first. Then, in the hood_and_horns solution, a reference to the TextRPG.dll compiled file must be added.
