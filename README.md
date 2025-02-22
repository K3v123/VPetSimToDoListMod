# VPetSimToDoListMod
Vpet game (steam) mod (incomplete)

# VPet To-Do List Mod

I lost the original saves for this project, but fortunately, VS Code was still open, preserving the core files. While testing the VPet simulator, the game crashed due to my mod. I'm still troubleshooting the issue and working on a fix.

For now, you can **run the `.exe`** if you just want to test it.  
This was a fun project, and I plan to revisit it in the future.

---

## 🛠️ **How to Set Up & Run This Project**

### **1️⃣ Install .NET SDK (if not installed)**
This project uses **.NET 7.0**, so make sure you have the SDK installed.  
[Download .NET SDK](https://dotnet.microsoft.com/en-us/download)

You can check if .NET is installed by running:
```
dotnet --version
 Build the Project
```
Since .deps.json is missing, you need to restore dependencies first:

dotnet restore

Then build it:

`dotnet build`**

If the build is successful, you’ll find the .exe in:

`bin\Debug\net7.0-windows\VPetToDoListMod.exe`**

Run the Project

To run it directly from the terminal:

`dotnet run`**

Or manually open bin\Debug\net7.0-windows\VPetToDoListMod.exe.

 Troubleshooting

    If you get an error MSBUILD : error MSB1009: Project file does not exist, make sure you're inside the correct directory:

`cd path/to/VPetToDoListMod`**

Then try building again.

If dotnet restore fails, try deleting the obj/ and bin/ folders, then run:

`dotnet clean`**
`dotnet restore``**
