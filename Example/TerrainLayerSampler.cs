using System;
using UnityEngine;
using sc.terrain.layersampler;

/// <summary>
/// An example MonoBehaviour that uses the API to sample a position and retrieve a set of of weights as a float array
/// </summary>
[ExecuteInEditMode]
public class TerrainLayerSampler : MonoBehaviour
{
    public LayerMask terrainLayer = -1;
    
    public enum TerrainLayers
    {
        Snow,
        Dirt,
        Grass,
        Sand
    }

    [Header("Example outputs")]
    [Range(0f, 1f)]
    public float grassAmount;
    [Range(0f, 1f)]
    public float dirtAmount;
    [Range(0f, 1f)]
    public float snowAmount;
    [Range(0f, 1f)]
    public float sandAmount;
    
    // This array is where the sampled data is going to be stored into. It is initialized/resized when needed by the TerrainLayerData.Sample function
    // The float elements in this array correspond to how strongly a terrain material is painted at the sampled position
    // The first layer added to the terrain is represented by the first array element, and so forth. Here an Enum is used to identify them, it's been mirrored with the terrain in the example scene.
    private float[] weights;
    private TerrainLayerData layerData;

    private Vector3 samplePosition;

    private void Update()
    {
        samplePosition = this.transform.position;
        
        /// Lazy method! Raycasts down at the position to find a terrain with a TerrainMaterialComponent on it
        /// This component should be set up on terrains in the editor, or at the start of the game!
        /// If the TerrainLayerData is missing, a bake is performed
        ValidateSetup(samplePosition);
        
        ReadTerrainMaterialWeights();
    }

    private void ReadTerrainMaterialWeights()
    {
        /// Lazy runtime function. Finds a terrain below the position and gets a TerrainMaterialComponent reference from it.
        layerData = FindData(samplePosition);
        
        weights = layerData.Sample(samplePosition, true);
        
        grassAmount = weights[(int)TerrainLayers.Grass];
        dirtAmount = weights[(int)TerrainLayers.Dirt];
        snowAmount = weights[(int)TerrainLayers.Snow];
        sandAmount = weights[(int)TerrainLayers.Sand];
    }

    private static RaycastHit hit;
    private static Ray ray = new Ray();
    private static TerrainLayerData m_Data;
    
    private TerrainLayerData FindData(Vector3 position)
    {
        ray.direction = Vector3.down;
        ray.origin = position + (Vector3.up * 1000f);

        if (Physics.Raycast(ray, out hit, 2000, (int)terrainLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetType() == typeof(TerrainCollider))
            {
                m_Data = hit.collider.GetComponent<TerrainLayerComponent>().data;
            }
        }

        return m_Data;
    }
    
    private static void ValidateSetup(Vector3 position)
    {
        ray.direction = Vector3.down;
        ray.origin = position + (Vector3.up * 1000f);
        
        if (Physics.Raycast(ray, out hit, 2000, -1, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetType() == typeof(TerrainCollider))
            {
                TerrainLayerComponent comp = hit.collider.GetComponent<TerrainLayerComponent>();

                if (!comp)
                {
                    comp = hit.collider.gameObject.AddComponent<TerrainLayerComponent>();

                    comp.terrain = hit.collider.GetComponent<Terrain>();
                    comp.CreateDataAsset();
                    comp.Bake();
                }
                else
                {
                    if (!comp.data)
                    {
                        comp.CreateDataAsset();
                        comp.Bake();
                    }
                }
            }
        }
    }
}
