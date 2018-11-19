using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	public TileConnector[] connections;

	List<TileConnector> _listOfFreeConnections;

	public CellGrid.Cell Cell { get { return _cell; } }
	CellGrid.Cell _cell;

	public int distanceFromStart;

	public void Init(CellGrid.Cell cell, CellGrid cellGrid) {
		_listOfFreeConnections = new List<TileConnector>();
		for(int i=0; i<connections.Length; ++i) {
			connections[i].ownerTile = this;

			if(connections[i].IsConnected) {
				continue;
			}

			if(connections[i].direction == TileConnector.DIRECTION.TOP) {
				if(cell.row == cellGrid.TotalRows-1 || cellGrid.GetCell(cell.row+1, cell.col).IsOccupied()) {
					connections[i].SetToInvalid();
				}
			}
			else if(connections[i].direction == TileConnector.DIRECTION.BOT) {
				if(cell.row == 0 || cellGrid.GetCell(cell.row-1, cell.col).IsOccupied()) {
					connections[i].SetToInvalid();
				}
			}
			else if(connections[i].direction == TileConnector.DIRECTION.LEFT) {
				if(cell.col == 0 || cellGrid.GetCell(cell.row, cell.col-1).IsOccupied()) {
					connections[i].SetToInvalid();
				}
			}
			else if(connections[i].direction == TileConnector.DIRECTION.RIGHT) {
				if(cell.col == cellGrid.TotalCols-1 || cellGrid.GetCell(cell.row, cell.col+1).IsOccupied()) {
					connections[i].SetToInvalid();
				}
			}

			if(!connections[i].IsInvalid) {
				_listOfFreeConnections.Add(connections[i]);
			}
		}

		cell.tile = this;
		_cell = cell;
	}

	public void RefreshFreeConnections() {
		for(int i=0; i<_listOfFreeConnections.Count; ++i) {
			if(_listOfFreeConnections[i].IsInvalid || _listOfFreeConnections[i].IsConnected) {
				_listOfFreeConnections.RemoveAt(i);
				i--;
			}
		}
	}

	public List<TileConnector> GetFreeConnections() {
		ZUtils.RandomizeList<TileConnector>(ref _listOfFreeConnections);
		return _listOfFreeConnections;
	}

	public TileConnector GetOppositeConnector(TileConnector.DIRECTION dirn) {
		for(int i=0; i<connections.Length; ++i) {
			if(connections[i].direction == TileConnector.GetOppositeDirection(dirn)) {
				return connections[i];
			}
		}

		return null;
	}

	public void Rotate() {
		int rotAngle = 90;
		transform.Rotate(Vector3.up * rotAngle);
		for(int i=0; i<connections.Length; ++i) {
			if(connections[i].direction == TileConnector.DIRECTION.TOP) {
				connections[i].direction = TileConnector.DIRECTION.RIGHT;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.BOT) {
				connections[i].direction = TileConnector.DIRECTION.LEFT;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.LEFT) {
				connections[i].direction = TileConnector.DIRECTION.TOP;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.RIGHT) {
				connections[i].direction = TileConnector.DIRECTION.BOT;
			}

		}
	}

	public bool HasAdjacentConnections() {
		bool hasTop = false;
		bool hasBot = false;
		bool hasRight = false;
		bool hasLeft = false;
		for(int i=0; i<connections.Length; ++i) {
			if(connections[i].direction == TileConnector.DIRECTION.TOP) {
				hasTop = true;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.BOT) {
				hasBot = true;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.RIGHT) {
				hasRight = true;
			}
			else if(connections[i].direction == TileConnector.DIRECTION.LEFT) {
				hasLeft = true;
			}

		}

		if(hasTop && (hasRight||hasLeft))
			return true;
		else if(hasBot && (hasRight||hasLeft))
			return true;
		else if(hasRight && (hasTop||hasBot))
			return true;
		else if(hasLeft && (hasTop||hasBot))
			return true;

		return false;
	}

	public void SetDistanceFromStart(Tile parentTile) {
		if(parentTile==null) {
			distanceFromStart = 0;
		}
		else {
			distanceFromStart = parentTile.distanceFromStart+1;
		}
	}
}
