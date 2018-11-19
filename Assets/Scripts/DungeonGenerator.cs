using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DungeonGenerator : MonoBehaviour {
	[Range(3, 100)]
	public int numTiles;
	public Tile[] tileSet;

	public int gridRows, gridCols;
	public float gridSize;
	public int startRow, startCol;

	public bool autoGeneration, autoGenerationFillGaps, autoGenerationFillEndTiles, autoGenerationFillDeadEnds;

	private Random.State _prevRandomState;

	CellGrid _cellGrid;
	List<GameObject> _cellObjectList;

	// Base TileSet
	List<Tile> _baseTileSet;
	Transform _baseTileSetT, _dungeonT;
	int _totalTilesInBaseTileSet;

	List<TileConnector> _listOfFreeConnections;
	int _numTilesCreated;
	Tile _startTile, _endTile;
	List<Tile> _dungeonTiles;
	bool _isGenerationCompleted;

	enum TILE_TYPE { START, CONNECTION, CONNECTION_FILLER, DEAD_END, END }

	void Update() {
		if(Input.GetKeyUp(KeyCode.G)) {
			GenerateAnimated();
		}
	}

	void GenerateAnimated() {
		_isGenerationCompleted = false;
		Generate();
		for(int i=0; i<_dungeonTiles.Count; ++i) {
			_dungeonTiles[i].gameObject.SetActive(false);
		}
		StopAllCoroutines();
		StartCoroutine(AnimateGenerated());
	}

	IEnumerator AnimateGenerated() {
		int i=0;
		while(i<_dungeonTiles.Count) {
			_dungeonTiles[i].gameObject.SetActive(true);
			Transform tileT = _dungeonTiles[i].transform;
			Vector3 localScale = Vector3.one * 0.1f;
			float animateVal = 0.1f;
			while(animateVal<1.0f) {
				tileT.localScale = Vector3.one * animateVal;
				animateVal += 0.25f;
				yield return new WaitForSeconds(0.0001f);
			}
			tileT.localScale = Vector3.one;
			yield return new WaitForSeconds(0.005f);
			i++;
		}
		_isGenerationCompleted = true;
	}

	[ContextMenu("GenerateFromSeed")]
	public void GenerateFromSeed () {
		Random.state = _prevRandomState;
		Generate();
	}

	[ContextMenu("Generate")]
	public void Generate () {
		_prevRandomState = Random.state;

		_cellGrid = new CellGrid(transform.position, gridRows, gridCols, gridSize);

		Reset();

		// Start Tile
		CreateTile(_cellGrid.GetCell(startRow, startCol), TILE_TYPE.START);

		// Connection Tiles
		if(autoGeneration) {
			while(GenerateNextTile()) {
				// Keep generating as long as the conditions are valid
			}
		}

		// Fill Connection Gaps
		if(autoGenerationFillGaps) {
			while(CloseFreeConnection()) {
				// Close all free connections if they still exist
			}
		}

		// End Tiles
		if(autoGenerationFillEndTiles) {
			// Sort by furthest distance from start tile
			_listOfFreeConnections.Sort(DistanceFromStartTile);
			GenerateEndTile();
		}

		// Dead Ends
		if(autoGenerationFillDeadEnds) {
			while(CloseDeadEnds()) {
				// Close all free connections if they still exist
			}
		}

		// Assign distance from nodes

	}

	[ContextMenu("GenerateNextTile")]
	bool GenerateNextTile() {
		if(_numTilesCreated<numTiles && _listOfFreeConnections.Count>0) {
			// Start with first free connection
			TileConnector newConnector = _listOfFreeConnections[0];
			_listOfFreeConnections.RemoveAt(0);
			// Find a tile for this connection
			CreateTile(GetNextCell(newConnector), TILE_TYPE.CONNECTION, newConnector);
			return true;
		}

		RefreshFreeConnections();
		return false;
	}

	[ContextMenu("CloseFreeConnections")]
	bool CloseFreeConnection() {
		if(_listOfFreeConnections.Count>0) {
			// Start with first free connection
			TileConnector newConnector = _listOfFreeConnections[0];
			_listOfFreeConnections.RemoveAt(0);
			// Find a tile for this connection
			CreateTile(GetNextCell(newConnector), TILE_TYPE.CONNECTION_FILLER, newConnector);
			return true;
		}

		RefreshFreeConnections();
		return false;
	}

	[ContextMenu("CreateEndTiles")]
	void GenerateEndTile() {
		if(_listOfFreeConnections.Count>0) {
			// Start with first free connection
			TileConnector newConnector = _listOfFreeConnections[0];
			_listOfFreeConnections.RemoveAt(0);
			// Find a tile for this connection
			CreateTile(GetNextCell(newConnector), TILE_TYPE.END, newConnector);

			RefreshFreeConnections();
		}
		else {
			Debug.Log("Error: No free connections for end tile");
		}
	}

	[ContextMenu("CloseDeadEnds")]
	bool CloseDeadEnds() {
		if(_listOfFreeConnections.Count>0) {
			// Start with first free connection
			TileConnector newConnector = _listOfFreeConnections[0];
			_listOfFreeConnections.RemoveAt(0);
			// Find a tile for this connection
			CreateTile(GetNextCell(newConnector), TILE_TYPE.DEAD_END, newConnector);
			return true;
		}

		RefreshFreeConnections();

		if(_listOfFreeConnections.Count>0) {
			Debug.Log("Error: Unable to fill " + _listOfFreeConnections.Count.ToString() + " connections");
		}

		return false;
	}

	void CreateTile(CellGrid.Cell cell, TILE_TYPE tileType, TileConnector parentConnector=null) {
		ZUtils.RandomizeList<Tile>(ref _baseTileSet);

		Tile newTile = null;
		if(tileType == TILE_TYPE.START) {
			newTile = FindStartTile();
		}
		else if(tileType == TILE_TYPE.CONNECTION) {
			newTile = FindConnectionTile(cell);
		}
		else if(tileType == TILE_TYPE.CONNECTION_FILLER) {
			newTile = FindConnectionTileToFill(cell);
		}
		else if(tileType == TILE_TYPE.END) {
			newTile = FindEndTile(parentConnector);
			_endTile = _endTile;
		}
		else if(tileType == TILE_TYPE.DEAD_END) {
			newTile = FindDeadEndTile(parentConnector);
		}

		if(newTile==null) {
			// No valid option right now, move to next step
			return;
		}

		GameObject newTileGO = Instantiate(newTile.gameObject);
		Transform newTileT = newTileGO.transform;
		newTileT.SetParent(_dungeonT);
		newTileT.position = cell.Position;

		newTile = newTileGO.GetComponent<Tile>();

		if(parentConnector!=null) {
			TileConnector childConnector = newTile.GetOppositeConnector(parentConnector.direction);
			if(childConnector!=null) {
				TileConnector.Connect(childConnector, parentConnector);
			}
			else {
				Debug.Log("Error: Unable to find a connector so skipping");
				return;
			}
		}

		newTile.Init(cell, _cellGrid);

		if(AllowNewConnections(tileType)) {
			// Add free connections
			List<TileConnector> freeConnectionList = newTile.GetFreeConnections();
			for(int i=0; i<freeConnectionList.Count; ++i) {
				_listOfFreeConnections.Add(freeConnectionList[i]);
			}
		}

		// Reiterate free connection list for new blockers
		for(int i=0; i<_listOfFreeConnections.Count; ++i) {
			_listOfFreeConnections[i].UpdateBasedOnGrd(_cellGrid);
			if(!_listOfFreeConnections[i].IsFree()) {
				_listOfFreeConnections[i].SetToInvalid();
				_listOfFreeConnections.RemoveAt(i);
				--i;
			}
		}

		_dungeonTiles.Add(newTile);
		_cellObjectList.Add(newTileGO);
		_numTilesCreated++;

		if(tileType == TILE_TYPE.START) {
			_startTile = newTile;
		}
		else if(tileType == TILE_TYPE.END) {
			_endTile = newTile;
		}

		//Debug.Log("GenNextTile: "  + _numCellsCreated.ToString() + " " + _listOfFreeConnections.Count.ToString());
	}

	Tile FindStartTile() {
		for(int i=0; i<_totalTilesInBaseTileSet; ++i) {
			if(_baseTileSet[i].connections.Length==1) {
				return _baseTileSet[i];
			}
		}

		Debug.Log("Unable to find valid start tile");
		return null;
	}

	Tile FindConnectionTile(CellGrid.Cell cell) {
		// Find total neighbors on this cell
		int r = cell.row;
		int c = cell.col;
		// Top
		bool isTopNeeded = false;
		bool isTopBlocked = false;
		if(r+1 < gridRows-1) {
			CellGrid.Cell topCell = _cellGrid.GetCell(r+1, c);
			if(topCell.IsOccupied()) {
				if(topCell.tile.GetOppositeConnector(TileConnector.DIRECTION.TOP)!=null) {
					isTopNeeded = true;
				}
				else {
					isTopBlocked = true;
				}
			}
		}
		else {
			isTopBlocked = true;
		}
		// Bot
		bool isBotNeeded = false;
		bool isBotBlocked = false;
		if(r > 0) {
			CellGrid.Cell botCell = _cellGrid.GetCell(r-1, c);
			if(botCell.IsOccupied()) {
				if(botCell.tile.GetOppositeConnector(TileConnector.DIRECTION.BOT)!=null) {
					isBotNeeded = true;
				}
				else {
					isBotBlocked = true;
				}
			}
		}
		else {
			isBotBlocked = true;
		}
		// Right
		bool isRightNeeded = false;
		bool isRightBlocked = false;
		if(c+1 < gridCols-1) {
			CellGrid.Cell rightCell = _cellGrid.GetCell(r, c+1);
			if(rightCell.IsOccupied()) {
				if(rightCell.tile.GetOppositeConnector(TileConnector.DIRECTION.RIGHT)!=null) {
					isRightNeeded = true;
				}
				else {
					isRightBlocked = true;
				}
			}
		}
		else {
			isRightBlocked = true;
		}
		// Left
		bool isLeftNeeded = false;
		bool isLeftBlocked = false;
		if(c > 0) {
			CellGrid.Cell leftCell = _cellGrid.GetCell(r, c-1);
			if(leftCell.IsOccupied()) {
				if(leftCell.tile.GetOppositeConnector(TileConnector.DIRECTION.LEFT)!=null) {
					isLeftNeeded = true;
				}
				else {
					isLeftBlocked = true;
				}
			}
		}
		else {
			isLeftBlocked = true;
		}

		for(int i=0; i<_totalTilesInBaseTileSet; ++i) {
			Tile baseTile = _baseTileSet[i];
			int numConnections = baseTile.connections.Length;

			if(numConnections==1)
				continue;

			bool isTopValid = !isTopNeeded;
			bool isBotValid = !isBotNeeded;
			bool isRightValid = !isRightNeeded;
			bool isLeftValid = !isLeftNeeded;
			for(int j=0; j<numConnections; ++j) {
				if(baseTile.connections[j].direction == TileConnector.DIRECTION.TOP) {
					if(isTopNeeded) 
						isTopValid = true;
					else if(isTopBlocked) 
						isTopValid = false;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.BOT) {
					if(isBotNeeded) 
						isBotValid = true;
					else if(isBotBlocked) 
						isBotValid = false;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.RIGHT) {
					if(isRightNeeded) 
						isRightValid = true;
					else if(isRightBlocked) 
						isRightValid = false;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.LEFT) {
					if(isLeftNeeded) 
						isLeftValid = true;
					else if(isLeftBlocked) 
						isLeftValid = false;
				}
			}

			if(isTopValid && isBotValid && isRightValid && isLeftValid) {
				return baseTile;
			}
		}

		Debug.Log("Error: Unable to find a connection tile so skipping");
		return null;
	}

	Tile FindConnectionTileToFill(CellGrid.Cell cell) {
		// Find total neighbors on this cell
		int r = cell.row;
		int c = cell.col;
		// Top
		bool isTopNeeded = false;
		if(r+1 < gridRows-1) {
			CellGrid.Cell topCell = _cellGrid.GetCell(r+1, c);
			if(topCell.IsOccupied()) {
				if(topCell.tile.GetOppositeConnector(TileConnector.DIRECTION.TOP)!=null) {
					isTopNeeded = true;
				}
			}
		}
		// Bot
		bool isBotNeeded = false;
		if(r > 0) {
			CellGrid.Cell botCell = _cellGrid.GetCell(r-1, c);
			if(botCell.IsOccupied()) {
				if(botCell.tile.GetOppositeConnector(TileConnector.DIRECTION.BOT)!=null) {
					isBotNeeded = true;
				}
			}
		}
		// Right
		bool isRightNeeded = false;
		if(c+1 < gridCols-1) {
			CellGrid.Cell rightCell = _cellGrid.GetCell(r, c+1);
			if(rightCell.IsOccupied()) {
				if(rightCell.tile.GetOppositeConnector(TileConnector.DIRECTION.RIGHT)!=null) {
					isRightNeeded = true;
				}
			}
		}
		// Left
		bool isLeftNeeded = false;
		if(c > 0) {
			CellGrid.Cell leftCell = _cellGrid.GetCell(r, c-1);
			if(leftCell.IsOccupied()) {
				if(leftCell.tile.GetOppositeConnector(TileConnector.DIRECTION.LEFT)!=null) {
					isLeftNeeded = true;
				}
			}
		}

		int totalNeeded = 0;
		if(isTopNeeded) totalNeeded++;
		if(isBotNeeded) totalNeeded++;
		if(isRightNeeded) totalNeeded++;
		if(isLeftNeeded) totalNeeded++;
		if(totalNeeded==1) {
			return null;
		}
		for(int i=0; i<_totalTilesInBaseTileSet; ++i) {
			Tile baseTile = _baseTileSet[i];
			int numConnections = baseTile.connections.Length;

			if(numConnections==1)
				continue;

			bool hasTop = false;
			bool hasBot = false;
			bool hasRight = false;
			bool hasLeft = false;
			for(int j=0; j<numConnections; ++j) {
				if(baseTile.connections[j].direction == TileConnector.DIRECTION.TOP) {
					hasTop = true;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.BOT) {
					hasBot = true;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.RIGHT) {
					hasRight = true;
				}
				else if(baseTile.connections[j].direction == TileConnector.DIRECTION.LEFT) {
					hasLeft = true;
				}
			}
				
			int totalValid = 0;
			if(isTopNeeded && hasTop) totalValid++;
			else if(!isTopNeeded && !hasTop) totalValid++;
			if(isBotNeeded && hasBot) totalValid++;
			else if(!isBotNeeded && !hasBot) totalValid++;
			if(isRightNeeded && hasRight) totalValid++;
			else if(!isRightNeeded && !hasRight) totalValid++; 
			if(isLeftNeeded && hasLeft) totalValid++;
			else if(!isLeftNeeded && !hasLeft) totalValid++;

			if(totalValid == 4) {
				return baseTile;
			}
		}

		Debug.Log("Error: Unable to find a connection tile to fill so skipping");
		return null;
	}

	Tile FindDeadEndTile(TileConnector parentConnector) {
		for(int i=0; i<_totalTilesInBaseTileSet; ++i) {
			Tile baseTile = _baseTileSet[i];
			int numConnections = baseTile.connections.Length;

			if(numConnections>1)
				continue;

			for(int j=0; j<numConnections; ++j) {
				if(baseTile.connections[j].direction == TileConnector.GetOppositeDirection(parentConnector.direction)) {
					return baseTile;
				}
			}
		}

		Debug.Log("Error: Unable to find a dead end tile to fill");
		return null;
	}

	Tile FindEndTile(TileConnector parentConnector) {
		for(int i=0; i<_totalTilesInBaseTileSet; ++i) {
			Tile baseTile = _baseTileSet[i];
			int numConnections = baseTile.connections.Length;

			if(numConnections>1)
				continue;

			for(int j=0; j<numConnections; ++j) {
				if(baseTile.connections[j].direction == TileConnector.GetOppositeDirection(parentConnector.direction)) {
					return baseTile;
				}
			}
		}

		Debug.Log("Error: Unable to find a end tile to fill");
		return null;
	}

	void Reset() {
		_dungeonTiles = new List<Tile>();
		_startTile = null;
		_endTile = null;
		_numTilesCreated = 0;

		_cellObjectList = new List<GameObject>();
		if(_dungeonT != null) {
			DestroyImmediate(_dungeonT.gameObject);
		}

		GameObject dungeonGO = new GameObject("Dungeon");
		_dungeonT = dungeonGO.transform;
		_dungeonT.SetParent(transform);
		_dungeonT.localPosition = Vector3.zero;

		// Create base tileset
		if(_baseTileSetT != null) {
			DestroyImmediate(_baseTileSetT.gameObject);
		}

		GameObject baseTileSetGO = new GameObject("BaseTileSet");
		_baseTileSetT = baseTileSetGO.transform;
		_baseTileSetT.SetParent(transform);
		_baseTileSetT.localPosition = Vector3.zero;
		baseTileSetGO.SetActive(false);

		_baseTileSet = new List<Tile>();
		for(int i=0; i<tileSet.Length; ++i) {
			int numConnections = tileSet[i].connections.Length;
			CreateBaseTile(tileSet[i]);
			if(numConnections<4) {
				// Create all the rotational variants
				Tile baseRotTile = CreateBaseTileWithRotation(tileSet[i], "_R1");
				if(numConnections==1 || numConnections==3 || 
					(numConnections==2 && baseRotTile.HasAdjacentConnections())) {
					baseRotTile = CreateBaseTileWithRotation(baseRotTile, "1");
					baseRotTile = CreateBaseTileWithRotation(baseRotTile, "1");
				}
			}
		}
		_totalTilesInBaseTileSet = _baseTileSet.Count;

		_listOfFreeConnections = new List<TileConnector>();
	}

	#region BASE_SET
	Tile CreateBaseTile(Tile tile) {
		GameObject newTileGO = Instantiate(tile.gameObject);
		Transform newTileT = newTileGO.transform;
		newTileT.SetParent(_baseTileSetT);

		string tileName = newTileT.name;
		string removeStr = "(Clone)";
		int index = tileName.IndexOf(removeStr);
		newTileT.name = tileName.Remove(index, removeStr.Length);

		Tile newTile = newTileGO.GetComponent<Tile>();
		_baseTileSet.Add(newTile);
		return newTile;
	}

	Tile CreateBaseTileWithRotation(Tile tile, string namePostFix) {
		Tile newTile = CreateBaseTile(tile);
		newTile.Rotate();
		newTile.transform.name = string.Concat(newTile.transform.name, namePostFix);
		return newTile;
	}
	#endregion

	#region HELPERS
	CellGrid.Cell GetNextCell(TileConnector tileConnector) {
		Tile ownerTile = tileConnector.ownerTile;
		if(tileConnector.direction == TileConnector.DIRECTION.TOP) {
			return _cellGrid.GetCell(ownerTile.Cell.row+1, ownerTile.Cell.col);
		}
		else if(tileConnector.direction == TileConnector.DIRECTION.BOT) {
			return _cellGrid.GetCell(ownerTile.Cell.row-1, ownerTile.Cell.col);
		}
		else if(tileConnector.direction == TileConnector.DIRECTION.LEFT) {
			return _cellGrid.GetCell(ownerTile.Cell.row, ownerTile.Cell.col-1);
		}
		else if(tileConnector.direction == TileConnector.DIRECTION.RIGHT) {
			return _cellGrid.GetCell(ownerTile.Cell.row, ownerTile.Cell.col+1);
		}

		return null;
	}

	int DistanceFromStartTile(TileConnector t1, TileConnector t2) {
		float distT1 = ZUtils.GetDistanceBetweenVector(t1.transform.position, _startTile.transform.position, false, true, false);
		float distT2 = ZUtils.GetDistanceBetweenVector(t1.transform.position, _startTile.transform.position, false, true, false);
		return distT2.CompareTo(distT1);
	}

	void RefreshFreeConnections() {
		_listOfFreeConnections.Clear();
		for(int i=0; i<_dungeonTiles.Count; ++i) {
			_dungeonTiles[i].RefreshFreeConnections();
			_listOfFreeConnections.AddRange(_dungeonTiles[i].GetFreeConnections());
		}
	}

	bool AllowNewConnections(TILE_TYPE tileType) {
		return tileType == TILE_TYPE.START || tileType == TILE_TYPE.CONNECTION;
	}
	#endregion

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(transform.position, 0.5f);

		if(_cellGrid!=null) {
			_cellGrid.DebugDraw();
			if(_isGenerationCompleted) {
				if(_startTile!=null && _startTile.Cell!=null) {
					_startTile.Cell.DebugDraw(Color.green);
				}
				if(_endTile!=null && _endTile.Cell!=null) {
					_endTile.Cell.DebugDraw(Color.red);
				}
			}
		}
	}
}
