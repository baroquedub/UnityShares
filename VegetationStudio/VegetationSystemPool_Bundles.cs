/// <summary>
/// Vegetation Studio pooling system
/// v 1.0.0 hacked together by baroquedub based on the script by Peter@ProceduralWorlds (MrWagoner)
/// If you want to say thanks: https://www.buymeacoffee.com/baroquedub
/// </summary>

using AwesomeTechnologies;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;



public class VegetationSystemPool_Bundles : MonoBehaviour {
	public bool useDebug = false;
	[Tooltip("For display only - this is the prefix string that gets removed from terrain names in the console log, to make things easier to read")]
	public string debugTerrainName = "";

    public uint poolSize = 10;
    public GameObject vsSystemPrefab;
    public VegetationPackage vsPackage;

	public string persistentVegAssetBundle = "persistentstorage1"; //"vegetationpersistentstorage"

	public string persistentStorageAssetPrefix = "";
	public string persistentStorageAssetSuffix = "_PersistentVegetationStoragePackage";

    List<VegetationSystem> existingVS = new List<VegetationSystem>();
    AssetBundle persistentStorageBundle;

	string shortenTerrainName(string name){
		return name.Replace(debugTerrainName,"T_");
	}

	void OnEnable() {
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	void OnDisable() {
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {

		GameObject terrainParent = GameObject.Find(scene.name);
		if(terrainParent){
			if(useDebug) Debug.Log("Loading "+scene.name+" FOUND a GO named after terrain");
			Terrain theTerrain = terrainParent.transform.GetChild(0).GetComponent<Terrain>();
			if(theTerrain){
				if(useDebug) Debug.Log(">> "+scene.name+" FOUND terrain "+shortenTerrainName(theTerrain.name)+" assigning");
				AssignTerrainToFreeVS(theTerrain);
			}else{
				if(useDebug) Debug.Log(">> "+scene.name+" Couldn't find first child or terrain");
			}
		}else{
			if(useDebug) Debug.Log("Loading "+scene.name+" Couldn't find a GO named after terrain");
		}
	}

	void Start () {
        for (int i = 0; i < poolSize; i++)
        {
			AddVegetationSystem(i.ToString());
        }

         persistentStorageBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, persistentVegAssetBundle));
        if (persistentStorageBundle == null)
        {
            if(useDebug) Debug.Log("Failed to load persistent vegetation storage AssetBundle!");
            return;
        }
		if(useDebug) Debug.Log("pool size check: "+existingVS.Count);
    }

    private void OnDestroy()
    {
        persistentStorageBundle.Unload(false);
    }
		

	void AddVegetationSystem(string num)
    {
        GameObject newGO = GameObject.Instantiate(vsSystemPrefab, this.transform);
		newGO.name = "VegetationSystem_Clone("+num+")";
        existingVS.Add(newGO.GetComponent<VegetationSystem>());
    }


    public void AssignTerrainToFreeVS(Terrain terrain)
    {
		if(useDebug) Debug.Log("<b>Starting trying</b> to AssignTerrainToFreeVS() on "+shortenTerrainName(terrain.name));
		//VegetationSystem targetVS = existingVS.Find(x => x.currentTerrain == null);
		VegetationSystem targetVS = null;

		for (int i = 0; i < existingVS.Count; i++) {
			if (existingVS[i].currentTerrain == null) {
				if(useDebug) Debug.Log (shortenTerrainName(terrain.name)+ " > i=" +i.ToString()+". <color=green>VS System" + existingVS[i].name + " has currentTerrain as NULL</color>");
				targetVS = existingVS[i];
				break;
			}else{
				if(useDebug) Debug.Log (shortenTerrainName(terrain.name)+ " " +i.ToString()+". <color=red>VS System: " + existingVS[i].name + " has currentTerrain "+existingVS[i].currentTerrain.name+"</color>");
			}
		}


        if (targetVS!=null)
        {
			var storagePackage = persistentStorageBundle.LoadAsset<PersistentVegetationStoragePackage> (persistentStorageAssetPrefix+terrain.name + persistentStorageAssetSuffix+".asset"); 
			if(useDebug) Debug.Log("<color=green>Adding PersistentVSPackage to terrain: " + shortenTerrainName(terrain.name)+"</color>");
            AssignTerrainToVS(terrain, targetVS, vsPackage, storagePackage);
		}else{
			if(useDebug) Debug.Log("<color=red>targetVS is null</color> on terrain: " + shortenTerrainName(terrain.name) + " _<b>can't assign</b>");
		}

    }


    void AssignTerrainToVS(Terrain terrain, VegetationSystem vs, VegetationPackage vegetationPackage, PersistentVegetationStoragePackage storagePackage)
    {
        vs.AutoselectTerrain = false;
        
        vs.CurrentVegetationPackage = vegetationPackage;
        PersistentVegetationStorage storage = vs.GetComponent<PersistentVegetationStorage>();
        if (storage != null)
        {
            storage.SetPersistentVegetationStoragePackage(storagePackage);
        }
        if (vs.InitDone)
        {
            vs.HotswapTerrain(terrain, new UnityTerrainData(terrain));
        }
        else
        {
            vs.SetTerrain(terrain);
        }

    }
}
