using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEngine.TerrainTools;
#else
using UnityEngine.Experimental.TerrainAPI;
#endif

namespace sc.terrain.materialsampler
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Terrain))]
    public class TerrainLayerComponent : MonoBehaviour
    {
        public TerrainLayerData data;
        
        public Terrain terrain;

        /// <summary>
        /// Whenever texture painting data changes on the terrain, re-bake the material data.
        /// </summary>
        [Tooltip("Whenever texture painting data changes on the terrain, re-bake the material data.")]
        public bool autoBake = true;

        void Reset()
        {
            terrain = GetComponent<Terrain>();
            
            CreateDataAsset();
            Bake();
        }

        private void OnEnable()
        {
            TerrainCallbacks.textureChanged += OnSplatmapModified;

            #if TMS_DEV
            Bake();
            #endif
        }

        private void OnDisable()
        {
            TerrainCallbacks.textureChanged -= OnSplatmapModified;
        }

        private void OnSplatmapModified(Terrain targetTerrain, string textureName, RectInt texelregion, bool synced)
        {
            if (autoBake && synced && targetTerrain == terrain)
            {
                DataComposer.BakeArea(terrain, data, texelregion);
            }
        }

        /// <summary>
        /// Create a new (and empty) data asset to bake the material weights to. Use this only at runtime, since the data won't persist.
        /// Requires calling the "Bake" method afterwards...
        /// </summary>
        [ContextMenu("Create data asset (saves with scene)")]
        public void CreateDataAsset()
        {
            data = (TerrainLayerData)ScriptableObject.CreateInstance(typeof(TerrainLayerData));
            data.name = this.name + "_LayerData";
        }

        /// <summary>
        /// Creates a cell for every splatmap texel, and samples the weight of every splatmap channel to an array
        /// </summary>
        [ContextMenu("Bake")]
        public void Bake()
        {
            if(data) DataComposer.Bake(terrain, data);
        }
    }
}