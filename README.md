# Trails Studio

Trails Studio is an application that allows users to design BMX dirt jump courses in a 3D visualized environment. [cite: 2, 16].

## Installation

1. Navigate to the project's GitHub Releases page.
2. Download the latest zipped build folder.
3. Extract the contents to your preferred directory.
4. To run Trails Studio, run the `TrailsStudio.exe` binary [cite: 6]. You will be taken to the main menu [cite: 6].

## Main Menu

In the main menu, you have multiple buttons available [cite: 8]:
* **New Line**: Opens the Line Initialization window where you can input the name of the line and parameters of its roll-in [cite: 14, 15]. If the calculated exit speed is sufficient, clicking 'Build' takes you to the Studio [cite: 16, 17].
* **Load Line**: Opens a window with all present saves listed [cite: 19]. Select an existing save and press 'Load' to enter the Studio, or delete saves as needed [cite: 20, 21].
* **Settings**: Allows you to adjust module-specific application settings [cite: 26, 27]. Reckless use of settings can break the application, but a 'Restore Defaults' button is available [cite: 23, 25].

## Studio Usage

After entering the Studio, you are presented with a view of the line and a sidebar containing building and deleting tools [cite: 31].

### Camera Control
* The default view of the Studio can be moved around along a path on top of the line [cite: 44].
* Use the **WASD** keys to move around relative to the direction you are looking [cite: 45].
* Click and hold the **right mouse button** and drag to change your looking direction [cite: 46].

### Examining Elements
* **Obstacles**: Click on an obstacle to view it from any direction (holding right-click to drag) and see an information tooltip with its parameter values [cite: 48, 49, 50].
  
  ![Obstacle Detail View](docs/img/obstacle_detail.png) [cite: 52]

* **Slope Changes**: Check the "Show slope change information" checkbox in the sidebar to display start and end points (green spheres) and information tooltips for all present slope changes [cite: 65, 66].
  
  ![Slope Change Detail View](docs/img/slope_detail.png) [cite: 67]

### Position and Build Controls

Clicking "New Jump" or "New Slope Change" switches the application to a position & build state with a top-down view [cite: 141].
* **Anchoring**: The application moves the object based on your mouse input [cite: 143]. Press the **left mouse button** to anchor the positioned object in place so you can adjust parameters without moving it [cite: 143, 146]. An anchor icon will appear in the bottom left corner [cite: 147].
* **Slope Change**: Move the mouse to position the start of the slope change and adjust its length and height difference to affect the exit speed [cite: 153, 154, 156].
  
  ![Slope Change Position & Build](docs/img/slope_build.png) [cite: 158]

* **Takeoff**: Control the takeoff's position with the mouse, and adjust defining parameters (radius, height) as well as cosmetic parameters (width, thickness) [cite: 173, 175]. Click 'Build' to move to the landing [cite: 176].
  
  ![Takeoff Position & Build](docs/img/takeoff_build.png) [cite: 179]

* **Landing**: The application calculates viable positions and slopes [cite: 198]. Hover over a position to switch to it, adjust height and rotation, and click 'Build' to finish the jump [cite: 199, 201, 204].
  
  ![Landing Position & Build](docs/img/landing_build.png) [cite: 205]

### Deleting Elements
* **Delete Jump**: Press the "Delete Jump" button and hover over obstacles to enter delete mode [cite: 230]. A green highlight indicates it can be deleted (generally only the last obstacle in the line) [cite: 231, 232]. Deleting a takeoff also deletes its landing [cite: 235].
* **Delete Slope**: Only untouched slope changes (with no obstacles built on or after them) can be deleted via the "Delete Slope" button [cite: 238, 239].

### Generating PDF Reports

Trails Studio offers the ability to generate a PDF document containing the textual representation of the designed line, which is useful for real-world building [cite: 95, 96].
* Press the **Escape** key to open the escape menu [cite: 40].
* Click **Save Textual Representation** to open a file explorer and choose the save location [cite: 89, 90, 97].
  
  ![Line Preview](docs/img/line_for_pdf.png) [cite: 103, 105]
  
  ![Generated PDF Report](docs/img/generated_pdf.png) [cite: 106, 139]
