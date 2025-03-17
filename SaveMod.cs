using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[assembly: MelonInfo(typeof(SaveMod.MainMod), "Save Game Mod", "1.0.0", "Kua8 On Cord")]
[assembly: MelonGame("TVGS", "Schedule I Free Sample")]

namespace SaveMod
{
    public class MainMod : MelonMod
    {
        private static MainMod Instance;
        private static Type saveManagerType;
        private static object saveManagerInstance;
        private static MethodInfo saveGameMethod;
        private GameObject saveButton;
        private static float lastSaveCheck = 0f;
        private bool isInGame = false;
        private Type pauseMenuType;
        private GameObject pauseMenuObject;

        private void FindPauseMenu()
        {
            try
            {
                if (pauseMenuType == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name == "Assembly-CSharp")
                        {
                            pauseMenuType = asm.GetType("ScheduleOne.UI.PauseMenu");
                            break;
                        }
                    }
                }

                if (pauseMenuType != null)
                {
                    var pauseMenuComponent = GameObject.FindObjectOfType(pauseMenuType);
                    if (pauseMenuComponent != null)
                    {
                        pauseMenuObject = ((Component)pauseMenuComponent).gameObject;
                        
                        // Remove the "Found pause menu!" log message
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Failed to find pause menu: {ex.Message}");
            }
        }

        // Keep these utility methods but they won't be used for logging
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private void LogChildren(Transform parent, int depth)
        {
            // This method is kept for debugging purposes but won't be called
            string indent = new string(' ', depth * 2);
            foreach (Transform child in parent)
            {
                // Debug logging removed
                if (child.GetComponent<Button>() != null)
                {
                    Text buttonText = child.GetComponentInChildren<Text>();
                    if (buttonText != null)
                    {
                        // Debug logging removed
                    }
                }
                LogChildren(child, depth + 1);
            }
        }

        private void CreateSaveButton()
        {
            try
            {
                if (saveButton != null || !isInGame) return;

                // Find the pause menu first
                FindPauseMenu();
                if (pauseMenuObject == null)
                {
                    Instance.LoggerInstance.Error("No pause menu found!");
                    return;
                }

                // Find the Container/Bank where all menu items are stored
                Transform bank = pauseMenuObject.transform.Find("Container/Container/Bank");
                if (bank == null)
                {
                    Instance.LoggerInstance.Error("Could not find Bank container!");
                    return;
                }

                // Check if a save button already exists and remove it
                Transform existingSave = bank.Find("Save");
                if (existingSave != null)
                {
                    GameObject.Destroy(existingSave.gameObject);
                }

                // Find the Quit button to clone
                Transform quitButton = bank.Find("Quit");
                if (quitButton == null)
                {
                    Instance.LoggerInstance.Error("Could not find Quit button to clone!");
                    return;
                }

                // Simply clone the Quit button - this preserves ALL components and styling
                saveButton = GameObject.Instantiate(quitButton.gameObject, bank);
                
                // Change the name
                saveButton.name = "Save";
                
                // Update the text
                TMPro.TextMeshProUGUI[] texts = saveButton.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    text.text = "Save Game";
                    // Remove debug logging
                }
                
                // Clear all existing button click events and add our save function
                Button button = saveButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener(() => TriggerSave());
                    // Remove debug logging
                }
                
