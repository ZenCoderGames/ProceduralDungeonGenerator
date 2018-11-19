using UnityEngine;

public class CellGrid {
	public int TotalRows { get { return _rows; } }
	public int TotalCols { get { return _cols; } }
	int _rows, _cols;

	[System.Serializable]
	public class Cell {
		public int row, col;
		public float size;

		public Vector3 Position { get { return _position; } }
		Vector3 _position;

		public Tile tile;

		public Cell(int r, int c, float s, Vector3 startPos) {
			row = r;
			col = c;
			size = s;
			_position = startPos + Vector3.right * col * size + Vector3.forward * row * size;
			tile = null;
		}

		public bool IsOccupied() {
			return tile!=null;
		}

		public void DebugDraw(Color cellColor) {
			Gizmos.color = cellColor;
			Gizmos.DrawWireCube(_position, Vector3.right * size + Vector3.forward * size + Vector3.up * 0.1f);
		}
	}

	Cell[,] _grid;

	public CellGrid (Vector3 startPos, int rows, int cols, float cellSize) {
		_rows = rows;
		_cols = cols;

		_grid = new Cell[_rows,_cols];
		for(int r=0; r<_rows; ++r) {
			for(int c=0; c<_rows; ++c) {
				_grid[r,c] = new Cell(r,c,cellSize,startPos);
			}
		}
	}

	public Cell GetCell(int r, int c) {
		return _grid[r,c];
	}

	public void DebugDraw() {
		for(int r=0; r<_rows; ++r) {
			for(int c=0; c<_rows; ++c) {
				_grid[r,c].DebugDraw(Color.yellow);
			}
		}
	}
}