using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] {GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION})]
    public class SimuLiteLoader : ScenarioModule
    {
        public void Start()
        {
            string finalPath = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/SimuLite_backup.sfs";
            if (HighLogic.LoadedSceneIsGame && !(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor))
            { //Don't load the backup if currently in the flight scene
                SimuLite.LoadBackupFile(HighLogic.LoadedScene);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            //load the saved data from our node
            try
            {
                if (node.HasNode("SimuLite"))
                {
                    ConfigNode ourNode = node.GetNode("SimuLite");
                    //get the corehours from the node
                    string coreHoursStr = ourNode.GetValue(nameof(StaticInformation.RemainingCoreHours));
                    double coreHours = 0;
                    if (double.TryParse(coreHoursStr, out coreHours))
                    {
                        StaticInformation.RemainingCoreHours = coreHours;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            try
            {
                //Add the remaining core hours to the save file
                ConfigNode ourNode = new ConfigNode("SimuLite");
                ourNode.AddValue(nameof(StaticInformation.RemainingCoreHours), StaticInformation.RemainingCoreHours);
                node.AddNode("SimuLite", ourNode);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            base.OnSave(node);
            
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class SimuLite : MonoBehaviour
    {
        public static SimuLite Instance { get; private set; }
        public const string BACKUP_FILENAME = "SimuLite_backup";

        #region Fields
        private double lastUT = -1;
        #endregion Fields


        #region Public Properties

        #endregion Public Properties


        //public void Awake()
        //{
        //    if (HighLogic.LoadedSceneIsEditor && KSP.UI.Screens.StageManager.Instance != null)
        //    {
        //        Destroy(KSP.UI.Screens.StageManager.Instance);
        //    }
        //}

        public void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            //load config
            Configuration.LoadFromFile();
            Configuration.SaveToFile();

            if (HighLogic.LoadedSceneIsEditor) //hacky hack for now
            {
                simConfigWindow.Show();
            }
            if (StaticInformation.IsSimulating && HighLogic.LoadedSceneIsFlight)
            {
                lastUT = Planetarium.GetUniversalTime();
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !StaticInformation.IsSimulating)
            {
                return;
            }
            //remove some corehours based on how much time has passed since the last frame
            double UT = Planetarium.GetUniversalTime();
            StaticInformation.RemainingCoreHours -= (UT - lastUT) * StaticInformation.CurrentComplexity;
            lastUT = UT;

            if (StaticInformation.RemainingCoreHours <= 0)
            {
                //pause. Popup message saying out of time, purchase more or revert
                pauseWindow.Show();
            }

            
            //if (PauseMenu.exists && PauseMenu.isOpen) //close the regular pause menu
            //{
            //    PauseMenu.Close();
            //}
            if (GameSettings.PAUSE.GetKey()) //if paused, show our window
            {   
                pauseWindow.Show(); //show ours
            }

        }

        #region GUI Code
        private SimulationConfigWindow simConfigWindow = new SimulationConfigWindow();
        private PauseWindow pauseWindow = new PauseWindow();

        private void OnGUI()
        {
            simConfigWindow.OnGUIHandler();
            pauseWindow.OnGUIHandler();
        }
        #endregion GUI Code

        #region Public Methods
        public void ActivateSimulation(SimulationConfiguration simulation)
        {
            MakeBackupFile();
            StaticInformation.Simulation = simulation;
            StaticInformation.IsSimulating = true;
            StaticInformation.LastEditor = HighLogic.CurrentGame.editorFacility;
            StaticInformation.LastShip = ShipConstruction.ShipConfig;
            activateSimulationLocks();
            lastUT = Planetarium.GetUniversalTime();
        }

        public void DeactivateSimulation(bool returnToEditor)
        {
            if (!StaticInformation.IsSimulating)
            {
                return;
            }

            StaticInformation.IsSimulating = false;

            deactivateSimulationLocks();

            GameScenes targetScene = HighLogic.LoadedScene;
            if (returnToEditor) //if we should return to the editor, then do that rather than the current scene
            {
                targetScene = GameScenes.EDITOR;
            }

            LoadBackupFile(targetScene);
        }

        public static void LoadBackupFile(GameScenes targetScene)
        {
            string finalPath = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/" + BACKUP_FILENAME + ".sfs";
            if (File.Exists(finalPath))
            { //Load the backup file if it exists
                //File.Copy(finalPath, KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs", true);
                //File.Delete(finalPath);

                Instance.StartCoroutine(loadBackup(targetScene, finalPath));
                


                //return true;
            }
            //return false;
        }

        public void MakeBackupFile()
        {
            GamePersistence.SaveGame(BACKUP_FILENAME, HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        /// <summary>
        /// Gets the (integer) level of a particular facility
        /// </summary>
        /// <param name="facility">The facility to check</param>
        /// <returns>The level of the facility</returns>
        public static int GetFacilityLevel(SpaceCenterFacility facility)
        {
            int lvl = 0;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                lvl = (int)(ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility) * ScenarioUpgradeableFacilities.GetFacilityLevel(facility));
            }
            else
            {
                lvl = ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility); //returns 2 for VAB in Sandbox
            }
            return lvl;
        }

        #endregion Public Methods

        #region Private Methods
        private void activateSimulationLocks()
        {
            string pre = "SIMULITE_";
            InputLockManager.SetControlLock(ControlTypes.QUICKLOAD, pre + "QUICKLOAD");
            InputLockManager.SetControlLock(ControlTypes.QUICKSAVE, pre + "QUICKSAVE");
        }

        private void deactivateSimulationLocks()
        {
            string pre = "SIMULITE_";
            InputLockManager.RemoveControlLock(pre + "QUICKLOAD");
            InputLockManager.RemoveControlLock(pre + "QUICKSAVE");
        }


        //Credit to QuickGoTo mod by Malah. Apparently you have to wait until the end of the frame or the game crashes...
        private static IEnumerator loadScene(GameScenes scene)
        {
            yield return new WaitForEndOfFrame();
            HighLogic.LoadScene(scene);
        }

        private static IEnumerator loadBackup(GameScenes targetScene, string path)
        {
            yield return new WaitForEndOfFrame();
            ConfigNode lastShip = StaticInformation.LastShip;
            if (lastShip == null)
            {
                lastShip = ShipConstruction.ShipConfig;
            }
            EditorFacility lastEditor = StaticInformation.LastEditor;
            if (lastEditor == EditorFacility.None)
            {
                lastEditor = HighLogic.CurrentGame.editorFacility;
            }

            Game newGame = GamePersistence.LoadGame(BACKUP_FILENAME, HighLogic.SaveFolder, true, false);
            GamePersistence.SaveGame(newGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            //GameScenes targetScene = HighLogic.LoadedScene;
            newGame.startScene = targetScene;

            // This has to be before... newGame.Start()
            if (targetScene == GameScenes.EDITOR)
            {
                if (lastEditor == EditorFacility.None)
                {
                    lastEditor = EditorFacility.VAB;
                }
                newGame.editorFacility = lastEditor;
            }


            newGame.Start();

            // ... And this has to be after. <3 KSP
            if (targetScene == GameScenes.EDITOR)
            {
                EditorDriver.StartupBehaviour = EditorDriver.StartupBehaviours.LOAD_FROM_CACHE;
                ShipConstruction.ShipConfig = lastShip;
            }

            File.Delete(path);
        }
        #endregion Private Methods
    }
}