                // Position it properly below Quit
                // First, get all buttons in the bank to determine their vertical spacing
                Button[] allButtons = bank.GetComponentsInChildren<Button>();
                if (allButtons.Length > 1)
                {
                    // Get the RectTransform of our save button
                    RectTransform saveRect = saveButton.GetComponent<RectTransform>();
                    
                    // Get the RectTransform of the Quit button
                    RectTransform quitRect = quitButton.GetComponent<RectTransform>();
                    
                    // Calculate the vertical spacing between buttons
                    float verticalSpacing = 0;
                    for (int i = 0; i < allButtons.Length - 1; i++)
                    {
                        RectTransform button1 = allButtons[i].GetComponent<RectTransform>();
                        RectTransform button2 = allButtons[i+1].GetComponent<RectTransform>();
                        if (button1 && button2)
                        {
                            float spacing = button1.anchoredPosition.y - button2.anchoredPosition.y;
                            if (spacing > 0)
                            {
                                verticalSpacing = spacing;
                                break;
                            }
                        }
                    }
                    
                    // If we couldn't determine spacing, use a default value
                    if (verticalSpacing <= 0)
                    {
                        verticalSpacing = 50f; // Default spacing
                    }
                    
                    // Position the save button below the Quit button
                    saveRect.anchoredPosition = new Vector2(
                        quitRect.anchoredPosition.x,
                        quitRect.anchoredPosition.y - verticalSpacing
                    );
                    
                    // Remove debug logging
                }
                else
                {
                    // Fallback to sibling index if we can't determine proper positioning
                    int quitIndex = quitButton.GetSiblingIndex();
                    saveButton.transform.SetSiblingIndex(quitIndex + 1);
                    // Remove debug logging
                }
                
                // Make sure the button is active
                saveButton.SetActive(true);
                
                // Remove the "Save button created successfully" log message
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Failed to create save button: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        private void TriggerSave()
        {
            // Try alternative save methods first since they're working correctly
            try
            {
                TryAlternativeSaveMethods();
                return; // If successful, return early
            }
            catch (Exception)
            {
                // Completely suppress all errors from alternative save methods
                // Fall back to the original method if alternatives fail
            }
            
            // Original method as fallback
            if (saveManagerInstance != null && saveGameMethod != null)
            {
                try
                {
                    // Check if the method name contains "Error" or "Report" - if so, don't use it
                    if (saveGameMethod.Name.Contains("Error") || saveGameMethod.Name.Contains("Report"))
                    {
                        // Try to find the save manager again
                        FindSaveManager();
                        try {
                            TryAlternativeSaveMethods();
                        } catch (Exception) {
                            // Suppress all errors
                        }
                        return;
                    }
                    
                    saveGameMethod.Invoke(saveManagerInstance, null);
                    // Remove success logging
                }
                catch (Exception)
                {
                    // Suppress all errors
                    
                    // Try alternative save methods if the first one fails
                    try {
                        TryAlternativeSaveMethods();
                    } catch (Exception) {
                        // Suppress all errors
                    }
                }
            }
            else
            {
                // Try to find the save manager again
                FindSaveManager();
                
                // If found, try alternative save methods
                if (saveManagerInstance != null)
                {
                    try {
                        TryAlternativeSaveMethods();
                    } catch (Exception) {
                        // Suppress all errors
                    }
                }
            }
        }
        
        private void TryAlternativeSaveMethods()
        {
            bool saved = false;
            
            // Try direct approach with SaveInfo class first
            Type saveInfoType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Assembly-CSharp")
                {
                    saveInfoType = asm.GetType("ScheduleOne.Persistence.SaveInfo");
                    break;
                }
            }
            
            if (saveInfoType != null)
            {
                // Try to get the static SaveGame method
                MethodInfo saveGameStaticMethod = saveInfoType.GetMethod("SaveGame", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                
                if (saveGameStaticMethod != null)
                {
                    saveGameStaticMethod.Invoke(null, null);
                    // Remove success logging
                    saved = true;
                }
                else
                {
                    // Try to find any method with "Save" in the name
                    var methods = saveInfoType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        if (method.Name.Contains("Save") && !method.Name.Contains("Error") && !method.Name.Contains("Report") && 
                            method.GetParameters().Length == 0)
                        {
                            method.Invoke(null, null);
                            // Remove success logging
                            saved = true;
                            break;
                        }
                    }
                }
            }
            
            // If we haven't saved yet, try GenericSaveablesManager
            if (!saved)
            {
                // Try to find methods in the GenericSaveablesManager
                Type genericSaveablesManagerType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        genericSaveablesManagerType = asm.GetType("ScheduleOne.Persistence.GenericSaveablesManager");
                        break;
                    }
                }
                
