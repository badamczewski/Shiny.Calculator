# Shiny Calculator

![anim1](https://user-images.githubusercontent.com/752380/113292334-a900a780-92f4-11eb-8c05-58876ac2648c.gif)

A REPL calculator that helps developers with everyday tasks that require different types of calculations. 

This project aims to provide a handy tool to learn binary, prototype different bit hacking solutions, learn assembly and how the CPU works, and have a tool that can solve problems from different domains.

Shiny Calculator is written in C# and targets .net 5.0

## Features

- Number calculations
- Bit manipulation (with explain mode)
- Assembly code execution (the engine features an x86 assembly simulator)
- Text manipulation and parsing
- Blocks that enable the user to write programs that mix all high-level expressions with low-level assembly
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

![obraz](https://user-images.githubusercontent.com/752380/113301594-7ad49500-92ff-11eb-979a-be31d854523c.png)

A block starts with ```{``` and ends with ```}```. 
Each time a start block symbol is used, the REPL will switch from single-line mode to multi-line mode. 

## Copyright

Copyright © 2021 Bartosz Adamczewski
