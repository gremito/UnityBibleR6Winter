using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// ネットワーク関連の処理を実装
    /// </summary>
    public class NetworkUtility
    {
        private static bool isHeadlessResult;
        private static bool isHeadlessCache = false;

        /// <summary>
        /// BatchMode 起動かどうかを調べて返します
        /// </summary>
        public static bool IsBatchModeRun
        {
            get
            {
#if ENABLE_AUTO_CLIENT
                if (isHeadlessCache)
                {
                    return isHeadlessResult;
                }
                isHeadlessResult = IsBatchMode();
                isHeadlessCache = true;
#else
                isHeadlessResult = false;
#endif
                return isHeadlessResult;
            }
        }

        /// <summary>
        /// ローカルIPアドレス取得
        /// </summary>
        public static string GetLocalIP()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            IPHostEntry ipentry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in ipentry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(ip.ToString());
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Batch Mode 確認
        /// </summary>
        private static bool IsBatchMode()
        {
            var commands = System.Environment.GetCommandLineArgs();

            foreach (var command in commands)
            {
                if (command.ToLower().Trim() == "-batchmode")
                {
                    return true;
                }
            }

            return (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
        }


        /// <summary>
        /// Headless起動 初期化
        /// </summary>
        public static void RemoveUpdateSystemForHeadlessServer()
        {
            RemoveUpdateSystem(ShouldExcludeForHeadless, true);
        }

        /// <summary>
        /// BatchMode起動 初期化
        /// </summary>
        public static void RemoveUpdateSystemForBatchBuild()
        {
            RemoveUpdateSystem(ShouldExcludeForHeadless, false);
        }

        /// <summary>
        /// 余計なプレイヤーループを削除する関数
        /// </summary>
        private static void RemoveUpdateSystem(System.Func<PlayerLoopSystem, bool> shouldExcludeFunc, bool removeAllPhysics)
        {
            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            var replaceSubSystems = new List<PlayerLoopSystem>();
            var replaceUpdateSystems = new List<PlayerLoopSystem>();

            foreach (var subsystem in currentLoop.subSystemList)
            {
                if (removeAllPhysics && subsystem.type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
                {
                    continue;
                }

                replaceUpdateSystems.Clear();
                var newSubSystem = subsystem;

                foreach (var updateSystem in subsystem.subSystemList)
                {
                    if (!shouldExcludeFunc(updateSystem))
                    {
                        replaceUpdateSystems.Add(updateSystem);
                    }
                }

                newSubSystem.subSystemList = replaceUpdateSystems.ToArray();
                replaceSubSystems.Add(newSubSystem);
            }

            currentLoop.subSystemList = replaceSubSystems.ToArray();
            PlayerLoop.SetPlayerLoop(currentLoop);
        }

        /// <summary>
        /// Headless起動 初期化
        /// </summary>
        private static bool ShouldExcludeForHeadless(PlayerLoopSystem updateSystem)
        {
            return
                // マウスイベントを削除
                (updateSystem.type == typeof(PreUpdate.SendMouseEvents)) ||
                // Inputいらない
                (updateSystem.type == typeof(PreUpdate.NewInputUpdate)) ||
                // Audioの更新もいらない
                (updateSystem.type == typeof(PostLateUpdate.UpdateAudio)) ||
                // Animationの類を消す
                (updateSystem.type == typeof(PreLateUpdate.DirectorUpdateAnimationBegin)) ||
                (updateSystem.type == typeof(PreLateUpdate.DirectorDeferredEvaluate)) ||
                (updateSystem.type == typeof(PreLateUpdate.DirectorUpdateAnimationEnd)) ||
                (updateSystem.type == typeof(Update.DirectorUpdate)) ||
                (updateSystem.type == typeof(PreLateUpdate.LegacyAnimationUpdate)) ||
                (updateSystem.type == typeof(PreLateUpdate.ConstraintManagerUpdate)) ||
                // Particleの類を消す
                (updateSystem.type == typeof(PreLateUpdate.ParticleSystemBeginUpdateAll)) ||
                (updateSystem.type == typeof(PostLateUpdate.ParticleSystemEndUpdateAll)) ||
                // Videoの類を消す
                (updateSystem.type == typeof(PostLateUpdate.UpdateVideoTextures)) ||
                (updateSystem.type == typeof(PostLateUpdate.UpdateVideo)) ||
                // Rendererの更新消す
                (updateSystem.type == typeof(PostLateUpdate.UpdateAllRenderers)) ||
                (updateSystem.type == typeof(PostLateUpdate.UpdateAllSkinnedMeshes)) ||
                // Canvasもいらない
                (updateSystem.type == typeof(PostLateUpdate.PlayerUpdateCanvases)) ||
                (updateSystem.type == typeof(PostLateUpdate.PlayerEmitCanvasGeometry)) ||
                // AI Updateもいらない
                (updateSystem.type == typeof(PreUpdate.AIUpdate)) ||
                (updateSystem.type == typeof(PreLateUpdate.AIUpdatePostScript)) ||
                false;
        }

        /// <summary>
        /// Headless 必要設定クラス
        /// </summary>
        public class RequireAtHeadless : System.Attribute
        {
        }

        /// <summary>
        /// Standalone コンポーネント全削除
        /// </summary>
        /// <param name="gmo"></param>
        public static void RemoveAllStandaloneComponents(GameObject gmo)
        {
            var allComponents = gmo.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in allComponents)
            {
                if (component is Unity.Netcode.NetworkObject ||
                    component is Unity.Netcode.NetworkBehaviour)
                {
                    continue;
                }

                var attr = component.GetType().GetCustomAttribute(typeof(RequireAtHeadless), false);
                if (attr != null)
                {
                    continue;
                }

                Object.Destroy(component);
            }
        }

    }
}