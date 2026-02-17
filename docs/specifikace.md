## TRAILS STUDIO

*Aplikace s 3D vizualnim prostredim, ktera je schopna uzivateli na zaklade vstupnich dat rozjezdu umoznit namodelovat lajnu BMX skoku.*

### SPECIFIKACE 

#### OVERVIEW
+ Aplikace se sklada z konfiguracniho main menu (WPF) a samotneho line builderu, ktery bude implementovan pomoci WPF 3D nebo Unity.
+ Pod line builder spada fyzikalni engine, ktery bude provadet kalkulace treni a dalsich sil pusobicich na kolo projizdejici vytvorenymi prekazkami a tim urcovat spravne parametry dalsich skoku a jinych prekazek
+ Take pod nej spada terrain editor, skrze ktery se nastavi parametry prostredi, ve kterem se budou prekazky stavet.
+ Nakonec pod nim lezi i samotny editor prekazek, ktery do prostredi umoznuje vkladat a upravovat skoky a jine prekazky.

#### USE-CASES
+ Uzivatel chce vytvorit novy spot
	+ Klikne na tlacitko "Vytvorit spot" v main menu
	+ Je tazan na parametry rozjezdu
	+ Nachazi se v terrain editoru, kde muze upravovat spot podle sebe
+ Uzivatel chce postavit skok	
	+ Klikne na tlacitko "Postavit skok" v toolboxu
	+ V terrain editoru se mu zvyrazni mista, kde je mozne dle fyzikalnich vypoctu odraz postavit
	+ Po postaveni odrazu je mozne ho dale editovat
	+ Pote se stavi dopad a rovnez jsou zvyraznena mista, kde je mozne dopad postavit
	+ Dopad se po postaveni take muze dal editovat
+ Uzivatel chce postavit jinou prekazku
	+ Uzivatel klikne na tlacitko "Postavit prekazku"
	+ A) Jednodussi varianta:
		+ Zobrazi se mu dialog, kde vybere jednu z moznych predem definovanych typu prekazek (klopena zatacka, boule, wallride, quarter pipe..)
		+ Tu umisti tam, kam mu je to dovoleno a muze ji dale editovat
	+ B) Slozitejsi varianta:
		+ Uzivatel vymodeluje prekazku kompletne podle sebe (fyzikalni engine bude muset poznat, vsechny parametry prekazky z toho, jak bude vytvorena)
		+ *Nejspise by vyzadovalo o dost slozitejsi integraci s fyzikalnim modelem
+ Uzivatel chce zmerit vzdalenost mezi dvema body na spotu (dulezite pro realne vyuziti aplikace)
	+ Klikne na tlacitko "Zmerit vzdalenost"
	+ Umisti pocatecni bod mereni
	+ Umisti koncovy bod mereni
	+ Je mu zobrazena vzdalenost mezi temito body
+ Uzivatel chce vygenerovat lajnu od posledni postavene prekazky nebo rozjezdu do nejakeho koncoveho bodu
	+ Klikne na tlacitko "Vygenerovat lajnu"
	+ Umisti koncovy bod, kam se ma lajna vygenerovat
	+ Zobrazi se mu dialog, kde nastavi koeficienty pro generovani (napr. slozitost skoku, clenitost prekazek, delka mezer mezi jednotlivymi prekazkami, ...)
+ Uzivatel chce editovat jiz umisteny skok/prekazku
	+ Klikne na danou prekazku
	+ A) Na prekazce se zobrazi sipky, za ktere uzivatel muze tahat a tim ji upravovat
	+ B) Zobrazi se dialog, kde bude mozne menit parametry prekazky (naklon, smer, radius, vyska, sirka, ...)
	
To maintain a strong software engineering perspective in your thesis, the ideal next section after "Choice of Framework" should be **"3.2 Application Architecture & Design Patterns"**.

While your draft currently has "Managing States" as section 3.2, that is just one part of the picture. From a software engineering standpoint, after selecting your tool (Unity), you must explain **how you structured your code** before you explain **what features you built** (like terrain editing or mesh generation).

