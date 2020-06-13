![SpellFire logo](Resources/SpellFire.png)

*Bringing back the magic!*

## **What is it exactly?**
System that lets you chime into World of Warcraft inner mechanisms
and command game using programming language.

Right now only supported WoW version is **3.3.5a (build 12340)**, however structure of the project should easily let you adapt to other version.

Project goal is to provide clean and maintainable API of a game instrumentation.

Framework includes protection measures against Blizzard's Warden Anti-Cheat system, which should make usage immensely secure. Note however, that you can **never guarantee total undetectability**.

[You can see it in action here.](https://kkovs.github.io/SpellFire/Resources/Demo/SpellFire_demo.mp4)

---
Radar
![screenshot1](Resources/Demo/Screenshot1.png)

![screenshot2](Resources/Demo/Screenshot2.png)

---
Warden Anti-Cheat bypass
![screenshot2](Resources/Demo/WB_Screenshot1.png)

## **How does it work?**
The system entails **two** components working together, their general description:

- **Primer**, an application that contains business logic(i.e. routines that specify what game should do and when).
- **Well**, a library that is injected into orchestrated process and exposes API that **Primer** consumes as well as contains business model.

Following technologies were used for implementation:
- **.NET Framework**
- **Json .NET**
- **EasyHook**

## **How can I use it?**
- Configure to your needs or use already shipped logic
- Build using modern .NET toolchain
- Start World of Warcraft
- Run **Primer.exe** (ensure **Well.dll** and **EasyHook binaries** are in the same directory as **Primer.exe**)
- ???

*Enjoy **enhanced** game!*

*PS. Please, use this software in a good will* 😉

---
### **Credits**
Developed by Krzysztof Kowalski

Shared under MIT license

This project is made possible thanks to:
- Community at OwnedCore.com, GitHub.com and other sites
- Reversing and development software
- *All collaborators and supporters* 😊
