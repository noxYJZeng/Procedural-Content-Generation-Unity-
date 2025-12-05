# Project 3 — Procedural UFO Generator

**Author:** Xilu Zeng - Undergraduate

## Overview
This Unity project procedurally generates **UFO-style vehicles** with random geometry, color, and modular parts. Each UFO is created automatically from a single seed.

---

## Usage
1. Open the Unity project and load **SampleScene**.  
2. Press **Play** — the interface panel will appear.  
3. In the UI, you can:
   - Adjust **Main Seed** → regenerates the entire UFO fleet.  
   - Change **UFO Count** → sets how many to display.  
4. Use **keyboard controls** to explore:  
   - `W / S` – move forward / back  
   - `A / D` – strafe left / right  
   - `Q / E` – move down / up  
   - Mouse drag to look around 
---

## Features
- **Curved Surface Algorithm:** main body built using a **cubic Bézier surface of revolution** (custom implementation in `BezierPatchGenerator.cs`).  
- **Swappable Parts:**  
  - Dome – 3 variants  
  - Lower Pod – 3 variants  
  - Engine Core – 3 variants (rings / particles / beam)
- **Variation:** Randomized size, proportion, and color.  
- **Animation:** engine pulse, dynamic beam color, particle emission, gentle floating motion.  
- **Texture Mapping:** shared texture sets applied across UFO surfaces (graduate-level feature).  
- **UI Control:** adjust generation parameters interactively in real time.  
- **Camera Movement:** free-fly camera navigation with **WASD + QE** keys.  

---

## Summary
This project fulfills all undergraduate requirements for Project 3:
- Procedural generation of ≥ 5 vehicles  
- Single main random seed  
- Custom curved surface algorithm (cubic Bézier revolution)  
- Multiple swappable parts & visual variation  
- Clean geometry and animated effects
**Additionally, several graduate-level features are implemented:**
- Animated parts and dynamic particle systems  
- Texture mapping on vehicle surfaces  
