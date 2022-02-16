# Terrain Layer Sampler for Unity
Scripting functionality to efficiently read terrain layer weights in realtime.

![Image](https://i.imgur.com/cD285sQ.gif "Example")

## Motivations

The painted strength of a terrain layer is stored in a channel of an RGBA splatmap texture. Reading this back using Texture2D.GetPixel is too slow for realtime uses.

Instead this script can pre-bake this information into a ScriptableObject, and provides an API for sampling the data at any given world-space position. The weight/strength for each terrain layer is returned as an array.

- Sampling is performed using bi-linear interpolation, providing smoothly interpolated data between splatmap texels.
- Data is automatically updated for areas that are modified through terrain texture painting. This ensures fast updates and retains common workflows.

## Example use cases

- Controlling particle effects for feet and/or wheels.
- Driving audio events or parameters.
- Deriving physics properties from the terrain surface (eg. checking if the ground is icy or wet).
- Prefab spawning, based on textures.

# Adding it to a project

**via Package Manager:**

- Press the "+" icon in the top-left
- Choose "Add package from git URL"
- Paste in `https://github.com/staggartcreations/UnityTerrainLayerSampler.git`.

**via Git:**

- Clone the repo as usual via cmd/terminal or in your favourite Git GUI software.
- Checkout the 'master' branch

**via browser**
- Click the green "Code" button at the top of the page, and choose "Download ZIP"
- Extract the folder to your Assets folder

## Usage instructions
- Add the `TerrainLayerComponent` to your terrain(s)

*A `TerrainLayerData` asset will automatically be created, and will be saved on the component with the scene. You can however choose to use a ScriptableObject that is saved on disk. In order to do so, right-click in the Project window and choose `Create/TerrainLayerData` and assign it. The context menu on the component has a "Bake" option.*

- See the `Example/TerrainLayerSampler` script for further practical details.

**Scripting Workflow**
- Find a reference to the terrain you are on (either through Raycasting, or a custom look-up method). This is likely needed anyway (eg. checking if feet are grounded).
- Once found, the `TerrainLayerComponent` can be fetched through GetComponent, and its `data` field can be accessed. 
- Finally, the `data.Sample(Vector3 position, bool interpolated)` function can be called, which returns a `float[]` array. Each array element corresponds to a layer on the terrain (in the same order as they are added to it). The float values represent the strength a layer at the given position (always between 0 and 1).

