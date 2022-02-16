using UnityEngine;

namespace sc.terrain.layersampler
{
    public static class DataComposer
    {
        /// <summary>
        /// Converts a world-space position to a normalized local-space XZ value (0-1 range)
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static Vector2 GetNormalizedPosition(Terrain terrain, Vector2 worldPosition)
        {
            //Position relative to terrain as 0-1 value
            return new Vector2(
                (worldPosition.x - terrain.GetPosition().x) / terrain.terrainData.size.x,
                (worldPosition.y - terrain.GetPosition().z) / terrain.terrainData.size.z);
        }

        /// <summary>
        /// Returns the texel indices of the splatmap map at a given position
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="position">Values should be normalized within 0-1 range. Use GetNormalizedPosition to convert a world-space position to this.</param>
        /// <returns></returns>
        public static Vector2Int SplatmapTexelIndex(Terrain terrain, Vector2 position)
        {
            return new Vector2Int(
                Mathf.CeilToInt(position.x * terrain.terrainData.alphamapWidth),
                Mathf.CeilToInt(position.y * terrain.terrainData.alphamapHeight) - 1
                );
        }

        private static int GetSplatmapID(int layerID)
        {
            if (layerID > 11) return 3;
            if (layerID > 7) return 2;
            if (layerID > 3) return 1;

            return 0;
        }

        //One extra row/column is added so that their center sits on a texel
        //An additional row/column is used to sample the data for any neighboring terrains. This makes it more straightforward to have seamless sampling on edges, rather than handling this in realtime
        private const int PADDING = 3;

        /// <summary>
        /// Creates cells for every splatmap texel and samples each channel into the data asset.
        /// Baking can be slow for high resolution splatmaps, particularly if there are multiple.
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="data"></param>
        /// <param name="downsampling"></param>
        public static void Bake(Terrain terrain, TerrainLayerData data, int downsampling = 1)
        {
            int resolution = terrain.terrainData.alphamapResolution / Mathf.Max(1, downsampling);

            float texelSize = 1f / resolution;

            int xCount = resolution + PADDING;
            int zCount = resolution + PADDING;
            int cellCount = xCount * zCount;

            float cellSize = (float)(terrain.terrainData.size.x) / ((float)resolution);
            float cellOffset = cellSize;

            //Setup data
            data.gridSize = new Vector2Int(xCount, zCount);
            data.cells = new TerrainLayerData.SplatmapCell[cellCount];
            data.cellSize = cellSize;
            data.terrainSize = new Vector2(terrain.terrainData.size.x, terrain.terrainData.size.z);
            data.origin = new Vector2(terrain.GetPosition().x, terrain.GetPosition().z);
            data.layerCount = terrain.terrainData.alphamapLayers;

            int i = 0;
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    #if UNITY_EDITOR
                    //Show progress bar every 10%
                    if (i % (cellCount / 10) == 0)
                    {
                        UnityEditor.EditorUtility.DisplayProgressBar("TerrainLayerData baking " + cellCount + " cells", terrain.name, (float)i / (float)cellCount);
                    }
                    #endif

                    //Offset position by one cell, which is the neighbor terrain cell
                    Vector2 wPos = new Vector2(
                        (terrain.GetPosition().x - cellOffset) + (x * cellSize),
                        (terrain.GetPosition().z - cellOffset) + (z * cellSize)
                        );

                    data.cells[i] = new TerrainLayerData.SplatmapCell();
                    data.cells[i].position = wPos;

                    SampleSplatmapWeights(terrain, wPos, data.cells[i]);

                    i++;
                }
            }

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorUtility.SetDirty(data);
            #endif

