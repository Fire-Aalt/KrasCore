using System.Collections.Generic;
using KrasCore.Essentials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KrasCore.Editor
{
    public static class SceneManagementDropdown
    {
        private const string Path = "KrasCore/Scene Management";
        private static readonly string Name = StringUtils.RemoveAllWhitespace(Path);
        
        static SceneManagementDropdown()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += (_, _) => Refresh();
            EditorSceneManager.sceneClosed += _ => Refresh();
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingPlayMode:
                    Refresh();
                    break;
            }
        }

        [MainToolbarElement(Path, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement SceneManagement()
        {
            GetActiveSceneGroup(out var text);
            var content = new MainToolbarContent(text, MainToolbarUtils.GetEditorIcon("UnityLogo"), string.Empty);
            var element = new MainToolbarDropdown(content, ShowDropdownMenu) { enabled = !EditorApplication.isPlayingOrWillChangePlaymode };
            return element;
        }
        
        private static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            AddBootloaderOption(menu);
            
            var selected = GetActiveSceneGroup(out _);
            foreach (var sceneGroup in ScenesDataSO.Instance.sceneGroups)
            {
                var isOn = selected == sceneGroup;
                
                menu.AddItem(new GUIContent(sceneGroup.GroupName), isOn, () =>
                {
                    OpenSceneGroup(sceneGroup);
                });
            }
            menu.DropDown(dropDownRect);
        }

        private static SceneGroup GetActiveSceneGroup(out string displayName)
        {
            var data = ScenesDataSO.Instance;
            var openScenes = GetOpenScenes();
            foreach (var sceneGroup in data.sceneGroups)
            {
                var active = true;
                foreach (var scene in openScenes)
                {
                    if (!SceneInGroupOrBootloader(sceneGroup, scene))
                    {
                        active = false;
                        break;
                    }
                }
                
                if (active && !OnlyBootloader(data))
                {
                    displayName = sceneGroup.GroupName;
                    return sceneGroup;
                }
            }
            
            displayName = OnlyBootloader(data) ? "_Bootloader" : "None";
            return null;
        }

        private static void AddBootloaderOption(GenericMenu menu)
        {
            var data = ScenesDataSO.Instance;
            var isBootloaderOn = data.bootLoaderScene.LoadedScene.IsValid();

            menu.AddItem(new GUIContent("_Bootloader"), isBootloaderOn, () =>
            {
                if (isBootloaderOn)
                {
                    if (SceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.CloseScene(data.bootLoaderScene.LoadedScene, true);
                    }
                }
                else
                {
                    var scene = EditorSceneManager.OpenScene(data.bootLoaderScene.Path, OpenSceneMode.Additive);
                    if (SceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.MoveSceneBefore(scene, SceneManager.GetSceneAt(0));
                    }
                }
            });
        }

        private static void OpenSceneGroup(SceneGroup sceneGroup)
        {
            var correctScenes = new List<string>();
            var scenesToRemove = new List<Scene>();
                    
            foreach (var scene in GetOpenScenes())
            {
                if (!SceneInGroupOrBootloader(sceneGroup, scene))
                {
                    scenesToRemove.Add(scene);
                }
                else
                {
                    correctScenes.Add(scene.path);
                }
            }
                    
            foreach (var sceneData in sceneGroup.Scenes)
            {
                if (!correctScenes.Contains(sceneData.Reference.Path))
                {
                    EditorSceneManager.OpenScene(sceneData.Reference.Path, OpenSceneMode.Additive);
                }

                if (sceneData.Reference == sceneGroup.MainScene.Reference)
                {
                    SceneManager.SetActiveScene(sceneData.Reference.LoadedScene);
                }
            }
            foreach (var scene in scenesToRemove)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Scene[] GetOpenScenes()
        {
            var openScenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                openScenes[i] = SceneManager.GetSceneAt(i);
            }

            return openScenes;
        }
        
        private static bool OnlyBootloader(ScenesDataSO data)
        {
            return data.bootLoaderScene.LoadedScene.IsValid() && SceneManager.sceneCount == 1;
        }
        
        private static bool SceneInGroupOrBootloader(SceneGroup sceneGroup, Scene scene)
        {
            return sceneGroup.IsSceneInGroup(scene) || scene.path == ScenesDataSO.Instance.bootLoaderScene.Path;
        }
        
        private static void Refresh()
        {
            EditorApplication.delayCall += () => MainToolbar.Refresh(Path);
        }
    }
}