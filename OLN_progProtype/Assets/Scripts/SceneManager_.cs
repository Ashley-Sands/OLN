/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager_ : MonoBehaviour
{

	[SerializeField]
	private Player main_player;

	[SerializeField]
	private SceneData[] scenes;
	
	private int activeSceneData = 0;    // 0 should always be the start scene.
	//private int nextSceneData = -1;

	[SerializeField]
	private List<int[]> cuedScenes;

	private TerainVector activeScenePosition = new TerainVector();

	private TerainVector positionInScene = new TerainVector();

	[SerializeField]
	private int loadDistance = 20;  // the ammount of distance away from the edge the the next scene should be loaded.

	private bool sceneLoading = false;

	public void Awake()
	{
		// init the start scene.
		LoadSceneAsync( 0, new TerainVector(), true );

		cuedScenes = new List<int[]>( 3 );

		for ( int i = 0; i < 3; i++ )
			cuedScenes.Add( new int[3] );
		

	}

	public void Update()	// might be a good idear if it was not like this.
	{

		TerainVector edgeToLoadAgainst = new TerainVector();

		if( CanMigrate() )
		{
			MigrateScene(nextSceneData);
			nextSceneData = -1;
		}
		else if ( nextSceneData < 0 && CanLoadNextScene( ref edgeToLoadAgainst ) )		//TODO: nextSceneData needs to be somwhere else
		{

			int nextSceneId = 0;

			if ( scenes.Length > 1 )
				do
				{
					nextSceneId = Random.Range( 0, scenes.Length );
				} while ( nextSceneId == activeSceneData );

			
			LoadSceneAsync(nextSceneId, edgeToLoadAgainst);

		}
		
	}

	public void LoadSceneAsync( int sceneDataId, TerainVector edgeToLoadAgainst, bool activeSceneOnLoad = false)
	{

		Vector3 worldPosition = GetLoadPosition( edgeToLoadAgainst, sceneDataId );
		TerainVector cueSlot = GetSceneCueSlot( edgeToLoadAgainst );

		StartCoroutine( LoadScene( sceneDataId, cueSlot, worldPosition, activeSceneOnLoad ) );

	}

	IEnumerator LoadScene( int sceneDataId, TerainVector cueSlot, Vector3 worldPosition, bool activeOnLoad = false )
	{

		while( sceneLoading )
		{
			yield return null;
		}

		sceneLoading = true;

		if ( cuedScenes[ cueSlot.width ][ cueSlot.length ] < 0 ) yield break;

		AsyncOperation loadingScene = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync( scenes[ sceneDataId ].name, LoadSceneMode.Additive );

		while ( !loadingScene.isDone )
		{
			yield return null;
		}

		scenes[ sceneDataId ].scene = SceneManager.GetSceneByName( scenes[ sceneDataId ].name );
		scenes[ sceneDataId ].SetAreaPosition( worldPosition );         //scenes[ sceneDataId ].areaPosition = new TerainVector(worldPosition);
		nextSceneData = sceneDataId;

		cuedScenes[ cueSlot.width ][ cueSlot.length ] = sceneDataId;

		if ( activeOnLoad )
		{
			MigrateScene( sceneDataId );
			nextSceneData = -1;
		}

		sceneLoading = false;

	}
	
	public bool UnloadScene( int sceneDataId )
	{
		return false;
	}
	
	private Vector3 GetLoadPosition(TerainVector edgeToLoadAgainst, int sceneId)
	{

		Vector3 pos = Vector3.zero;
		TerainVector currentSceneSize = scenes[ activeSceneData ].areaSize;
		TerainVector loadSceneSize = scenes[ sceneId ].areaSize;

		pos.x = GetPositionAlongAxis( edgeToLoadAgainst.width, scenes[ activeSceneData ].areaPosition.width, scenes[ activeSceneData ].areaSize.width );
		pos.z = GetPositionAlongAxis( edgeToLoadAgainst.length, scenes[ activeSceneData ].areaPosition.length, scenes[ activeSceneData ].areaSize.length );

		return pos;

	}

	private float GetPositionAlongAxis(int edgeValue, int axis_pos, int axis_length)
	{

		float pos = 0;

		if ( edgeValue < 0 )
			pos = axis_pos - axis_length;
		else if ( edgeValue > 0 )
			pos = axis_pos;

		return pos;

	}

	private bool CanLoadNextScene( ref TerainVector edge )
	{

		TerainVector edgeToLoad = new TerainVector();
		Vector3 playerPosition = main_player.transform.localPosition;

		edgeToLoad.width =  GetEdgeToLoadAgaints( playerPosition.x, scenes[ activeSceneData ].areaSize.width  );
		edgeToLoad.length = GetEdgeToLoadAgaints( playerPosition.z, scenes[ activeSceneData ].areaSize.length );

		edge = edgeToLoad;

		return edgeToLoad != new TerainVector();

	}
	
	private int GetEdgeToLoadAgaints( float axisPos, float axisLength)
	{

		int edge = 0;

		if ( axisPos < loadDistance )
			edge = -1;
		else if ( axisPos > axisLength - loadDistance )
			edge = 1;
		

		return edge;

	}

	/// <summary>
	/// returns false if the slot is already ocupied.
	/// </summary>
	private TerainVector GetSceneCueSlot( TerainVector edge)
	{

		int slot_x = 1 + edge.width;
		int slot_y = 1 + edge.length;

		return new TerainVector( slot_x, slot_y );

	}

	private bool InActiveSceneBounds()
	{

		Vector3 playerPosition = main_player.transform.localPosition;
		
		print("X:"+ InBoundsAlongAxis( playerPosition.x, scenes[ activeSceneData ].areaSize.width ) );
		print("Z:"+ InBoundsAlongAxis( playerPosition.z, scenes[ activeSceneData ].areaSize.length ) );
	
	
		return InBoundsAlongAxis( playerPosition.x, scenes[ activeSceneData ].areaSize.width ) &&
			   InBoundsAlongAxis( playerPosition.z, scenes[ activeSceneData ].areaSize.length );

	}

	private bool InBoundsAlongAxis( float axisPosition, float axisLength )
	{

		return axisPosition > 0 && axisPosition < axisLength;

	}

	private bool CanMigrate()
	{
		
		return !InActiveSceneBounds() && nextSceneData > -1;
	}

	private void MigrateScene( int sceneDataId )
	{

		// move the player and manager into the scene that we are migrating into.
		if ( !scenes[ sceneDataId ].SceneIsAvailable() )
		{
			Debug.LogError("Scene has not been loaded or is invalid :(");
			return;
		}

		SceneManager.SetActiveScene(scenes[ sceneDataId ].scene );
		activeSceneData = sceneDataId;


		// move the player and managers to the scene that we are migrating into.
		main_player.transform.parent = null;

		SceneManager.MoveGameObjectToScene( gameObject, scenes[ sceneDataId ].scene );
		SceneManager.MoveGameObjectToScene( main_player.gameObject, scenes[ sceneDataId ].scene );

		main_player.transform.parent = scenes[ sceneDataId ].scene.GetRootGameObjects()[ 0 ].transform;	// is this a good idear??

	}
	
}

