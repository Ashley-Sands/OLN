using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class WorldManager : MonoBehaviour
{

	[SerializeField]
	private Player main_player;

	[SerializeField]
	private SceneData[] sceneData;
	private List<SceneInstance> sceneInstances;

	private List<int[]> activeWorld;    // 3*3 (id 1, 1 is the current active tile)

	public int ActiveSceneId {
		get { return activeWorld[ 1 ][ 1 ]; }
	}
	public int ActiveSceneDataId {
		get { return sceneInstances[ activeWorld[ 1 ][ 1 ] ].sceneDataId; }
	}

	public List<SceneData> scenes = new List<SceneData>();

	[SerializeField]
	private float loadFromEdgeDistance = 20;
	[SerializeField]
	private float unloadFromEdgeDistance = 40;

	private bool loadingScene = false;
	private bool autoMigrate = false;

	private void Awake()
	{

		sceneInstances = new List<SceneInstance>();
		activeWorld = new List<int[]>( 3 );

		for ( int i = 0; i < 3; i++ )
			activeWorld.Add( new int[ 3 ] );

		ClearWorld();

		LoadSceneAsync( 0, new EdgeVector(1, 1) );
		autoMigrate = true;
		
	}

	private void Update()
	{

		EdgeVector migrateTo = GetMigrateScene();
		EdgeVector loadTo = GetWorldSlotToLoadTo();

		bool canMigrate = migrateTo.x != 1 || migrateTo.z != 1;
		bool canLoad = loadTo.x != 1 || loadTo.z != 1;

//		print( migrateTo.x + " :: " + migrateTo.z );
		print( "Can Load: " + canLoad + " Can Migrate: " + canMigrate );
		DEBUG_PRINT_WORLD();

		if ( canMigrate )
		{
			MigrateScene( migrateTo );

		}
		else if ( canLoad )
		{

			LoadSceneAsync( GetRandomSceneId(), loadTo );

			// if its a conrer pice load the two ajcent tiles
			if ( loadTo.x != 1 && loadTo.z != 1 )
			{
				//TODO: FFS this only covers one corner
				LoadSceneAsync( GetRandomSceneId(), new EdgeVector( loadTo.x, loadTo.x + ( loadTo.x != 0 ? -1 : 1 ) ) );
				LoadSceneAsync( GetRandomSceneId(), new EdgeVector( loadTo.z + ( loadTo.z != 0 ? -1 : 1 ), loadTo.z ) );

			}

		}

		//UnloadTile();

	}

	private void LoadSceneAsync( int sceneId, EdgeVector edge )
	{


		Vector3 loadToPosition = GetWorldPositionForEdge( edge );

		StartCoroutine( LoadScene( sceneId, edge, loadToPosition ) );

	}

	private IEnumerator LoadScene( int sceneId, EdgeVector edge, Vector3 worldPosition )
	{

		while ( loadingScene )
			yield return null;

		if ( activeWorld[ edge.x ][ edge.z ] >= 0 )
		{
			Debug.Log( "Unable to load scene, world slot already contains a scene :( " );
			yield break;
		}

		loadingScene = true;

		AsyncOperation scene = SceneManager.LoadSceneAsync( sceneData[ sceneId ].name, LoadSceneMode.Additive );

		while ( !scene.isDone )
			yield return null;

		int sceneIndex = SceneManager.sceneCount - 1;

		SceneInstance sceneInst = new SceneInstance();

		sceneInst.sceneDataId = sceneId;
		sceneInst.scene = SceneManager.GetSceneAt( sceneIndex );// SceneManager.GetSceneByName( sceneData[ sceneId ].name );
		sceneInst.SetAreaPosition( worldPosition );

		sceneInstances.Add( sceneInst );

		activeWorld[ edge.x ][ edge.z ] = sceneInstances.Count - 1; //sceneId;
		

		if( autoMigrate )
		{
			MigrateScene( edge );
			autoMigrate = false;
		}

		loadingScene = false;

	}

	private void UnloadScene( Scene sceneToUnload )
	{
		SceneManager.UnloadSceneAsync( sceneToUnload );
	}

	private int GetRandomSceneId()
	{

		
		int sceneId = 0;

		do
		{
			sceneId = Random.Range(0, sceneData.Length);
		}
		while ( sceneId == activeWorld[1][1] );

		return sceneId;

	}

	private EdgeVector GetWorldSlotToLoadTo()
	{

		if ( ActiveSceneId == -1 ) return new EdgeVector( 1, 1 );

		Vector3 playerPos = main_player.transform.localPosition;
		EdgeVector loadToSlot = new EdgeVector();

		loadToSlot.x = GetWorldSlotToLoadToAlongAxis( playerPos.x, sceneData[ ActiveSceneDataId ].areaSize.width );
		loadToSlot.z = GetWorldSlotToLoadToAlongAxis( playerPos.z, sceneData[ ActiveSceneDataId ].areaSize.length );

		return loadToSlot;

	}

	private int GetWorldSlotToLoadToAlongAxis(float posAlongAxis, float axisLength)
	{

		if ( posAlongAxis < loadFromEdgeDistance ) return 0;
		else if ( posAlongAxis > axisLength - loadFromEdgeDistance ) return 2;
		else return 1;

	}

	private void UnloadTile()
	{

		Vector3 playerPos = main_player.transform.localPosition;
		EdgeVector unloadId = new EdgeVector();

		for ( int x = 0; x < 3; x++ )
		{
			for ( int z = 0; z < 3; z++ )
			{

				if ( activeWorld[ x ][ z ] == -1 ) continue;

				int sceneDataId = sceneInstances[ activeWorld[ x ][ z ] ].sceneDataId;

				unloadId.x = GetWorldSlotToUnloadAlongAxis( playerPos.x, sceneData[ sceneDataId ].areaSize.width );
				unloadId.z = GetWorldSlotToUnloadAlongAxis( playerPos.z, sceneData[ sceneDataId ].areaSize.length );

				bool canUnload = unloadId.x != 1 || unloadId.z != 1;

				if ( canUnload )
				{
					ClearWorldSlot( unloadId.x, unloadId.z, true );
					print( "UNLOAD: "+ unloadId.x +" , "+unloadId.z );
				}

			}
		}

	}

	private int GetWorldSlotToUnloadAlongAxis( float posAlongAxis, float axisLength )
	{

		if ( posAlongAxis > unloadFromEdgeDistance ) return 0;
		else if ( posAlongAxis < axisLength - unloadFromEdgeDistance ) return 2;
		else return 1;

	}

	private Vector3 GetWorldPositionForEdge( EdgeVector edge )
	{

		if ( ActiveSceneId == -1 ) return Vector3.zero;

		Vector3 wPos = Vector3.zero;

		wPos.x = GetPositionAlongAxis( edge.x, sceneInstances[ ActiveSceneId ].areaPosition.width, sceneData[ ActiveSceneDataId ].areaSize.width );
		wPos.z = GetPositionAlongAxis( edge.z, sceneInstances[ ActiveSceneId ].areaPosition.length, sceneData[ ActiveSceneDataId ].areaSize.length );



		return wPos;

	}

	private float GetPositionAlongAxis( int axisEdge, float axisPosition, float axisLength )
	{

		return axisPosition + ( axisLength * -(1-axisEdge) );	//stupid - it has invert it all. haha

	}

	private EdgeVector GetMigrateScene()
	{

	//	if ( ActiveSceneId == -1 ) return new EdgeVector(1, 1);

		EdgeVector sceneToMigrateTo = new EdgeVector();

		Vector3 playerPostion = main_player.transform.localPosition;

		sceneToMigrateTo.x = 1 + GetEdgeOutSideOfBoundsAlongAxis( playerPostion.x, sceneData[ ActiveSceneDataId ].areaSize.width );
		sceneToMigrateTo.z = 1 + GetEdgeOutSideOfBoundsAlongAxis( playerPostion.z, sceneData[ ActiveSceneDataId ].areaSize.length );


		return sceneToMigrateTo;

	}

	private int GetEdgeOutSideOfBoundsAlongAxis( float axisPos, float axisLen )
	{

		if ( axisPos <= 0 )
			return -1; 
		else if ( axisPos >= axisLen )
			return 1;
		else
			return 0;

	}

	private void MigrateScene( EdgeVector sceneToMigrateTo )
	{
		int sceneId = activeWorld[ sceneToMigrateTo.x ][sceneToMigrateTo.z] ;

		Debug.LogWarning( "(SceneDataID: "+ sceneId +" :: "+ sceneToMigrateTo.x + " :: "+ sceneToMigrateTo.z + " )");

		if ( sceneId < 0 || !sceneInstances[ sceneId ].SceneIsAvailable()  )
		{
			Debug.LogError( "Scene has not been loaded or is invalid :( " );
			return;
		}

		SceneManager.SetActiveScene( sceneInstances[ sceneId ].scene );

		ShiftScenes( sceneToMigrateTo );

		main_player.transform.parent = null;

		SceneManager.MoveGameObjectToScene( gameObject, sceneInstances[ sceneId ].scene );
		SceneManager.MoveGameObjectToScene( main_player.gameObject, sceneInstances[ sceneId ].scene );

		main_player.transform.parent = sceneInstances[ sceneId ].scene.GetRootGameObjects()[ 0 ].transform;

	}

	private void ShiftScenes( EdgeVector newActiveSceneId )
	{

		if ( newActiveSceneId.x != 1 )
			ShiftSceneX( !(newActiveSceneId.x == 0) );

		if ( newActiveSceneId.z != 1 )
			ShiftSceneZ( newActiveSceneId.z != 0 );

	}

	private void ShiftSceneX( bool up )
	{

		int row = up ? 0 : 2;
		int clearRowId = up ? 0 : 2;

		// clear the slots that we are going to lose.
		ClearWorldSlot( clearRowId, 0, true );
		ClearWorldSlot( clearRowId, 1, true );
		ClearWorldSlot( clearRowId, 2, true );

		for ( int x = 0; x < 2; x++ )
		{
			for( int z = 0; z < 3; z++ )
			{
				activeWorld[ row ][ z ] = activeWorld[ row + ( up ? 1 : -1) ][ z ];
			}

			if ( up ) row++;
			else	  row--;

		}

		// clear the slots that have just been moved.
		ClearWorldSlot( 2 - clearRowId, 0 );
		ClearWorldSlot( 2 - clearRowId, 1 );
		ClearWorldSlot( 2 - clearRowId, 2 );

	}

	private void ShiftSceneZ( bool right )
	{
		// this needes inverting :| TODO: COME BACK TOO:
		int col = right ? 0 : 2;
		int colToClear = right ? 0 : 2;
		print( "RIGHT: "+right + " :: " + colToClear );
		
		// clear the slots that we are going to lose once shiffed
		ClearWorldSlot( 0, colToClear, true );
		ClearWorldSlot( 1, colToClear, true );
		ClearWorldSlot( 2, colToClear, true );
		
		for ( int z = 0; z < 2; z++ )
		{ 
			for ( int x = 0; x < 3; x++ )
			{

				activeWorld[ x ][ col ] = activeWorld[ x ][ col + ( right ? 1 : -1 ) ];

			}

			if ( right ) col++;
			else col--;
		}

		// clear the slots that have just been moved :)
		ClearWorldSlot( 0, 2 - colToClear );
		ClearWorldSlot( 1, 2 - colToClear );
		ClearWorldSlot( 2, 2 - colToClear );

	}

	private void ClearWorld()
	{
		
		for ( int x = 0; x < 3; x++ )
			for ( int z = 0; z < 3; z++ )
				ClearWorldSlot( x, z );

	}

	private void ClearWorldSlot( int x, int z, bool unload = false )
	{
		int sceneIdToUnload = activeWorld[ x ][ z ];

		activeWorld[ x ][ z ] = -1;


		//	if( sceneIdToUnload > -1 && sceneData[sceneIdToUnload].SceneIsAvailable() )
		if ( sceneIdToUnload > -1 && unload )
		{
			print( "x: " + x + ", z: " + z );

			UnloadScene( sceneInstances[ sceneIdToUnload ].scene );

			// removed the scene instance and correct the active world id's
			/*
			sceneInstances.RemoveAt( sceneIdToUnload );

			for ( int x_ = 0; x_ < 3; x_++ )
				for ( int z_ = 0; z_ < 3; z_++ )
					if ( activeWorld[ x_ ][ z_ ] >= sceneIdToUnload )
						activeWorld[ x_ ][ z_ ] -= 1;
			*/
		}

	}




	private void DEBUG_PRINT_WORLD()
	{
		string worldStr = "";

		for ( int x = 0; x < 3; x++ )
		{
			for ( int z = 0; z < 3; z++ )
			{
				worldStr += activeWorld[ x ][ z ] + " | ";
			}
			worldStr += "\n";
		}

		print(worldStr);

	}

}

