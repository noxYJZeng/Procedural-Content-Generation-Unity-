Project: Procedural Plant Generator(Undergraduate)

This project creates fully procedural plants and a dynamic grassy ground in Unity.
Each plant grows from parameters like trunk height, curvature, and branching order.
The scene also includes a generated grass mesh that uses layered Perlin noise to create natural hills and ground variations.

Main Scripts:
PlantSpawner.cs – Main entry point. Generates multiple plants and the ground platform automatically.
ProceduralPlant.cs – Builds the trunk and all branch orders using procedural meshes.
TubeMesh.cs – Creates tubular meshes for the branches and trunk.
LeafCanopyDecorator.cs – Adds leaves procedurally on branch tips.
PlantSpawnerUI.cs – A simple runtime UI to adjust the random seed and regenerate plants directly in play mode.

Features:
Procedural tree trunk and branching structure with adjustable parameters
Bark material using PBR textures (albedo, normal, height maps)
Branches get natural tapering (thicker near base, thinner near tip) within a single branch.
Gravity causes downward bending for long/heavy branches near the trunk base, producing realistic drooping.
Procedural leaf placement with optional material customization
Procedurally generated grass platform with layered Perlin noise for hills and dips
Real-time seed control through on-screen UI (change seed, regenerate instantly)

How to Run:
1.Open the Unity project in Unity 2022.3.
3.Click Play.
4.Use the “Plant Runtime Controls” UI in the Game view to adjust the random seed or regenerate plants.
5.Each time you change the seed or press Generate, new plants and ground will appear.
