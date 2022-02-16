# Terrain Layer Sampler for Unity
Scripting functionality to efficiently read terrain layer weights in realtime.

![Image](https://i.imgur.com/cD285sQ.gif "Example")

## Motivations

The painted strength of a terrain layer is stored in a channel of an RGBA splatmap texture. Reading this back using Texture2D.GetPixel is too slow for realtime uses.

Instead this script can pre-bake this information into a ScriptableObject, and provides an API for sampling the data at any given world-space position, and returns the weights for each terrain layer.

- Sampling is performed using bi-linear interpolation, providing smoothly interpolated data between splatmap texels.
- Data is automatically re-baked after terrain texture painting modifications

## Use cases

- Controlling particle effects for feet and wheels
- Driving audio events or parameters
- Deriving physics properties for the terrain surface (eg. checking if the ground is icy or wet)

# Getting the it

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
- See the `Example/TerrainLayerSampler` script for further details. 

*A `TerrainLayerData` asset will automatically be added, and will be save with the scene. You can however choose to use a ScriptableObject that is saved on disk. In order to do so, right-click in the Project window and choose `Create/TerrainLayerData`.*

*Note that a single 256px splatmap can already take up ~5mb of disk space*

- A reference to a `TerrainLayerData` is always required to sample data. It always belongs to a specific terrain and is referenced on the `TerrainLayerComponent` component.