This approach allows you to introduce your core classes (`Managers`, `StateController`) as the "skeleton" of the application before fleshing out the specific features.

### Proposed Structure for Section 3.2

Here is how you can detail your code in this section using the files you provided:

#### 3.2.1 Core Architectural Patterns

Explain that you chose a **Manager-based architecture** to centralize control over key subsystems.

* **The Singleton Pattern:**
* **Concept:** Discuss how you used `Singleton<T>` to provide global access to unique systems like `CameraManager`, `PhysicsManager`, and `TerrainManager`.
* **SW Engineering Justification:** Explain that while Singletons can introduce coupling, they are standard in Unity development for managers that must persist across the application's lifecycle.
* **Code Reference:** Cite `CameraManager : Singleton<CameraManager>` and `PhysicsManager : Singleton<PhysicsManager>` as the central access points for the rest of the application.



#### 3.2.2 Application Logic Control (State Pattern)

This is where your original "Managing States" content fits perfectly.

* **The State Machine:**
* **Concept:** Describe how `StateController.cs` manages the high-level application flow (e.g., switching between "Default View" and "Build Mode").
* **Implementation:** Discuss how `StateController` holds the `CurrentState` and handles transitions via `ChangeState(State newState)`.
* **SW Engineering Justification:** This pattern encapsulates behavior specific to a mode (e.g., locking the camera when building) and adheres to the **Open/Closed Principle**—you can add new states without modifying the core controller.



#### 3.2.3 Event-Driven Communication

Explain how different parts of your system talk to each other without being tightly coupled.

