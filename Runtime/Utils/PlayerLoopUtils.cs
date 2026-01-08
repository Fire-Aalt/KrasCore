using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace KrasCore
{
    public static class PlayerLoopUtils
    {
        /// <summary>
        /// Remove a system from the player loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loop"></param>
        /// <param name="systemToRemove"></param>
        public static void RemoveSystem<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToRemove)
        {
            if (loop.subSystemList == null) return;

            var playerLoopSystemList = new List<PlayerLoopSystem>(loop.subSystemList);
            for (int i = 0; i < playerLoopSystemList.Count; ++i)
            {
                if (playerLoopSystemList[i].type == systemToRemove.type && playerLoopSystemList[i].updateDelegate == systemToRemove.updateDelegate)
                {
                    playerLoopSystemList.RemoveAt(i);
                    loop.subSystemList = playerLoopSystemList.ToArray();
                }
            }

            HandleSubSystemLoopForRemoval<T>(ref loop, systemToRemove);
        }

        static void HandleSubSystemLoopForRemoval<T>(ref PlayerLoopSystem loop, PlayerLoopSystem systemToRemove)
        {
            if (loop.subSystemList == null) return;

            for (int i = 0; i < loop.subSystemList.Length; ++i)
            {
                RemoveSystem<T>(ref loop.subSystemList[i], systemToRemove);
            }
        }

        /// <summary>
        /// Insert a system into the player loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loop"></param>
        /// <param name="systemToInsert"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool InsertSystem<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToInsert, int index)
        {
            if (loop.type != typeof(T)) return HandleSubSystemLoop<T>(ref loop, systemToInsert, index);

            var playerLoopSystemList = new List<PlayerLoopSystem>();
            if (loop.subSystemList != null) playerLoopSystemList.AddRange(loop.subSystemList);
            playerLoopSystemList.Insert(index, systemToInsert);
            loop.subSystemList = playerLoopSystemList.ToArray();
            return true;
        }

        public static bool AddSystem<TTiming>(PlayerLoopSystem system, Action clearAction)
        {
            var currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            if (!InsertSystem<TTiming>(ref currentPlayerLoop, system, 0))
            {
                Debug.LogWarning($"{system.type} not initialized, unable to register {system.type} into the Update loop.");
                return false;
            }
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateUpdateLoop;
            EditorApplication.playModeStateChanged += OnPlayModeStateUpdateLoop;

            void OnPlayModeStateUpdateLoop(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    RemoveSystem<TTiming>(ref playerLoop, system);
                    PlayerLoop.SetPlayerLoop(playerLoop);
                    
                    clearAction?.Invoke();
                }
            }
#endif
            return true;
        }

        static bool HandleSubSystemLoop<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToInsert, int index)
        {
            if (loop.subSystemList == null) return false;

            for (int i = 0; i < loop.subSystemList.Length; ++i)
            {
                if (!InsertSystem<T>(ref loop.subSystemList[i], in systemToInsert, index)) continue;
                return true;
            }

            return false;
        }

        public static void PrintPlayerLoop(PlayerLoopSystem loop)
        {
            StringBuilder sb = new();
            sb.AppendLine("Unity Player Loop");
            foreach (PlayerLoopSystem subSystem in loop.subSystemList)
            {
                PrintSubsystem(subSystem, sb, 0);
            }
            Debug.Log(sb.ToString());
        }

        static void PrintSubsystem(PlayerLoopSystem system, StringBuilder sb, int level)
        {
            sb.Append(' ', level * 2).AppendLine(system.type.ToString());
            if (system.subSystemList == null || system.subSystemList.Length == 0) return;

            foreach (PlayerLoopSystem subSystem in system.subSystemList)
            {
                PrintSubsystem(subSystem, sb, level + 1);
            }
        }
    }
}