[System.Serializable]
public struct TerainVector
{
	public int width, length;

	public TerainVector(int w, int l)
	{
		width = w;
		length = l;
	}

	public TerainVector( Vector3 size )
	{
		width =  (int)size.x;
		length = (int)size.z;
	}

	public static TerainVector operator+ (TerainVector lhs, TerainVector rhs)
	{
		lhs.width  += rhs.width;
		lhs.length += rhs.length;

		return lhs;
	}

	public static TerainVector operator- ( TerainVector lhs, TerainVector rhs )
	{
		lhs.width -= rhs.width;
		lhs.length -= rhs.length;

		return lhs;
	}

	public static bool operator== ( TerainVector lhs, TerainVector rhs )
	{
		lhs.width  += rhs.width;
		lhs.length += rhs.length;

		return lhs.width == rhs.width && lhs.length == rhs.length;
	}

	public static bool operator!= ( TerainVector lhs, TerainVector rhs )
	{
		lhs.width  += rhs.width;
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
	public bool inActive;

	public TerainVector areaSize;
	public TerainVector areaPosition;

	public Scene scene;	// null if not loaded.

	//Sets the area position and moves the root object to that position in world space.
	public void SetAreaPosition( Vector3 position )
	{

		if ( !SceneIsAvailable() )
		{
			Debug.LogError( "Scene not loaded. Unable to update it position." );
			return;
		}

		areaPosition.width =  (int)position.x;
		areaPosition.length = (int)position.z;

		scene.GetRootGameObjects()[ 0 ].transform.position = position;

	}

	public bool SceneIsAvailable()
	{
		return scene != null && scene.IsValid() && scene.isLoaded;
	}

}

*/