/// <summary>
/// v1.0.1 hacked together by baroquedub from code by AwesomeTechnologies
/// If you want to say thanks: https://www.buymeacoffee.com/baroquedub
/// </summary>

using System.Collections;
using System.Collections.Generic;
using AwesomeTechnologies;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


public class VegetationSystemBatchBakeEditor : EditorWindow
{

    const string _DEFAULT_FOLDER = "_VS_Store";
    const string _DEFAULT_SUFFIX = "_persistentStore";

    private VegetationPackage _vegetationPackage;
    private GameObject _vegetationSystemPrefab;
    private bool _usePrefab;
    private GameObject _rootObject;
    private bool _debugToLog = false;
    private bool _addPersistentStorage = true;
    private bool _autoBake = true;
    private string _assetsPath = "Assets/" + _DEFAULT_FOLDER;



    [MenuItem("Window/Awesome Technologies/Multi terrain/Vegetation System Batch Bake")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(VegetationSystemBatchBakeEditor));
        window.minSize = new Vector2(460f, 600f);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Multi-Terrain VegetationSystem PersistentStorage Baker", EditorStyles.boldLabel);

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("A tool to add Vegetation System components on all terrains in the scene.\nThe tool can also bake persistent storage for you. ", MessageType.Info);
        _debugToLog = EditorGUILayout.Toggle("Debug output to console", _debugToLog);

        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("SOURCE Terrains: Choose a root object for Terrains.\nIf this is empty all terrains in the scene will be selected.", MessageType.Info);
        _rootObject =
            (GameObject)EditorGUILayout.ObjectField("", _rootObject, typeof(GameObject), true);
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");

