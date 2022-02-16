using System;
using UnityEngine;

namespace sc.terrain.layersampler
{
    [CreateAssetMenu(fileName = "TerrainLayerData", menuName = "TerrainLayerData", order = 0)]
    public class TerrainLayerData : ScriptableObject
    {
        public float cellSize;
        public Vector2Int gridSize;
        public Vector2 terrainSize;
        public Vector2 origin;
        public int layerCount;

        private static Vector2 m_gridPos;
        private static Vector2Int m_gridIndex;
        [NonSerialized]
        private float[] m_weights = new float[0];

        [Serializable]
        public class SplatmapCell
        {
            public Vector2 position;
            public float[] weights;
        }

        public SplatmapCell[] cells = new SplatmapCell[0];

        public Vector2 SnapToGrid(Vector2 pos)
        {
            float Snap(float coord) => Mathf.FloorToInt((coord + (cellSize * 0.5f)) / cellSize) * (cellSize);

            m_gridPos.x = Snap(pos.x);
            m_gridPos.y = Snap(pos.y);

            return m_gridPos;
        }

        private Vector2 SnapToGridIntersection(Vector2 pos)
        {
            float Snap(float coord) => Mathf.CeilToInt(coord / cellSize) * (cellSize);

            m_gridPos.x = Snap(pos.x);
            m_gridPos.y = Snap(pos.y);

            return m_gridPos;
        }

        private Vector2Int PositionToCellIndex(Vector2 position)
        {
            Vector2 normalizedPos = new Vector2((position.x - origin.x + (cellSize * 0.5f)) / terrainSize.x, (position.y - origin.y + (cellSize * 0.5f)) / terrainSize.y);

            m_gridIndex.x = Mathf.FloorToInt((terrainSize.x / cellSize) * normalizedPos.x);
            m_gridIndex.y = Mathf.FloorToInt((terrainSize.y / cellSize) * normalizedPos.y);

            //Clamp, in case out of bounds
            m_gridIndex.x = Mathf.Max(m_gridIndex.x, 0);
            m_gridIndex.y = Mathf.Max(m_gridIndex.y, 0);
            m_gridIndex.y = Mathf.Min(m_gridIndex.y, gridSize.y - 1);
            m_gridIndex.x = Mathf.Min(m_gridIndex.x, gridSize.x - 1);

            return m_gridIndex;
        }

        public int CoordsToCellIndex(int x, int y)
        {
            //Clamp, in case out of bounds
            x = Mathf.Max(x, 0);
            y = Mathf.Min(y, gridSize.y - 1);

            return x * gridSize.x + y;
        }

        private static float BiLerp(float bottomLeft, float bottomRight, float topLeft, float topRight, float tx, float ty)
        {
            return Mathf.Lerp(Mathf.Lerp(bottomLeft, bottomRight, tx), Mathf.Lerp(topLeft, topRight, tx), ty);
        }

        public static float NormalizedDistance(float start, float end, float position)
        {
            return (position - start) / (end - start);
        }

        public struct BilinearData
        {
            public float tX;
            public float tY;

            public SplatmapCell topLeft;
            public SplatmapCell topRight;
            public SplatmapCell bottomRight;
            public SplatmapCell bottomLeft;
        }
        private static BilinearData bd;

        private enum DiagonalDirections
        {
            TopLeft,
            TopRight,
            BottomRight,
            BottomLeft
        }

        private static readonly Vector2[] DiagonalVectors =
        {
            new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1),
        };

        public BilinearData CreateBilinearData(Vector3 position)
        {
            m_gridPos.x = position.x;
            m_gridPos.y = position.z;

            return CreateBilinearData(m_gridPos);
        }

        private SplatmapCell GetNeighbour(Vector2 center, DiagonalDirections direction, float offset)
        {
            Vector2Int indices = PositionToCellIndex(center + (DiagonalVectors[(int)direction] * offset));

            return cells[CoordsToCellIndex(indices.x, indices.y)];
        }

        public BilinearData CreateBilinearData(Vector2 position)
        {
            //Snap position to the nearest grid intersection point on the grid
            Vector2 center = SnapToGridIntersection(m_gridPos);

            //Offsetting by this much in a diagonal direction finds the center of the neighboring cells
            var cellExtends = cellSize * 0.5f;

            //Fetch cells surrounding the grid intersection point (2x2)
            bd.topLeft = GetNeighbour(center, DiagonalDirections.TopLeft, cellExtends);
            bd.topRight = GetNeighbour(center, DiagonalDirections.TopRight, cellExtends);
            bd.bottomRight = GetNeighbour(center, DiagonalDirections.BottomRight, cellExtends);
            bd.bottomLeft = GetNeighbour(center, DiagonalDirections.BottomLeft, cellExtends);

            //Normalized (0-1) distance of sample point (in world-space), relative to the XY axis of the area
            bd.tX = NormalizedDistance(bd.bottomLeft.position.x, bd.bottomRight.position.x, position.x);
            bd.tY = NormalizedDistance(bd.bottomLeft.position.y, bd.topLeft.position.y, position.y);

            return bd;
        }

        private float Interpolate(BilinearData data, int index)
        {
            return BiLerp(
                data.bottomLeft.weights[index],
                data.bottomRight.weights[index],
                data.topLeft.weights[index],
                data.topRight.weights[index],
                data.tX, data.tY);
        }

        /// <summary>
        /// Given a world-space position, returns the (interpolated) weights of all the painted terrain layers
        /// </summary>
        /// <param name="position"></param>
        /// <param name="interpolated"></param>
        /// <returns></returns>
        public float[] Sample(Vector3 position, bool interpolated)
        {
            m_gridPos.x = position.x;
            m_gridPos.y = position.z;

            m_gridIndex = PositionToCellIndex(m_gridPos);
            
            //Point sampling
            if (!interpolated)
            {
                return cells[CoordsToCellIndex(m_gridIndex.x, m_gridIndex.y)].weights;
            }

            //Bilinear sampling

            //Create array once, initially. Or when baking was performed with more or less layers
            if (m_weights.Length != layerCount) m_weights = new float[layerCount];

            BilinearData bilinearData = CreateBilinearData(m_gridPos);

            for (int i = 0; i < layerCount; i++)
            {
                m_weights[i] = Interpolate(bilinearData, i);
            }

            return m_weights;
        }
    }
}