# Shiny Calculator

![anim2](https://user-images.githubusercontent.com/752380/113320774-318e4080-9313-11eb-8436-394f67c507e4.gif)

A REPL calculator that helps developers with everyday tasks that require different types of calculations. 

This project aims to provide a handy tool to learn binary, prototype different bit hacking solutions, learn assembly and how the CPU works, and have a tool that can solve problems from different domains.

Shiny Calculator is written in C# and targets .net 5.0

## Features

- Number calculations
- Bit manipulation (with explain mode)
- Assembly code execution (the engine features an x86 assembly simulator)
- Text manipulation and parsing
- Blocks that enable the user to write programs that mix all high-level expressions with low-level assembly
- User-friendly error messages
- Tests and diagnostics that are integrated into the tool
- Others

Assembly code execution is currently limited to a handful of instructions, but it will support most of the instructions that current compilers use to generate code.

## Installation

Clone the project and build it using .net 5 by running:

```csharp
dotnet build -c Release
```

The executable should be located in: 

```
..\Shiny.Calculator\src\bin\Release\net5.0\Shiny.Calculator.exe
```

The calculator uses VT-100 mode, which means you should run in a terminal that supports this mode like Windows Terminal.

![obraz](https://user-images.githubusercontent.com/752380/113300667-70fe6200-92fe-11eb-9569-e5b827d43778.png)

## Feature Notes

- Commands

The calculator has many commands that are useful like: 
```
help
Displays the help screen
```
```
explain
Enables the explain mode where each sub-expression is displayed as a binary result
```
```
cls
Clears the screen
```
```
vars
Displays the declared variables
```
```
regs
Displays the x86 registers
```
```
mem
Displays the x86 memory
```

- Blocks

Blocks allow writing programs in the calculator; a block will execute all of the commands sequentially.

![anim3](https://user-images.githubusercontent.com/752380/113322672-4370e300-9315-11eb-8cf1-5ce562bc398c.gif)

A block starts with ```{``` and ends with ```}```. 
Each time a start block symbol is used, the REPL will switch from single-line mode to multi-line mode. 

- X86 assembly emulation

You can write X86 assembly code and mix it with calulation expresions.

![anim4](https://user-images.githubusercontent.com/752380/113322699-4ff53b80-9315-11eb-865d-054a2e157961.gif)

- Error Messages

![anim5](https://user-images.githubusercontent.com/752380/113323268-107b1f00-9316-11eb-8bb0-9114e0b94667.gif)


## Copyright

Copyright © 2021 Bartosz Adamczewski