            //Debug.Log("Baked " + cellCount + " cells");
        }

        public static RectInt TexelToTerrain(Terrain terrain, RectInt texelRect)
        {
            //Incoming rect is in splatmap texel space. Offset and scale to transform to match terrain world-position

            int scaleX = Mathf.CeilToInt(terrain.terrainData.size.x / (terrain.terrainData.alphamapWidth));
            int scaleY = Mathf.CeilToInt(terrain.terrainData.size.z / (terrain.terrainData.alphamapHeight));

            //Rect is relative to the splatmap resolution, scale up to match terrain position
            texelRect.x *= scaleX;
            texelRect.y *= scaleY;

            //Relative to terrain
            texelRect.x += Mathf.CeilToInt(terrain.GetPosition().x);
            texelRect.y += Mathf.CeilToInt(terrain.GetPosition().z);

            texelRect.width *= scaleX;
            texelRect.height *= scaleX;

            //Center
            texelRect.x += (texelRect.width / 2);
            texelRect.y += (texelRect.height / 2);

            return texelRect;
        }

        public static Vector2 TexelToTerrain(int x, int y, Terrain terrain)
        {
            int scaleX = Mathf.CeilToInt(terrain.terrainData.size.x / (terrain.terrainData.alphamapWidth));
            int scaleY = Mathf.CeilToInt(terrain.terrainData.size.z / (terrain.terrainData.alphamapHeight));

            x -= 1;
            y -= 1;

            return new Vector2(
                (x * scaleX) + terrain.GetPosition().x,
                (y * scaleY) + terrain.GetPosition().z);
        }

        public static void BakeArea(Terrain terrain, TerrainLayerData data, RectInt texelregion)
        {
            if (data.cells.Length == 0)
            {
                #if TMS_DEV
                Debug.Log("No data was previously baked, performing a complete bake");
                #endif
                
                Bake(terrain, data);

                return;
            }

            /*
            texelregion = TexelToTerrain(terrain, texelregion);
            Debug.LogFormat("Baking area X:{0}->{1} - Y:{2}->{3}", texelregion.x, texelregion.x+texelregion.width, texelregion.y, texelregion.y+texelregion.height);
            */

            for (int x = texelregion.x; x <= texelregion.x + texelregion.width; x++)
            {
                for (int y = texelregion.y; y <= texelregion.y + texelregion.height; y++)
                {
                    int index = data.CoordsToCellIndex(x, y);
                    TerrainLayerData.SplatmapCell cell = data.cells[index];

                    SampleSplatmapWeights(terrain, TexelToTerrain(x, y, terrain), cell);

                    //Debug.Log(TexelToTerrain(x, y, terrain));
                }
            }

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(data);
            #endif
        }

        private static int m_splatmapIndex;
        private static Texture2D m_splatmap;
        private static Color m_splatTexel;
        private static Terrain m_targetTerrain;
        
        private static void SampleSplatmapWeights(Terrain terrain, Vector2 position, TerrainLayerData.SplatmapCell cell)
        {
            m_targetTerrain = terrain;

            //Select neighboring terrain if out of bounds
            if (position.x < terrain.GetPosition().x) m_targetTerrain = terrain.leftNeighbor;
            if (position.x > terrain.GetPosition().x + terrain.terrainData.size.x) m_targetTerrain = terrain.rightNeighbor;

            if (position.y < terrain.GetPosition().z) m_targetTerrain = terrain.bottomNeighbor;
            if (position.y > terrain.GetPosition().z + terrain.terrainData.size.z) m_targetTerrain = terrain.topNeighbor;

            //Exit if no neighboring terrain is present at the given position
            if (!m_targetTerrain)
            {
                //Dummy data for cells above the void
                cell.weights = new float[terrain.terrainData.alphamapTextureCount * 4];
                return;
            }

            //Convert position to UV-space for terrain (texel-space, really)
            Vector2 normalizedTerrainPos = GetNormalizedPosition(m_targetTerrain, position);

            //Point-sampled UV coordinates for splatmap texture
            Vector2Int splatmapTexelIndex = SplatmapTexelIndex(m_targetTerrain, normalizedTerrainPos);

            //Each splatmap having 4 color channels
            cell.weights = new float[m_targetTerrain.terrainData.alphamapTextureCount * 4];

            //Sample and store the weights of each splatmap channel, for each splatmap
            for (int j = 0; j < cell.weights.Length; j++)
            {
                m_splatmapIndex = GetSplatmapID(j);
                m_splatmap = m_targetTerrain.terrainData.GetAlphamapTexture(m_splatmapIndex);
                m_splatTexel = m_splatmap.GetPixel(splatmapTexelIndex.x, splatmapTexelIndex.y);

                cell.weights[j] = m_splatTexel[j % 4];
            }
        }
    }
}