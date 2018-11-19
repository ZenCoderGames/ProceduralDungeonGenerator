using UnityEngine;

public class TileConnector : MonoBehaviour {
	public enum DIRECTION { TOP, BOT, LEFT, RIGHT }
	public DIRECTION direction;

	public Tile ownerTile;
	public bool IsConnected { get { return _connectedTile!=null; } }
	TileConnector _connectedTile;
	public bool IsInvalid { get { return _isInvalid; } }
	bool _isInvalid;

	void OnDrawGizmos() {
		Gizmos.color = Color.green;

		if(IsConnected)
			Gizmos.color = Color.yellow;
		else if(_isInvalid)
			Gizmos.color = Color.red;

		Gizmos.DrawSphere(transform.position, 0.25f);

		Vector3 dirn = Vector3.zero;
		switch(direction) {
			case DIRECTION.TOP: dirn = Vector3.forward; break;
			case DIRECTION.BOT: dirn = Vector3.back; break;
			case DIRECTION.LEFT: dirn = Vector3.left; break;
			case DIRECTION.RIGHT: dirn = Vector3.right; break;
		}

		Gizmos.DrawRay(transform.position, dirn);

		Vector3 right = Quaternion.LookRotation(dirn) * Quaternion.Euler(0,180+45,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(dirn) * Quaternion.Euler(0,180-45,0) * new Vector3(0,0,1);
		Gizmos.DrawRay(transform.position + dirn, right * 0.5f);
		Gizmos.DrawRay(transform.position + dirn, left * 0.5f);
	}

	public void UpdateBasedOnGrd(CellGrid cellGrid) {
		CellGrid.Cell myCell = ownerTile.Cell;
		int r = myCell.row;
		int c = myCell.col;
		if(direction == DIRECTION.TOP) {
			ConnectIfPossible(cellGrid.GetCell(r+1,c));
		}
		else if(direction == DIRECTION.BOT) {
			ConnectIfPossible(cellGrid.GetCell(r-1,c));
		}
		else if(direction == DIRECTION.LEFT) {
			ConnectIfPossible(cellGrid.GetCell(r,c-1));
		}
		else if(direction == DIRECTION.RIGHT) {
			ConnectIfPossible(cellGrid.GetCell(r,c+1));
		}
	}

	void ConnectIfPossible(CellGrid.Cell nextCell) {
		if(nextCell.IsOccupied()) {
			TileConnector nextCellConnector = nextCell.tile.GetOppositeConnector(direction);
			if(nextCellConnector!=null) {
				Connect(this, nextCellConnector);
			}
			else {
				_isInvalid = true;
			}
		}
	}

	public void SetToInvalid() {
		_isInvalid = true;
	}

	public bool IsFree() {
		return !_isInvalid && !IsConnected;
	}

	public static void Connect(TileConnector tc1, TileConnector tc2) {
		tc1.Connect(tc2);
		tc2.Connect(tc1);
	}

	public void Connect(TileConnector tc) {
		_connectedTile = tc;
	}

	public static TileConnector.DIRECTION GetOppositeDirection(TileConnector.DIRECTION dirn) {
		if(dirn == TileConnector.DIRECTION.TOP) {
			return TileConnector.DIRECTION.BOT;
		}
		else if(dirn == TileConnector.DIRECTION.BOT) {
			return TileConnector.DIRECTION.TOP;
		}
		else if(dirn == TileConnector.DIRECTION.LEFT) {
			return TileConnector.DIRECTION.RIGHT;
		}
		else if(dirn == TileConnector.DIRECTION.RIGHT) {
			return TileConnector.DIRECTION.LEFT;
		}

		return TileConnector.DIRECTION.TOP;
	}
}