[System.Serializable]
public struct EdgeVector
{
	public int x, z;

	public EdgeVector( int x_, int z_)
	{
		x = x_;
		z = z_;
	}

}

[System.Serializable]
public struct TerainVector
{
	public int width, length;

	public TerainVector( int w, int l )
	{
		width = w;
		length = l;
	}

	public TerainVector( Vector3 size )
	{
		width = (int)size.x;
		length = (int)size.z;
	}

	public static TerainVector operator +( TerainVector lhs, TerainVector rhs )
	{
		lhs.width += rhs.width;
		lhs.length += rhs.length;

		return lhs;
	}

	public static TerainVector operator -( TerainVector lhs, TerainVector rhs )
	{
		lhs.width -= rhs.width;
		lhs.length -= rhs.length;

		return lhs;
	}

	public static bool operator ==( TerainVector lhs, TerainVector rhs )
	{
		lhs.width += rhs.width;
		lhs.length += rhs.length;

		return lhs.width == rhs.width && lhs.length == rhs.length;
	}

	public static bool operator !=( TerainVector lhs, TerainVector rhs )
	{
		lhs.width += rhs.width;
		lhs.length += rhs.length;

		return lhs.width != rhs.width || lhs.length != rhs.length;
	}

	public override bool Equals( object obj )
	{
		return base.Equals( obj );
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

}

[System.Serializable]
public struct SceneData
{

	public string name;

	public TerainVector areaSize;

}

[System.Serializable]
public struct SceneInstance
{

	public int sceneDataId;
	public TerainVector areaPosition;
	public Scene scene; // null if not loaded.

	//Sets the area position and moves the root object to that position in world space.
	public void SetAreaPosition( Vector3 position )
	{

		if ( !SceneIsAvailable() )
		{
			Debug.LogError( "Scene not loaded. Unable to update it position." );
			return;
		}

		areaPosition.width = (int)position.x;
		areaPosition.length = (int)position.z;

		scene.GetRootGameObjects()[ 0 ].transform.position = position;

	}

	public bool SceneIsAvailable()
	{
		return scene != null && scene.IsValid() && scene.isLoaded;
	}

}