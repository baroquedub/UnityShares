/// <summary>
/// v 1.0.0 hacked together by baroquedub based on the script by Kostiantyn Dvornik
/// If you want to say thanks: https://www.buymeacoffee.com/baroquedub

/// original about: http://kostiantyn-dvornik.blogspot.com/2013/12/unity-split-terrain-script.html
/// original download: https://www.dropbox.com/s/6drt9vccbl4bzgf/Dvornik-Split-Terrain.unitypackage?dl=0
/// </summary>
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

/// <summary>
/// Split terrain.
/// </summary>
public class JdpSplitTerrain : EditorWindow {

	List<TerrainData> terrainData = new List<TerrainData>();
	List<GameObject> terrainGo = new List<GameObject>();

	List<Terrain> terrainMatrix = new List<Terrain>(); // cache each iteration of created terrain tiles, so each one can be split again as per iterations value
	List<string> terrainAssets = new List<string>(); // cache all created terrain data assets (by name, so unused ones can be deleted)
	
	Terrain parentTerrain;
	GameObject cachedParent;
	
	const int terrainsCount = 4;	

	const string _DEFAULT_FOLDER = "_Split-Terrains";
	private int iterations = 2;	
	private string _assetsPath = "Assets/"+_DEFAULT_FOLDER;

	private bool _debugToLog = false;




	// Add submenu
    [MenuItem("Split Terrain/JDP Split Terrain Tool")]
	static void Init()
    {
		
		// Get existing open window or if none, make a new one:
		JdpSplitTerrain window = (JdpSplitTerrain)EditorWindow.GetWindow(typeof(JdpSplitTerrain));
		
      	window.minSize =  new Vector2( 460f,300f );		
//		window.maxSize =  new Vector2( 200f,200f );		
		
		window.autoRepaintOnSceneChange = true;
       	window.title = "Split terrain";
       	window.Show();
							
			
	}
	
	/// <summary>
	/// Determines whether this instance is power of two the specified x.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is power of two the specified x; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='x'>
	/// If set to <c>true</c> x.
	/// </param>
	bool IsPowerOfTwo(int x)
	{
    	return (x & (x - 1)) == 0;
	}

	int FastPower(int x, int pow)
    {
        switch (pow)
        {
            case 0: return 1;
            case 1: return x;
            case 2: return x * x;
            case 3: return x * x * x;
            case 4: return x * x * x * x;
            case 5: return x * x * x * x * x;
            case 6: return x * x * x * x * x * x;
            case 7: return x * x * x * x * x * x * x;
            case 8: return x * x * x * x * x * x * x * x;
            case 9: return x * x * x * x * x * x * x * x * x;
            case 10: return x * x * x * x * x * x * x * x * x * x; 
            case 11: return x * x * x * x * x * x * x * x * x * x * x; 
            // up to 32 can be added 
            default: // Vilx's solution is used for default
                int ret = 1;
                while (pow != 0)
                {
                    if ((pow & 1) == 1)
                        ret *= x;
                    x *= x;
                    pow >>= 1;
                }
                return ret;
        }
    }

	void DebugList(List<Terrain> ters){
	Debug.Log("------debugging terrainMatrix List --------");
		Debug.Log("Total count:"+ters.Count);
		for(int i=0; i < ters.Count; i++){
			if(ters[i] == null){
				Debug.Log("failed on terrain at i("+i+") "+ters[i].ToString());
			}else{
			Debug.Log("terrainGO at i("+i+") = "+ters[i].gameObject.name);
			}
			Debug.Log("-- with associated terrainAsset: "+terrainAssets[i]);
		}
	Debug.Log("//END/----debugging terrainMatrix List ------");
	}
			
