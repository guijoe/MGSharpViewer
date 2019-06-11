# README #

Hello world! Welcome to MG#Viewer, a Unity-based 3D viewer for simulations outputs by MG#Core.

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/Invagination.GIF "Set Model")

### Brief Description ###

* The parent MG# project enables anyone with minimum or no programming experience to run biological simulations and visualise the results. 
* MG#Core is the computational engine of MG#. It runs simulations and logs results into (custom) MG files and/or VTK files.
* MG#Viewer is a Unity(https://unity.com/) based viewer allowing 3D visualisation of MG log files. VTK files can be viewed with third party tools like ParaView(https://www.paraview.org/).

### Running a simulation with MG# ###

* MG#Core can be used with any leading PC Operation Systems (Windows, Linux, Mac OS) thanks to the cross-platform abilities of .NET.
* Building simulations with MG# requires installing .NET Core SDK (https://dotnet.microsoft.com/download). Windows users may also make use of the native .NET framework.
* A simulation can only be runned from a "Main" code file (file with the "Main" function). The "Main" file must be compiled, and the resulting executable file runned. This goes without saying that the path to the file must be referenced, either by being in its parent directory or by explicitly specifying it in the file path.
    
#### Windows
	dotnet run
	invagination.exe
	
#### Linux 
	dotnet run
	./invagination
	
#### Mac OS 
    	dotnet run
	./invagination

### Designing and programming a simulation ###

* Programming a new simulation is as simple as creating a new class inheriting from the Simulator class. The tutorial presented throughout this text describes the code that produces the Invagination simulation showcased above.

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/Invagination.PNG "Set Model")
	
* Any class inheriting from Simulator must override its Abstract methods, which are mandatory. The following code adds these methods to our new class, including the Main method, to make this class runnable

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/Methods.PNG "Methods")

#### Main(string[] args)
* The Main method is the entry point of the program. 
* In this example, an instance of simulator is dynamical created, intialised, and setup and runned, while also logging its parameters and results (the code is meant to self-explanatory). 
* From one simulator child class to another, the only required changes here are the type of the simulator instance (line 30 - mandatory) and its name (line 31 - optional). The rest of the code remains the same.

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/Main.PNG "Main method")

#### SetupSimulation()
* This method aims mainly at creating a cell population with a given spatial arragement of cells. 
* Other simulation parameters like simulation time and log frequency may also be set in this method. 
* In this example, a cell population made of a single tissue of 81 hexagonal (epithelial) cells is created. 
* All 81 cells will reside on the first layer of a 3D hexagonal grid of 9 layers (9x9x9 = 81x9 = 729 = popMaxSize - lines 63, 73, 74) 

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/SetupSimulation.PNG "Setup Simulation")

#### SetInitialConditions()
* This method aims at assigning to cells an initial behaviour. 
* Further behaviours may also be implemented in the Update method, at specific time points (frames) for example.
* In this example, all cells chosen out of a subset of the population are programmed to constrict apically from the start of the simulation

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/SetInitialConditions.PNG "Set initial conditions")

#### Update()
* This method submits cells to their programmed behaviours by calculating their states at each time step.
* New cellular behaviours may be added at specific time points with a conditional instruction. For example,
    if(frame == 1000) cellPopulation.Proliferation(true); 
* The method "DynamiseSystem()" of class CellPopulation (line 81) handles most of the computational effort (neighbours search, forces evaluation, new cell states) 
* However, specific actions may also be applied to cells individually.
* The Update() method logs computational results every %logFrequency% frames.

#### SetModel()
* This method set model global parameters.
* The default values showcased in this example would produce results for most simple use cases.

![Alt text](https://github.com/guijoe/MGSharpViewer/blob/master/images/SetModel.PNG "Set Model")
