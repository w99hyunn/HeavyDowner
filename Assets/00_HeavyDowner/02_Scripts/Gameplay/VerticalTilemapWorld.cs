using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [RequireComponent(typeof(Grid))]
    public sealed class VerticalTilemapWorld : MonoBehaviour
    {
        [SerializeField] private int horizontalCellCount = 9;

        private Grid grid;

        public Vector2 CellSize => grid.cellSize;
        public int HorizontalCellCount => horizontalCellCount;

        private void Awake()
        {
            TryGetComponent<Grid>(out grid);
        }

        public Vector3 SnapToCell(Vector3 position)
        {
            Vector2Int cell = WorldToCell(position);
            Vector3 snappedPosition = CellToWorld(cell);
            position.x = snappedPosition.x;
            position.y = snappedPosition.y;
            return position;
        }

        public Vector2Int WorldToCell(Vector3 position)
        {
            Vector3 origin = transform.position;
            int column = Mathf.RoundToInt((position.x - origin.x) / grid.cellSize.x);
            int row = Mathf.RoundToInt((position.y - origin.y) / grid.cellSize.y);
            return new Vector2Int(column, row);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 origin = transform.position;
            return new Vector3(
                origin.x + cell.x * grid.cellSize.x,
                origin.y + cell.y * grid.cellSize.y,
                origin.z);
        }

        public bool IsPlayableColumn(int column)
        {
            return Mathf.Abs(column) <= horizontalCellCount / 2;
        }

        public float ClampHorizontalCell(float position)
        {
            float origin = transform.position.x;
            float halfCellRange = (horizontalCellCount - 1) * grid.cellSize.x * 0.5f;
            float snappedPosition = origin + Mathf.Round((position - origin) / grid.cellSize.x) * grid.cellSize.x;
            return Mathf.Clamp(snappedPosition, origin - halfCellRange, origin + halfCellRange);
        }
    }
}