	void SplitIt()
	{
		

		cachedParent = Selection.activeGameObject;
		if ( cachedParent == null )
		{
			Debug.LogWarning("No terrain was selected");
			return;
		}
		
		parentTerrain = cachedParent.GetComponent(typeof(Terrain)) as Terrain;

		// clear Lists
		terrainData.Clear();
		terrainGo.Clear();
		terrainMatrix.Clear();
		terrainAssets.Clear();

				
		
		if ( parentTerrain == null )
		{
			Debug.LogWarning("Current selection is not a terrain");
			return;
		}

		_assetsPath = RemoveTrailingSlash(_assetsPath); // ensures that there isn't an ending slash

		if (!AssetDatabase.IsValidFolder(_assetsPath))
        {
			if(_debugToLog) Debug.Log("Asset path is invalid. Creating your folders");
			CreateAssetFolders();
        }


		int totalInMatrix = 0;

		for ( int iterationsCount=0; iterationsCount < iterations; iterationsCount++)
		{

			if(iterationsCount==0){ // first iteration
				DoFourWaySplit();
				//DEBUG
				if(_debugToLog) {
					Debug.Log("Iteration: "+iterationsCount+" end of initial four waysplit");
					Debug.Log("TerrainMatrix size: "+terrainMatrix.Count);
					Debug.Log("###############################################");
				}
				//end DEBUG

				cachedParent.SetActive(false);

			}else{ // subsequent repeats

				totalInMatrix = FastPower(terrainsCount,iterationsCount); // 4^1 = 4

				//DEBUG
				if(_debugToLog) {
					Debug.Log("Start Iteration: "+(iterationsCount+1).ToString());
					Debug.Log("TerrainMatrix size: "+terrainMatrix.Count);
					DebugList(terrainMatrix);
					Debug.Log("totalInMatrix: "+totalInMatrix);
				}
				//end DEBUG

				for(int i=0; i < totalInMatrix; i++){
					parentTerrain = terrainMatrix[i];

					if(_debugToLog) Debug.Log("About to do split on "+parentTerrain.gameObject.name);

					DoFourWaySplit();
				}

				// now clean up assets
				CleanUpAssets(totalInMatrix);

				if(_debugToLog) {
					Debug.Log("End Iteration: "+iterationsCount);
					Debug.Log("TerrainMatrix size: "+terrainMatrix.Count);

					Debug.Log("###############################################");
				}
			}

		}
		if(_debugToLog) DebugList(terrainMatrix);

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	void CleanUpAssets(int toIndex){
		if(_debugToLog) Debug.Log(" ## Start Cleanup ##");

		// delete terrain data assets. 
		// NB Removal index is always '0' because List collapses like a pack of cards - you always want to delete the bottom card
		for(int i=0; i<toIndex; i++){

		// terrainGos
			if(_debugToLog) Debug.Log("About to remove Go: "+ terrainMatrix[0].gameObject.name +" at Index i: "+0);

			DestroyImmediate(terrainMatrix[0].gameObject);
			terrainMatrix.RemoveAt(0);

		// assets
			if(_debugToLog) Debug.Log("About to delete: "+_assetsPath+"/" +terrainAssets[0]);

			AssetDatabase.DeleteAsset(_assetsPath+"/" +terrainAssets[0]);
			terrainAssets.RemoveAt(0);

		}

		if(_debugToLog){
			Debug.Log("Removed terrains indexed from 0 to "+toIndex);
			DebugList(terrainMatrix);
			Debug.Log(" ## END Cleanup ##");
		}
	}

	void DoFourWaySplit(){
		//Split terrain 
		for ( int i=0; i< terrainsCount; i++)
		{										
			
			EditorUtility.DisplayProgressBar("Split terrain","Process " + i, (float) i / terrainsCount );
								
			TerrainData td = new TerrainData();
			GameObject tgo = Terrain.CreateTerrainGameObject( td );
		
			tgo.name = parentTerrain.name + "_" + i;
			
			terrainData.Add( td );
			terrainGo.Add ( tgo );
			
			Terrain genTer = tgo.GetComponent(typeof(Terrain)) as Terrain;								
			genTer.terrainData = td;

			AssetDatabase.CreateAsset(td, _assetsPath+"/" + genTer.name+ ".asset");

			// added chunked tiles to lists for further iterations
			terrainMatrix.Add ( genTer );
			terrainAssets.Add (genTer.name+ ".asset"); 
			
			
			// Assign splatmaps
			genTer.terrainData.splatPrototypes = parentTerrain.terrainData.splatPrototypes;
			
			// Assign detail prototypes
			genTer.terrainData.detailPrototypes = parentTerrain.terrainData.detailPrototypes;
						
			// Assign tree information
			genTer.terrainData.treePrototypes = parentTerrain.terrainData.treePrototypes;
			
									
			// Copy parent terrain propeties
			#region parent properties
			genTer.basemapDistance = parentTerrain.basemapDistance;			
			genTer.castShadows = parentTerrain.castShadows;
			genTer.detailObjectDensity = parentTerrain.detailObjectDensity;
			genTer.detailObjectDistance = parentTerrain.detailObjectDistance;
			genTer.heightmapMaximumLOD = parentTerrain.heightmapMaximumLOD;
			genTer.heightmapPixelError = parentTerrain.heightmapPixelError;
			genTer.treeBillboardDistance = parentTerrain.treeBillboardDistance;
			genTer.treeCrossFadeLength = parentTerrain.treeCrossFadeLength;
			genTer.treeDistance = parentTerrain.treeDistance;
			genTer.treeMaximumFullLODCount = parentTerrain.treeMaximumFullLODCount;
			
			#endregion
			
			//Start processing it			
						
			// Translate peace to position
			#region translate peace to right position 
			
			Vector3 parentPosition = parentTerrain.GetPosition();
			
			int terraPeaces = (int) Mathf.Sqrt( terrainsCount );
			
			float spaceShiftX = parentTerrain.terrainData.size.z / terraPeaces;
			float spaceShiftY = parentTerrain.terrainData.size.x / terraPeaces;
			
			float xWShift = (i % terraPeaces ) * spaceShiftX;
			float zWShift = ( i / terraPeaces ) * spaceShiftY;
						
			tgo.transform.position = new Vector3( tgo.transform.position.x + zWShift,
												  tgo.transform.position.y,
												  tgo.transform.position.z + xWShift ); 	
			
			// Shift last position
			tgo.transform.position = new Vector3( tgo.transform.position.x + parentPosition.x,
												  tgo.transform.position.y + parentPosition.y,
												  tgo.transform.position.z + parentPosition.z
												 );
			
			
			
			#endregion 
			
			// Split height
			#region split height
			
			if(_debugToLog) Debug.Log ( "Split height on"+ tgo.name );
			
			//Copy heightmap											
			td.heightmapResolution = parentTerrain.terrainData.heightmapResolution /  terraPeaces;							
			
			//Keep y same
			td.size = new Vector3( parentTerrain.terrainData.size.x / terraPeaces,
								   parentTerrain.terrainData.size.y,
								   parentTerrain.terrainData.size.z / terraPeaces 
								  );
			
			float[,] parentHeight = parentTerrain.terrainData.GetHeights(0,0, parentTerrain.terrainData.heightmapResolution, parentTerrain.terrainData.heightmapResolution );
			
			float[,] peaceHeight = new float[ parentTerrain.terrainData.heightmapResolution / terraPeaces + 1,
											  parentTerrain.terrainData.heightmapResolution / terraPeaces + 1
											];
			
			// Shift calc
			int heightShift = parentTerrain.terrainData.heightmapResolution / terraPeaces;								
					
			int startX = 0;
			int startY = 0;
			
			int endX = 0;
			int endY = 0;
			
			if ( i==0 )
			{
				startX = startY = 0;				
				endX = endY = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
			}
			
			if ( i==1 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
				endY = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
			}
			
			if ( i==2 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
				endY = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
			}
			
			if ( i==3 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
				endY = parentTerrain.terrainData.heightmapResolution / terraPeaces + 1;
			}
									
			// iterate
			for ( int x=startX;x< endX;x++)
			{	
				
				EditorUtility.DisplayProgressBar("Split terrain","Split height", (float) x / ( endX - startX ));  
				
				for ( int y=startY;y< endY;y++)
				{
				
					int xShift=0; 
					int yShift=0;
					
					//
					if ( i==0 )
					{
						xShift = 0;
						yShift = 0;						
					}
					
					//
					if ( i==1 )
					{						
						xShift = heightShift;
						yShift = 0;						
					}
					
					//
					if ( i==2 )
					{
						xShift = 0;
						yShift = heightShift;	
					}
					
					if ( i==3 )
					{
						xShift = heightShift;
						yShift = heightShift;	
					}
					
					float ph = parentHeight[ x + xShift,y + yShift];	
												
					peaceHeight[x ,y ] = ph;
					
				}
														
			}
			
			EditorUtility.ClearProgressBar();
			
			// Set heightmap to child
			genTer.terrainData.SetHeights( 0,0, peaceHeight );
			#endregion
			
			// Split splat map
			#region split splat map	
								
			td.alphamapResolution = parentTerrain.terrainData.alphamapResolution /  terraPeaces;													
			
			float[,,] parentSplat = parentTerrain.terrainData.GetAlphamaps(0,0, parentTerrain.terrainData.alphamapResolution, parentTerrain.terrainData.alphamapResolution );			

			float[,,] peaceSplat = new float[ parentTerrain.terrainData.alphamapResolution / terraPeaces ,
											  parentTerrain.terrainData.alphamapResolution / terraPeaces,
											  parentTerrain.terrainData.alphamapLayers
											];
									
			// Shift calc
			int splatShift = parentTerrain.terrainData.alphamapResolution / terraPeaces;								
												
			if ( i==0 )
			{
				startX = startY = 0;				
				endX = endY = parentTerrain.terrainData.alphamapResolution / terraPeaces;
			}
			
			if ( i==1 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.alphamapResolution / terraPeaces;
				endY = parentTerrain.terrainData.alphamapResolution / terraPeaces;
			}
			
			if ( i==2 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.alphamapResolution / terraPeaces;
				endY = parentTerrain.terrainData.alphamapResolution / terraPeaces;
			}
			
			if ( i==3 )
			{
				startX = startY = 0;				
				endX = parentTerrain.terrainData.alphamapResolution / terraPeaces;
				endY = parentTerrain.terrainData.alphamapResolution / terraPeaces;
			}
			
			// iterate
			for ( int s=0;s<parentTerrain.terrainData.alphamapLayers;s++)
			{				
				for ( int x=startX;x< endX;x++)
				{	
					
					EditorUtility.DisplayProgressBar("Split terrain","Split splat", (float) x / ( endX - startX ));  
					
					for ( int y=startY;y< endY;y++)
					{
					
						int xShift=0; 
						int yShift=0;
						
						//
						if ( i==0 )
						{
							xShift = 0;
							yShift = 0;						
						}
						
						//
						if ( i==1 )
						{						
							xShift = splatShift;
							yShift = 0;						
						}
						
						//
						if ( i==2 )
						{
							xShift = 0;
							yShift = splatShift;	
						}
						
						if ( i==3 )
						{
							xShift = splatShift;
							yShift = splatShift;	
						}
						
						float ph = parentSplat[x + xShift,y + yShift, s];	
						peaceSplat[x ,y, s] = ph;
						
					}
															
					
				}			
			}
			
			EditorUtility.ClearProgressBar();
			
			// Set heightmap to child
			genTer.terrainData.SetAlphamaps( 0,0, peaceSplat );
			#endregion
				
			// Split detail map
			#region split detail map	
							
			td.SetDetailResolution( parentTerrain.terrainData.detailResolution / terraPeaces, 8 );													
						
			for ( int detLay=0; detLay< parentTerrain.terrainData.detailPrototypes.Length; detLay++)
			{								
				int[,] parentDetail = parentTerrain.terrainData.GetDetailLayer(0,0, parentTerrain.terrainData.detailResolution, parentTerrain.terrainData.detailResolution, detLay );			
	
				int[,] peaceDetail = new int[ parentTerrain.terrainData.detailResolution / terraPeaces,
											  parentTerrain.terrainData.detailResolution / terraPeaces												  
											];
										
				// Shift calc
				int detailShift = parentTerrain.terrainData.detailResolution / terraPeaces;								
													
				if ( i==0 )
				{
					startX = startY = 0;				
					endX = endY = parentTerrain.terrainData.detailResolution / terraPeaces;
				}
				
				if ( i==1 )
				{
					startX = startY = 0;				
					endX = parentTerrain.terrainData.detailResolution / terraPeaces;
					endY = parentTerrain.terrainData.detailResolution / terraPeaces;
				}
				
				if ( i==2 )
				{
					startX = startY = 0;				
					endX = parentTerrain.terrainData.detailResolution / terraPeaces;
					endY = parentTerrain.terrainData.detailResolution / terraPeaces;
				}
				
				if ( i==3 )
				{
					startX = startY = 0;				
					endX = parentTerrain.terrainData.detailResolution / terraPeaces;
					endY = parentTerrain.terrainData.detailResolution / terraPeaces;
				}
				
				// iterate				
					for ( int x=startX;x< endX;x++)
					{		
					
						EditorUtility.DisplayProgressBar("Split terrain","Split detail", (float) x / (endX - startX ));
					
						for ( int y=startY;y< endY;y++)
						{
						
							int xShift=0; 
							int yShift=0;
							
							//
							if ( i==0 )
							{
								xShift = 0;
								yShift = 0;						
							}
							
							//
							if ( i==1 )
							{						
								xShift = detailShift;
								yShift = 0;						
							}
							
							//
							if ( i==2 )
							{
								xShift = 0;
								yShift = detailShift;	
							}
							
							if ( i==3 )
							{
								xShift = detailShift;
								yShift = detailShift;	
							}
							
							int ph = parentDetail[x + xShift,y + yShift];	
							peaceDetail[x ,y] = ph;
							
						}										
					
				}				
				EditorUtility.ClearProgressBar();
				
				// Set heightmap to child
				genTer.terrainData.SetDetailLayer( 0,0, detLay, peaceDetail );
				
			}
				#endregion
					
			// Split tree data
			#region  split tree data
			
			for( int t=0; t< parentTerrain.terrainData.treeInstances.Length;t++)
			{
					
				EditorUtility.DisplayProgressBar("Split terrain","Split trees "  , (float) t / parentTerrain.terrainData.treeInstances.Length );					
					
				// Get tree instance					
				TreeInstance ti = parentTerrain.terrainData.treeInstances[t];				
												
				// First section	
				if ( i==0 && 
					 ti.position.x > 0f &&	ti.position.x < 0.5f &&
					 ti.position.z > 0f &&	ti.position.z < 0.5f 
					)
				{
					// Recalculate new tree position	
					ti.position = new Vector3( ti.position.x * 2f, ti.position.y, ti.position.z * 2f );
						
					// Add tree instance						
					genTer.AddTreeInstance( ti );												
				}
					
				// Second section
				if ( i==1 && 
					 ti.position.x > 0.0f &&ti.position.x < 0.5f &&
					 ti.position.z >= 0.5f &&	ti.position.z <= 1.0f 
					)
				{
					// Recalculate new tree position	
					ti.position = new Vector3( (ti.position.x ) * 2f, ti.position.y, ( ti.position.z - 0.5f ) * 2f );
						
					// Add tree instance						
					genTer.AddTreeInstance( ti );												
				}
					
				// Third section
				if ( i==2 && 
					 ti.position.x >= 0.5f && ti.position.x <= 1.0f &&
					 ti.position.z > 0.0f && ti.position.z < 0.5f 
					)
				{
					// Recalculate new tree position	
					ti.position = new Vector3( (ti.position.x - 0.5f ) * 2f, ti.position.y, ( ti.position.z ) * 2f );
						
					// Add tree instance						
					genTer.AddTreeInstance( ti );												
				}
									
				// Fourth section
				if ( i==3 && 
					 ti.position.x >= 0.5f && ti.position.x <= 1.0f &&
					 ti.position.z >= 0.5f && ti.position.z <= 1.0f 
					)
				{
					// Recalculate new tree position	
					ti.position = new Vector3( (ti.position.x - 0.5f ) * 2f, ti.position.y, ( ti.position.z - 0.5f ) * 2f );
						
					// Add tree instance						
					genTer.AddTreeInstance( ti );												
				}
					
					
			}											
			#endregion	
				
			AssetDatabase.SaveAssets();



		}
		
		EditorUtility.ClearProgressBar();

	}


	void ClearAllTerrainTiles(){

		var terrainTiles = GameObject.FindObjectsOfType<Terrain>();

		foreach (Terrain terrainTile in terrainTiles)
        {
			GameObject theTerrain = terrainTile.gameObject;
			if(theTerrain.activeInHierarchy) {
				DestroyImmediate(theTerrain); // remove
			}else{
				theTerrain.SetActive(true); // re-enable original parent
			}
		}



		// Now clear persistent storage
		ClearAllStorageInPath();

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void ClearAllStorageInPath(){
		string _assetsPathFix = RemoveTrailingSlash(_assetsPath);

		if (AssetDatabase.IsValidFolder(_assetsPathFix))
        {
			string[] guids = AssetDatabase.FindAssets("t:TerrainData", new[]{_assetsPathFix}); // search for type t:PersistentVegetationStorage didn't work

			//Debug.Log(guids.Length + " Assets in "+_assetsPath);

		        foreach (string guid in guids)
		        {
					string assetPath = AssetDatabase.GUIDToAssetPath (guid);
					//Debug.Log("going to delete: "+assetPath);
					AssetDatabase.DeleteAsset(assetPath);
		        }
        }
    }




	void OnGUI()
    {
		GUILayout.BeginVertical("box");
		EditorGUILayout.HelpBox("This tool will split the selected terrain into 4 parts.\nEach part will then be split again, based on the number of iterations.\ne.g.\n - a 4K terrain split with 1 iterations will result in 4 tiles each 2048 in size.\n - a 4K terrain split with 2 iterations will result in 24 tiles each 1024 in size.\n - a 4K terrain split with 3 iterations will result in 128 tiles each of 512 in size.\n - a 4K terrain split with 4 iterations will result in 256 tiles each 256 in size. ", MessageType.Info);
			_debugToLog = EditorGUILayout.Toggle("Debug output to console", _debugToLog);
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
			iterations =
				EditorGUILayout.IntField("Number of split iterations", iterations);	
			_assetsPath =
				EditorGUILayout.TextField("Terrains storage path", _assetsPath);	
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
			if(GUILayout.Button("Split terrain", GUILayout.Height(50)))
	        {			
				
				SplitIt();							
			}
			
		GUILayout.EndVertical();	

		EditorGUILayout.Separator(); 

		GUILayout.BeginVertical("box");
		EditorGUILayout.HelpBox("This will delete all Terrain Tiles in the scene, except for the disabled parent, as well as any terrain data assets in the specified path. Use as an UNDO for previous split operation. ", MessageType.Info);
			if (GUILayout.Button("UNDO: delete all terrain tiles"))
	        {

				if(EditorUtility.DisplayDialog("Delete all tiles",
	                    "This will delete all Terrain Tiles in the scene, except for the disabled parent. Terrain tile assets in the specified path will also be deleted. Use with caution! ", "Delete all", "Cancel")){

						ClearAllTerrainTiles();

	                }

	        }
		GUILayout.EndVertical();									
	}
	

	void CreateAssetFolders(){

		string tempAssetsPath = _assetsPath.Replace("Assets/", "");
		if(tempAssetsPath == "Assets") tempAssetsPath = _DEFAULT_FOLDER; // place in default folder if path is in Asset root

			string[] folders = tempAssetsPath.Split('/');

			string newFolderPath = "Assets"; // starts as root parent
			for(int i=0; i < folders.Length; i++)
	        {
				if(_debugToLog) Debug.Log("Creating :"+folders[i]+" in "+ newFolderPath);

				if(!AssetDatabase.IsValidFolder(newFolderPath+"/"+folders[i])){ // check if folder needs to be created
					if(_debugToLog) Debug.Log("Created :"+folders[i]+" in "+ newFolderPath);
					string guid = AssetDatabase.CreateFolder (newFolderPath, folders[i]);
	            	newFolderPath = AssetDatabase.GUIDToAssetPath (guid); // created folder, so now it becomes parent for next iteration
	            }else{
					if(_debugToLog) Debug.Log("Already exists :"+folders[i]+" in "+ newFolderPath+" > updated parent path");
					newFolderPath+= "/"+folders[i];
	            }
	        }

    }

	string RemoveTrailingSlash(string filePath){
		return filePath.EndsWith("/") ? filePath.Substring(0, filePath.Length - 1) : filePath;
	}
	
}