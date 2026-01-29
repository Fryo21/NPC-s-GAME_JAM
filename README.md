# Face of the Future  
**Kingston University Game Jam Winner 2025**

Face of the Future is a systems-driven surveillance and decision-making game prototype developed during the Kingston University Game Jam. The player does not directly control a character, but instead oversees an automated policing system, making high-stakes decisions under uncertainty as pressure escalates over time.

The project focuses on system design, state management, and player-facing consequences rather than content scale or visual fidelity.

---

## Core Gameplay
- Monitor autonomous surveillance drones scanning a moving population
- Identify and respond to suspects generated from a dynamic wanted list
- Balance speed, accuracy, and risk as system reliability degrades
- Manage economic consequences for correct and incorrect decisions
- Survive escalating rounds while avoiding system-driven failure states

---

## Technical Highlights
- Event-driven architecture connecting gameplay, UI, audio, and feedback systems  
- Modular manager-based design (Rounds, Drones, Economy, UI, Audio)  
- Data-driven NPC and wanted list system using ScriptableObjects  
- Scalable autonomous drone system with accuracy decay and false positives  
- Coroutine-based flow for timers, scanning intervals, and transitions  
- Animation-safe UI state handling using explicit completion callbacks  
- Reusable UI interaction and feedback components using DOTween  
- Layered music system with dynamic intensity transitions  
- Robust reset and failure handling without scene reloads  

---

## Controls
- **Mouse Click** — Select NPC / Interact with UI  
- **UI Buttons** — Confirm or cancel arrests, purchase drones, progress rounds  
- **ESC** — Open pause menu  

---

## Build
A playable Windows build is available in the **Releases** section of this repository.

No additional setup is required.

---

## Development Team
- **Dave Murray** — Lead Designer / Programmer  
- **Francis Obiokala** — Gameplay Programmer  
- **Ash Methven** — UI / Game Programmer / Artist 
- **Leo** — Gameplay Programmer  

---

## Notes
This project was created as a rapid prototype during a game jam, with an emphasis on system interaction, emergent pressure, and technical architecture rather than polish or content breadth.