* **Observer Pattern (C# Events):**
* **Concept:** Describe how objects notify the system of changes.
* **Code Reference:** Cite `ObstacleBase.cs`. It defines events like `HeightChanged` or `PositionChanged`.
* **Why it matters:** This allows the UI or Physics engine to react to a ramp being resized without the ramp needing to know *who* is listening.



### Why this is the "Ideal" Next Step

1. **Logical Flow:** You go from **Framework** (Unity)  **Architecture** (Singletons/States)  **Implementation** (Mesh Gen/Physics).
2. **Academic Rigor:** Discussing *Design Patterns* (Singleton, State, Template Method) shows you understand software engineering theory, not just coding.
3. **Context for Later Sections:** When you discuss "Terrain Editing" in section 3.4, the reader will already understand *why* you access `TerrainManager.Instance` to do it.


Based on the structure of the *Gutvald* thesis (which your supervisor approved) and the code you provided, the ideal next section is **3.2 Line Representation and Data Structure**.

In the *Gutvald* thesis, the Analysis chapter moves from **Choice of Engine** directly into **Map Representation** (or Grid System). It skips abstract "Architecture" discussions and goes straight to the core data structure that makes the application work.

For **Trails Studio**, your "Map" is the **Line**. Your code (`Line.cs`, `ILineElement`) is the backbone of the entire project. Before you can discuss "Managing States" (interaction) or "Mesh Generation" (visuals), you must analyze how the course is actually stored in memory.

### Proposed Section: 3.2 Line Representation and Data Structure

This section should analyze how you solved the problem of storing a sequential, verifiable BMX course.

#### What to include (citing your code):

1. **The Sequential List Approach:**
* Explain why you chose a `List<ILineElement>` rather than a graph or grid.
* **Analysis:** A BMX line is linear and sequential (Start  Takeoff  Landing). The data structure reflects this physical reality.
* **Code Reference:** Discuss `ILineElement` interface which allows different objects (Roll-ins, Jumps) to be treated uniformly in the list.


2. **Spline Integration:**
* Explain how the visual path is calculated.
* **Analysis:** You didn't just store points; you used a `SplineContainer` to generate a smooth path through the `ILineElement` transforms.
* **Code Reference:** Analyze the `RebuildSpline()` method, specifically how it adds `BezierKnot`s based on obstacle positions to create the camera path.


3. **The "Active" vs. "Underlying" Object Problem:**
* This is a specific engineering challenge you solved. You have the *visual/logic* object (`ObstacleBase`) and the *underlying* terrain/GameObject.
* **Code Reference:** Discuss `TryGetLineElementFromGameObject` and how you handle the parent-child relationship for complex objects like the Roll-in.



### Why this fits the "Perfect Thesis" style:

* **It is Targeted:** It talks about *your* specific problem (storing a BMX line), not generic "Software Architecture."
* **It is Foundational:** You cannot build meshes (3.3) or calculate physics (3.5) without defined `ILineElements`.
* **It mirrors the Reference:** Just as Gutvald likely discussed how he stored his Tower Defense grid/map immediately after choosing Unity, you discuss how you store the Jump Line.

### Revised Table of Contents for Chapter 3:

* 3.1 Choice of Framework
* **3.2 Line Representation and Data Structure** (New recommendation)
* 3.3 Managing Application States (Renamed from "Managing States" to be more specific)
* 3.4 Obstacle Procedural Mesh Generation
* 3.5 ...


Here is a draft for **Section 3.2** of your thesis. It is written to match the specific, code-focused style of the *Gutvald* reference thesis, analyzing **how** you solved the problem of storing and representing the course rather than discussing generic theory.

---

## 3.2 Line Representation and Data Structure

To facilitate the sequential nature of a BMX course design—where one obstacle logically follows another—we needed a robust data structure to manage the state of the course. Unlike a game level which might use a grid or a spatial graph, a dirt jump line is inherently linear. This section analyzes the implementation of the `Line` class and the data structures used to represent the course in memory.

### 3.2.1 The Line Collection

The core of the application's data model is the `Line` class, which serves as a centralized manager for all obstacles. We implemented the `Line` as a singleton to ensure global accessibility. To store the sequence of obstacles, we chose a standard generic list `List<ILineElement>` over more complex structures like linked lists or graphs.

This decision was driven by the specific requirements of the domain:

1. **Ordered Access:** The building process requires frequent access to the "last" element to determine where the next one can be placed.
2. **Iteration:** Features like physics calculation and spline generation require iterating through the entire line from start to finish. The `Line` class implements `IEnumerable<ILineElement>`, allowing external managers to traverse the course using standard `foreach` loops without exposing the internal list structure.

The list holds objects implementing the `ILineElement` interface. This abstraction allows the system to treat different types of objects—such as the starting `RollIn` or subsequent jumps—uniformly. For example, the `GetLastLineElement()` method returns the `ILineElement` interface, abstracting away the specific concrete type of the obstacle.

### 3.2.2 Visual Path Representation (Splines)

While `List<ILineElement>` manages the logical order of obstacles, the visual representation of the ride path requires a continuous curve. To achieve this, we integrated the `SplineContainer` component from the Unity Splines package.

We implemented a synchronization method, `RebuildSpline()`, which translates the discrete positions of `ILineElement` objects into a continuous Bézier curve. This method iterates through the `lineElements` collection and generates a new `BezierKnot` for specific points of interest on each obstacle, such as the start point and end point.

A specific challenge in this implementation was calculating the curvature (tangents) of the spline to ensure it smoothly follows the obstacles. We solved this by algorithmically setting the `TangentIn` and `TangentOut` of each knot based on the rotation of the obstacle. The length of these tangents is dynamically calculated as a fraction of the obstacle's length (specifically one-third), ensuring that the curve remains proportional regardless of the obstacle's size.

### 3.2.3 Object Identification and Mapping

A significant engineering challenge arose from the separation between the **visual/collision geometry** (GameObjects in the Unity scene) and the **logical data** (C# classes implementing `ILineElement`). When a user interacts with the scene (e.g., clicking to select an obstacle), the application receives a reference to a `GameObject` (specifically a `MeshCollider`).

To resolve this, we implemented the `TryGetLineElementFromGameObject(GameObject go)` method. This method performs a lookup to map the scene object back to its corresponding logical `ILineElement`. It handles complex hierarchies, such as the `RollIn`, where the user might click on a child object (like a pillar) rather than the root object. The method traverses the transform hierarchy upwards until it finds a registered element or reaches the root, ensuring robust selection behavior even for composite 3D models.