                if (genericSaveablesManagerType != null)
                {
                    // Try to get the instance
                    object genericSaveablesManager = null;
                    PropertyInfo instanceProperty = genericSaveablesManagerType.GetProperty("Instance", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (instanceProperty != null)
                    {
                        genericSaveablesManager = instanceProperty.GetValue(null);
                    }
                    
                    if (genericSaveablesManager != null)
                    {
                        // Try to call Save method
                        MethodInfo saveMethod = genericSaveablesManagerType.GetMethod("Save", 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (saveMethod != null)
                        {
                            saveMethod.Invoke(genericSaveablesManager, null);
                            // Remove success logging
                            saved = true;
                        }
                        else
                        {
                            // Try to call SaveAll method
                            MethodInfo saveAllMethod = genericSaveablesManagerType.GetMethod("SaveAll", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (saveAllMethod != null)
                            {
                                saveAllMethod.Invoke(genericSaveablesManager, null);
                                // Remove success logging
                                saved = true;
                            }
                            else
                            {
                                // Try any method with "Save" in the name
                                var methods = genericSaveablesManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                foreach (var method in methods)
                                {
                                    if (method.Name.Contains("Save") && !method.Name.Contains("Error") && !method.Name.Contains("Report") && 
                                        method.GetParameters().Length == 0)
                                    {
                                        method.Invoke(genericSaveablesManager, null);
                                        // Remove success logging
                                        saved = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // If we still haven't saved, try BaseSaveables
            if (!saved)
            {
                // Try to find methods in the BaseSaveables class
                Type baseSaveablesType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        baseSaveablesType = asm.GetType("ScheduleOne.Persistence.BaseSaveables");
                        break;
                    }
                }
                
                if (baseSaveablesType != null)
                {
                    // Try to find a static Save method
                    MethodInfo staticSaveMethod = baseSaveablesType.GetMethod("Save", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (staticSaveMethod != null)
                    {
                        staticSaveMethod.Invoke(null, null);
                        // Remove success logging
                        saved = true;
                    }
                    else
                    {
                        // Try any method with "Save" in the name
                        var methods = baseSaveablesType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        foreach (var method in methods)
                        {
                            if (method.Name.Contains("Save") && !method.Name.Contains("Error") && !method.Name.Contains("Report") && 
                                method.GetParameters().Length == 0)
                            {
                                method.Invoke(null, null);
                                // Remove success logging
                                saved = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!saved)
            {
                throw new Exception("No save methods found");
            }
        }

        private void FindSaveManager()
        {
            try
            {
                if (saveManagerType == null) return;
                
                // Try to find the SaveManager instance
                var saveManagerComponent = GameObject.FindObjectOfType(saveManagerType);
                
                if (saveManagerComponent != null)
                {
                    saveManagerInstance = saveManagerComponent;
                    // Remove debug logging
                }
                else
                {
                    // Try to get the instance through a static property if it exists
                    PropertyInfo instanceProperty = saveManagerType.GetProperty("Instance", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (instanceProperty != null)
                    {
                        saveManagerInstance = instanceProperty.GetValue(null);
                        if (saveManagerInstance != null)
                        {
                            // Remove debug logging
                        }
                    }
                    
                    // If still not found, try to find a static field named instance or _instance
                    if (saveManagerInstance == null)
                    {
                        FieldInfo instanceField = saveManagerType.GetField("instance", 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        
                        if (instanceField == null)
                        {
                            instanceField = saveManagerType.GetField("_instance", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        }
                        
                        if (instanceField != null)
                        {
                            saveManagerInstance = instanceField.GetValue(null);
                            if (saveManagerInstance != null)
                            {
                                // Remove debug logging
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Failed to find save manager: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            if (saveManagerInstance == null && Time.time - lastSaveCheck > 5f)
            {
                FindSaveManager();
                lastSaveCheck = Time.time;
            }

            // Check for pause menu and create button if needed
            if (isInGame && saveButton == null && pauseMenuObject != null)
            {
                CreateSaveButton();
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            lastSaveCheck = 0f;
            saveManagerInstance = null;
            pauseMenuObject = null;

            // Check if we're in the main game scene (the actual game scene is called "Main")
            isInGame = sceneName == "Main";
            
            if (!isInGame && saveButton != null)
            {
                GameObject.Destroy(saveButton);
                saveButton = null;
            }

            if (isInGame)
            {
                FindSaveManager();
                FindPauseMenu(); // Look for pause menu when scene loads
            }

            // Remove the "Scene loaded: X" log message
        }

        [Obsolete]
        public override void OnApplicationStart()
        {
            try
            {
                Instance = this;
                var harmony = new HarmonyLib.Harmony("com.savemod");

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        // Look for the SaveManager class in the ScheduleOne.Persistence namespace
                        saveManagerType = asm.GetType("ScheduleOne.Persistence.SaveManager");
                        
                        if (saveManagerType == null)
                        {
                            // Fallback to the old path if not found
                            saveManagerType = asm.GetType("ScheduleOne.SaveManager");
                        }

                        if (saveManagerType != null)
                        {
                            // Remove debug logging
                            
                            var methods = saveManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                            
                            // Remove debug logging of all methods
                            
                            // Look for Save method - be more specific to avoid error methods
                            foreach (var method in methods)
                            {
                                // Skip error reporting methods
                                if (method.Name.Contains("Error") || method.Name.Contains("Report"))
                                    continue;
                                
                                // Look for methods that are exactly named "Save" or start with "Save"
                                if ((method.Name.Equals("Save") || method.Name.StartsWith("Save")) && 
                                    method.GetParameters().Length == 0)
                                {
                                    saveGameMethod = method;
                                    // Remove debug logging
                                    break;
                                }
                            }

                            // Patch any methods related to save cooldown
                            var cooldownMethods = methods.Where(m => 
                                m.Name.Contains("Cooldown") || 
                                m.Name.Contains("CanSave") || 
                                m.Name.Contains("TimeBetweenSaves"));

                            foreach (var method in cooldownMethods)
                            {
                                harmony.Patch(
                                    method,
                                    prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownPrefix),
                                        BindingFlags.Static | BindingFlags.NonPublic))
                                );
                            }

                            // Do the same for SaveInfo if it exists
                            Type saveInfoType = asm.GetType("ScheduleOne.Persistence.SaveInfo");
                            if (saveInfoType != null)
                            {
                                var saveInfoCooldownFields = saveInfoType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                    .Where(f => (f.Name.Contains("Cooldown") || f.Name.Contains("TimeBetween") || f.Name.Contains("Delay")) && 
                                               (f.FieldType == typeof(float) || f.FieldType == typeof(int))).ToList();
                                
                                foreach (var field in saveInfoCooldownFields)
                                {
                                    if (field.FieldType == typeof(float))
                                    {
                                        field.SetValue(null, 0f);
                                        // Remove debug logging
                                    }
                                    else if (field.FieldType == typeof(int))
                                    {
                                        field.SetValue(null, 0);
                                        // Remove debug logging
                                    }
                                }
                            }
                            
                            // Also patch the SavePoint class which has the SAVE_COOLDOWN constant
                            Type savePointType = asm.GetType("ScheduleOne.Persistence.SavePoint");
                            if (savePointType != null)
                            {
                                // Remove debug logging
                                
                                // Try to find and patch the SAVE_COOLDOWN constant
                                FieldInfo saveCooldownField = savePointType.GetField("SAVE_COOLDOWN", 
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                                
                                if (saveCooldownField != null)
                                {
                                    // Remove debug logging
                                    
                                    // We can't modify constants directly, so we'll patch all methods that might use it
                                    // Remove debug logging
                                    
                                    // Find all methods in SavePoint that might check cooldown time
                                    var timeCheckMethods = savePointType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                        .Where(m => m.Name.Contains("Time") || m.Name.Contains("Elapsed") || m.Name.Contains("Check") || 
                                                   m.Name.Contains("Can") || m.Name.Contains("Allow")).ToList();
                                    
                                    foreach (var method in timeCheckMethods)
                                    {
                                        // Remove debug logging
                                        
                                        // Check return type to use appropriate prefix method
                                        if (method.ReturnType == typeof(bool))
                                        {
                                            harmony.Patch(
                                                method,
                                                prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownBoolPrefix),
                                                    BindingFlags.Static | BindingFlags.NonPublic))
                                            );
                                        }
                                        else if (method.ReturnType == typeof(float))
                                        {
                                            harmony.Patch(
                                                method,
                                                prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownFloatPrefix),
                                                    BindingFlags.Static | BindingFlags.NonPublic))
                                            );
                                        }
                                    }
                                }
                                
                                // Find all methods in SavePoint that might use the cooldown
                                var savePointMethods = savePointType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                var savePointCooldownMethods = savePointMethods.Where(m => 
                                    m.Name.Contains("Cooldown") || 
                                    m.Name.Contains("CanSave") || 
                                    m.Name.Contains("TimeBetweenSaves") ||
                                    m.Name.Contains("CheckSave") ||
                                    m.Name.Contains("SaveDelay") ||
                                    m.Name.Contains("Save")).ToList();
                                
                                foreach (var method in savePointCooldownMethods)
                                {
                                    // Remove debug logging
                                    
                                    // Check return type to use appropriate prefix method
                                    if (method.ReturnType == typeof(bool))
                                    {
                                        harmony.Patch(
                                            method,
                                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownBoolPrefix),
                                                BindingFlags.Static | BindingFlags.NonPublic))
                                        );
                                    }
                                    else if (method.ReturnType == typeof(float))
                                    {
                                        harmony.Patch(
                                            method,
                                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownFloatPrefix),
                                                BindingFlags.Static | BindingFlags.NonPublic))
                                        );
                                    }
                                    else
                                    {
                                        harmony.Patch(
                                            method,
                                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SaveCooldownGenericPrefix),
                                                BindingFlags.Static | BindingFlags.NonPublic))
                                        );
                                    }
                                }
                                
                                // Find all fields in SavePoint related to cooldown
                                var savePointFields = savePointType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                    .Where(f => (f.Name.Contains("Cooldown") || f.Name.Contains("TimeBetween") || f.Name.Contains("Delay") || f.Name.Equals("SAVE_COOLDOWN")) && 
                                               (f.FieldType == typeof(float) || f.FieldType == typeof(int))).ToList();
                                
                                foreach (var field in savePointFields)
                                {
                                    if (!field.IsLiteral && !field.IsInitOnly)
                                    {
                                        if (field.FieldType == typeof(float))
                                        {
                                            field.SetValue(null, 0f);
                                            // Remove debug logging
                                        }
                                        else if (field.FieldType == typeof(int))
                                        {
                                            field.SetValue(null, 0);
                                            // Remove debug logging
                                        }
                                    }
                                    else
                                    {
                                        // Remove debug logging
                                    }
                                }
                                
                                // Create a harmony patch for any method that might use the SAVE_COOLDOWN constant
                                harmony.Patch(
                                    AccessTools.Method(savePointType, "CanSave"),
                                    prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SavePointCanSavePrefix),
                                        BindingFlags.Static | BindingFlags.NonPublic))
                                );
                            }
                        }
                        else
                        {
                            Instance.LoggerInstance.Error("Could not find SaveManager type!");
                        }

                        break;
                    }
                }

                Instance.LoggerInstance.Msg("Save Game Mod loaded successfully!");
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Failed to initialize mod: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool SaveCooldownPrefix(ref bool __result)
        {
            __result = true; // Always allow saving
            return false;
        }

        private static bool SaveCooldownBoolPrefix(ref bool __result)
        {
            // Always allow saving by returning true for CanSave methods
            __result = true;
            // Skip the original method
            return false;
        }
        
        private static bool SaveCooldownFloatPrefix(ref float __result)
        {
            // Set cooldown time to 0
            __result = 0f;
            // Skip the original method
            return false;
        }
        
        private static bool SaveCooldownGenericPrefix()
        {
            // Skip the original method for any other return type
            return false;
        }
        
        private static bool SavePointCanSavePrefix(ref bool __result)
        {
            // Always allow saving from SavePoint
            __result = true;
            return false;
        }
    }
} 