        EditorGUILayout.HelpBox("REQUIRED: Select use prefab if you have a preconfigured vegetation system.\n Uncheck to use the supplied vegetation package (NB certain settings like water level are only set at by a VS prefab) ", MessageType.Info);
        _usePrefab = EditorGUILayout.Toggle("Use source prefab", _usePrefab);
        if (_usePrefab)
        {
            _vegetationSystemPrefab =
                (GameObject)EditorGUILayout.ObjectField("", _vegetationSystemPrefab, typeof(GameObject), true);
        }
        else
        {
            _vegetationPackage =
                (VegetationPackage)EditorGUILayout.ObjectField("", _vegetationPackage, typeof(VegetationPackage), true);


        }
        _addPersistentStorage = EditorGUILayout.Toggle("Add Persistent Storage", _addPersistentStorage);
        if (_addPersistentStorage)
        {
            if (_usePrefab)
            {
                _autoBake = false;
                EditorGUILayout.LabelField("Disabled because baking persitent storage on prefab instance is currently broken", EditorStyles.miniLabel);
            }
            // disable autoBake tick box if prefab is being used 
            using (new EditorGUI.DisabledScope(_usePrefab == true))
            {
                _autoBake = EditorGUILayout.Toggle("Bake Persistent Storage", _autoBake);
            }
            _assetsPath =
                EditorGUILayout.TextField("Baked Storage path", _assetsPath);
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        if (GUILayout.Button("Setup VS and Bake All", GUILayout.Height(50)))
        {

            if (EditorUtility.DisplayDialog("Bake vegetation",
                    "This will add VegetationSystems on all terrains and, if enabled, bake all Vegetation Items to the persistent storage. 'Enable run-time spawn' will be set to false after bake. ", "Do it", "Cancel"))
            {

                if (_usePrefab)
                {
                    SetupFromPrefab();
                }
                else
                {
                    SetupFromVegetationPackage();
                }

            }

        }
        GUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Additional Tools", EditorStyles.boldLabel);

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("This will delete all VegetationSystems on all terrains in the scene as well as any persistent storage assets in the specified path. ", MessageType.Info);
        if (GUILayout.Button("DELETE all VS and baked storage"))
        {

            if (EditorUtility.DisplayDialog("Delete all vegetation",
                    "This will delete all VegetationSystems in the scene and persistent storage assets in the specified path. Use with caution! ", "Delete all", "Cancel"))
            {

                ClearAllVegetationSystems();

            }

        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("Activate Runtime Spawn on all vegetation items for currently selected Vegetation Package. This is useful after a bake if you want to reactivate disabled items. ", MessageType.Info);
        if (GUILayout.Button("Activate Runtime Spawn"))
        {
            if (EditorUtility.DisplayDialog("Reactivate vegetation items",
            "This will re-enable Activate Runtime Spawn on all vegetation items. Are you sure?", "Enable items", "Cancel"))
            {

                SetAllItemsToRuntimeSpawn();

            }
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("Rebake existing PersistentStorage on all terrains. This will delete all existing data in the persistent storage assets used in this scene and reinitialise the selected Vegetation package. ", MessageType.Info);
        if (GUILayout.Button("Rebake ALL Vegetation Items"))
        {
            if (EditorUtility.DisplayDialog("Bake vegetation",
                "This will overwrite existing baked data in the scene. Are you sure?", "Do Rebake", "Cancel"))
            {

                ReBakeAllStorage();

            }
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("Bake all PersistentStorage found on all terrains. This will delete all existing data in the persistent storage assets used in this scene and reinitialise the selected Vegetation package. ", MessageType.Info);
        if (GUILayout.Button("Bake ALL Vegetation Items"))
        {
            if (EditorUtility.DisplayDialog("Force Bake vegetation",
                "This will overwrite existing baked data in the scene. Are you sure?", "Do Bake", "Cancel"))
            {

                ManualBake();

            }
        }
        GUILayout.EndVertical();
    }


    void SetupFromVegetationPackage()
    {
        if (_vegetationPackage == null)
        {
            Debug.LogWarning("You need to assign a vegetation package");
            return;
        }

        _assetsPath = RemoveTrailingSlash(_assetsPath); // ensures that there isn't an ending slash

        if (!AssetDatabase.IsValidFolder(_assetsPath))
        {
            if (_debugToLog) Debug.Log("Asset path is invalid. Creating your folders");
            CreateAssetFolders();
        }

        var terrains = _rootObject ? _rootObject.GetComponentsInChildren<Terrain>() : GameObject.FindObjectsOfType<Terrain>();
        int totalNum = terrains.Length;
        int counter = 0;
        foreach (Terrain terrain in terrains)
        {
            counter++;
            if (_addPersistentStorage)
            {
                // first check if a Vegetation System already exists
                VegetationSystem _vegetationSystem = terrain.gameObject.GetComponentInChildren<VegetationSystem>();
                if (_vegetationSystem == null)
                { // add if not already there
                    _vegetationSystem = VegetationStudioManager.AddVegetationSystemToTerrain(terrain, _vegetationPackage, true);
                    if (_debugToLog) Debug.Log("Added VS to " + terrain.name + " (" + counter + " of " + totalNum + ")");
                }

                if (_debugToLog) Debug.Log("Adding Persistent Storage " + terrain.name + " (" + counter + " of " + totalNum + ")");
                DoAddPersistentStorage(_vegetationSystem);

            }
            else
            {
                VegetationStudioManager.AddVegetationSystemToTerrain(terrain, _vegetationPackage);
            }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void SetupFromPrefab()
    {
        if (_vegetationSystemPrefab == null)
        {
            Debug.LogWarning("You need to assign a source prefab");
            return;
        }

        _assetsPath = RemoveTrailingSlash(_assetsPath); // ensures that there isn't an ending slash

        if (!AssetDatabase.IsValidFolder(_assetsPath))
        {
            if (_debugToLog) Debug.Log("Asset path is invalid. Creating your folders");
            CreateAssetFolders();
        }

        var terrains = _rootObject ? _rootObject.GetComponentsInChildren<Terrain>() : GameObject.FindObjectsOfType<Terrain>();
        int totalNum = terrains.Length;
        int counter = 0;
        foreach (Terrain terrain in terrains)
        {
            GameObject vegetationSystemObject = Instantiate(_vegetationSystemPrefab, terrain.gameObject.transform);
            VegetationSystem vegetationSystem = vegetationSystemObject.GetComponent<VegetationSystem>();
            if (vegetationSystem)
            {
                vegetationSystem.AutoselectTerrain = false;
                vegetationSystem.SetTerrain(terrain);

                counter++;
                if (_debugToLog) Debug.Log("Added VS Prefab to " + terrain.name + " (" + counter + " of " + totalNum + ")");
                if (_addPersistentStorage)
                {

                    if (_debugToLog) Debug.Log("Adding Persistent Storage " + terrain.name + " (" + counter + " of " + totalNum + ")");
                    DoAddPersistentStorage(vegetationSystem);

                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }




    void DoAddPersistentStorage(VegetationSystem _vegetationSystem)
    {

        // NB seems redundant to recurse into parent object - but this is needed to get the terrain name
        GameObject parentTerrainGo = _vegetationSystem.gameObject.transform.parent.gameObject;
        PersistentVegetationStorage _persistentVegetationStorage = parentTerrainGo.GetComponentInChildren<PersistentVegetationStorage>();

        PersistentVegetationStoragePackage newPersistentVegetationStoragePackage = ScriptableObject.CreateInstance<PersistentVegetationStoragePackage>();

        AssetDatabase.CreateAsset(newPersistentVegetationStoragePackage, _assetsPath + "/" + parentTerrainGo.name + _DEFAULT_SUFFIX + ".asset");
        _persistentVegetationStorage.PersistentVegetationStoragePackage = newPersistentVegetationStoragePackage;

        // initialize
        _persistentVegetationStorage.InitializePersistentStorage();

        if (_debugToLog) Debug.Log("Added storage asset for " + parentTerrainGo.name);

        if (_autoBake)
        {
            if (_debugToLog) Debug.Log("Auto Baking " + parentTerrainGo.name);
            // now bake
            List<string> vegetationItemIdList =
            VegetationPackageEditorTools.CreateVegetationInfoIdList(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);


            for (int i = 0; i <= vegetationItemIdList.Count - 1; i++)
            {
                _persistentVegetationStorage.BakeVegetationItem(vegetationItemIdList[i]);
            }
            _persistentVegetationStorage.VegetationSystem.DelayedClearVegetationCellCache();
            EditorUtility.SetDirty(_persistentVegetationStorage.PersistentVegetationStoragePackage);
            EditorUtility.SetDirty(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);
            if (_debugToLog) Debug.Log("Finished Baking for " + parentTerrainGo.name);
        }
    }




    void ReBakeAllStorage()
    {
        // do a rebake of existing persistent storage packages.
        // Not quite working as expected - adds additional VegetationPackages to the VegetationSystem component, rather than replacing the existing one
        // Also, buggy... seems to work first time but not on subsequent rebakes (baked storage ends up empty).

        if (_vegetationPackage == null)
        {
            Debug.LogWarning("You need to assign a vegetation package");
            return;
        }

        SetAllItemsToRuntimeSpawn(); // reactivates all vegetation items in selected package

        var vegetationSystems = _rootObject ? _rootObject.GetComponentsInChildren<VegetationSystem>() : GameObject.FindObjectsOfType<VegetationSystem>();

        foreach (VegetationSystem _vegetationSystem in vegetationSystems)
        {
            //_vegetationSystem.AddVegetationPackage(_vegetationPackage,true); // add new package and enable * TODO should not add but replace existing

            // todo get list of packages and remove old one

            PersistentVegetationStorage _persistentVegetationStorage = _vegetationSystem.gameObject.GetComponent<PersistentVegetationStorage>();

            // re-initialize - to clear existing and get it ready for new package
            _persistentVegetationStorage.InitializePersistentStorage();

            if (_debugToLog) Debug.Log("re-Initialized storage asset for " + _vegetationSystem.gameObject.transform.parent.gameObject.name);

            // now bake
            List<string> vegetationItemIdList =
            VegetationPackageEditorTools.CreateVegetationInfoIdList(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);

            for (int i = 0; i <= vegetationItemIdList.Count - 1; i++)
            {
                //_persistentVegetationStorage.RemoveVegetationItemInstances(vegetationItemIdList[i], 0); // not needed as already re-initialized
                _persistentVegetationStorage.BakeVegetationItem(vegetationItemIdList[i]);
            }
            _persistentVegetationStorage.VegetationSystem.DelayedClearVegetationCellCache();
            EditorUtility.SetDirty(_persistentVegetationStorage.PersistentVegetationStoragePackage);
            EditorUtility.SetDirty(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);
            if (_debugToLog) Debug.Log("Finished Baking for " + _vegetationSystem.gameObject.transform.parent.gameObject.name);

        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    }

    void ManualBake()
    {

        var vegetationSystems = _rootObject ? _rootObject.GetComponentsInChildren<VegetationSystem>() : GameObject.FindObjectsOfType<VegetationSystem>();

        foreach (VegetationSystem _vegetationSystem in vegetationSystems)
        {

            PersistentVegetationStorage _persistentVegetationStorage = _vegetationSystem.gameObject.GetComponent<PersistentVegetationStorage>();

            // now bake
            List<string> vegetationItemIdList =
            VegetationPackageEditorTools.CreateVegetationInfoIdList(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);

            for (int i = 0; i <= vegetationItemIdList.Count - 1; i++)
            {
                _persistentVegetationStorage.BakeVegetationItem(vegetationItemIdList[i]);
            }
            _persistentVegetationStorage.VegetationSystem.DelayedClearVegetationCellCache();
            EditorUtility.SetDirty(_persistentVegetationStorage.PersistentVegetationStoragePackage);
            EditorUtility.SetDirty(_persistentVegetationStorage.VegetationSystem.CurrentVegetationPackage);

        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    }


    void SetAllItemsToRuntimeSpawn()
    {
        if (_vegetationPackage == null)
        {
            Debug.LogWarning("You need to assign a vegetation package");
            return;
        }
        List<VegetationItemInfo> vegItems = _vegetationPackage.VegetationInfoList;
        foreach (VegetationItemInfo vegItem in vegItems)
        {
            vegItem.EnableRuntimeSpawn = true;
        }

    }


    void ClearAllVegetationSystems()
    {

        var vegetationSystems = _rootObject ? _rootObject.GetComponentsInChildren<VegetationSystem>() : GameObject.FindObjectsOfType<VegetationSystem>();

        foreach (VegetationSystem vegetationSystem in vegetationSystems)
        {
            DestroyImmediate(vegetationSystem.gameObject); // remove
        }

        // Now clear persistent storage
        ClearAllStorageInPath();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }


    void ClearAllStorageInPath()
    {
        string _assetsPathFix = RemoveTrailingSlash(_assetsPath);

        if (AssetDatabase.IsValidFolder(_assetsPathFix))
        {
            string[] guids = AssetDatabase.FindAssets("t:PersistentVegetationStoragePackage", new[] { _assetsPathFix }); // search for type t:PersistentVegetationStorage didn't work

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                //Debug.Log("going to delete: "+assetPath);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }


    void CreateAssetFolders()
    {

        string tempAssetsPath = _assetsPath.Replace("Assets/", "");
        if (tempAssetsPath == "Assets") tempAssetsPath = _DEFAULT_FOLDER; // place in default folder if path is in Asset root

        string[] folders = tempAssetsPath.Split('/');

        string newFolderPath = "Assets"; // starts as root parent
        for (int i = 0; i < folders.Length; i++)
        {
            if (_debugToLog) Debug.Log("Creating :" + folders[i] + " in " + newFolderPath);

            if (!AssetDatabase.IsValidFolder(newFolderPath + "/" + folders[i]))
            { // check if folder needs to be created
                if (_debugToLog) Debug.Log("Created :" + folders[i] + " in " + newFolderPath);
                string guid = AssetDatabase.CreateFolder(newFolderPath, folders[i]);
                newFolderPath = AssetDatabase.GUIDToAssetPath(guid); // created folder, so now it becomes parent for next iteration
            }
            else
            {
                if (_debugToLog) Debug.Log("Already exists :" + folders[i] + " in " + newFolderPath + " > updated parent path");
                newFolderPath += "/" + folders[i];
            }
        }

    }

    string RemoveTrailingSlash(string filePath)
    {
        return filePath.EndsWith("/") ? filePath.Substring(0, filePath.Length - 1) : filePath;
    }
}
