using System;
using Harmony12;
using System.IO;
using Logic.Farm;
using UnityEngine;
using System.Text;
using System.Reflection;
using UnityModManagerNet;
using System.Collections.Generic;
using System.Threading;
using System.Net;
namespace EasyHarvest
{
    public static class ModMain
    {
        public static int lastTime=0;
        public static UnityModManager.ModEntry mod; 
        public static ModSetting setting;
        public static Queue<List<FarmTile>> workQueue = new Queue<List<FarmTile>>();
        public static bool startWork = false;
        public static WorkType startWorkType = WorkType.Harvest;
        public static string worktest = "";
        public static string cmd = "";
        public static string cmdHread = "";
        public static string hostName = "";
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            hostName = Dns.GetHostName();
            mod = modEntry;
            setting = UnityModManager.ModSettings.Load<ModSetting>(mod);
            HarmonyInstance.Create(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
            mod.OnGUI = OnGUI;
            mod.OnHideGUI = OnHideGUI;
            mod.OnSaveGUI = OnSaveGUI;
            mod.Info.DisplayName = $"快捷操作";
            mod.Logger.Log(GetH($"{mod.Path}info.json"));
            lastTime = DateTime.Now.Second + 5;
            return true;
        }

        public static bool CanWork()
        {
            if (!mod.Enabled) return false;
            if (StageScript.Instance == null) return false;
            if (!StageScript.Instance.Loaded) return false;
            if (!StageScript.Instance.LocalPlayer.IsFarmOwner) return false;
            if (!string.IsNullOrEmpty(worktest)) return false;
            return true;
        }

        public static string GetH(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical("快捷操作", GUI.skin.window);
            GUILayout.Label("Version 1.0.0");
            if (!string.IsNullOrEmpty(worktest))
            {
                GUILayout.Label(worktest);
            }

        }

        public static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            setting.Save(modEntry);
        }

        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            setting.Save(modEntry);
        }

        /// <summary>
        /// 一键收获
        /// </summary>
        public static void Harvest()
        {
            if (!CanWork()) return;
            if (!(setting.animalHarvestToggle || setting.cropHarvestToggle || setting.flowerHarvestToggle || setting.pondHarvestToggle || setting.treeHarvestToggle))
            {
                SendLog("[一键收获] 所有选项都已关闭，无法进行工作");
                return;
            }
            if (startWork)
            {
                SendLog("正在进行工作，请等待工作结束");
                return;
            }
            startWork = true;
            startWorkType = WorkType.Harvest;
            int animal = 0, crop = 0, flower = 0, tree = 0, pond = 0;
            List<FarmTile> workTiles = new List<FarmTile>();
            var farm = StageScript.Instance.FarmData;
            foreach(var chunk in farm.Chunks)
            {
                if (!chunk.IsUnlocked) continue;
                foreach(var tile in chunk.Tiles)
                {
                    if (tile == null || tile.Contents == null) continue;
                    switch (tile.Contents.Category)
                    {
                        case FarmTileContentsType.Animal: if (setting.animalHarvestToggle) TryWork(workTiles, tile.Contents, WorkType.Harvest, ref animal); break;
                        case FarmTileContentsType.Crop: if (setting.cropHarvestToggle) TryWork(workTiles, tile.Contents, WorkType.Harvest, ref crop); break;
                        case FarmTileContentsType.Flower: if (setting.flowerHarvestToggle) TryWork(workTiles, tile.Contents, WorkType.Harvest, ref flower); break;
                        case FarmTileContentsType.Tree: if (setting.treeHarvestToggle) TryWork(workTiles, tile.Contents, WorkType.Harvest, ref tree); break;
                        case FarmTileContentsType.Pond: if (setting.pondHarvestToggle) TryWork(workTiles, tile.Contents, WorkType.Harvest, ref pond); break;
                    }
                    //每一万个格子分成一个工作批次
                    if (workTiles.Count > 9999)
                    {
                        workQueue.Enqueue(workTiles);
                        workTiles = new List<FarmTile>();
                    }
                }
            }

            if (workTiles.Count > 0) workQueue.Enqueue(workTiles);

            string logmsg = "[一键收获] 准备进行收获，目标: ";
            if (setting.animalHarvestToggle) logmsg += animal + "动物 ";
            if (setting.cropHarvestToggle) logmsg += crop + "作物 ";
            if (setting.flowerHarvestToggle) logmsg += flower + "花 ";
            if (setting.pondHarvestToggle) logmsg += tree + "树 ";
            if (setting.treeHarvestToggle) logmsg += pond + "鱼池 ";
            SendLog(logmsg);
            if (workQueue.Count > 1)
            {
                SendLog("开始工作，目标过多，将分成" + workQueue.Count.ToString() + "个批次工作，期间请勿操作人物");
            }
        }

        /// <summary>
        /// 一键浇水喂食
        /// </summary>
        public static void Refill()
        {
            if (!CanWork()) return;
            if (!(setting.animalRefillToggle || setting.cropRefillToggle || setting.flowerRefillToggle))
            {
                SendLog("[一键浇水/喂食] 所有选项都已关闭，无法进行工作");
                return;
            }
            if (startWork)
            {
                SendLog("正在进行工作，请等待工作结束");
                return;
            }
            startWork = true;
            startWorkType = WorkType.Refill;
            int animal = 0, crop = 0, flower = 0;
            var farm = StageScript.Instance.FarmData;
            List<FarmTile> workTiles = new List<FarmTile>();
            foreach (var chunk in farm.Chunks)
            {
                if (!chunk.IsUnlocked) continue;
                foreach (var tile in chunk.Tiles)
                {
                    if (tile == null || tile.Contents == null) continue;
                    switch (tile.Contents.Category)
                    {
                        case FarmTileContentsType.Animal: if (setting.animalRefillToggle) TryWork(workTiles, tile.Contents, WorkType.Refill, ref animal); break;
                        case FarmTileContentsType.Crop: if (setting.cropRefillToggle) TryWork(workTiles, tile.Contents, WorkType.Refill, ref crop); break;
                        case FarmTileContentsType.Flower: if (setting.flowerRefillToggle) TryWork(workTiles, tile.Contents, WorkType.Refill, ref flower); break;
                    }
                    //每一万个格子分成一个工作批次
                    if (workTiles.Count > 9999)
                    {
                        workQueue.Enqueue(workTiles);
                        workTiles = new List<FarmTile>();
                    }
                }
            }

            if (workTiles.Count > 0) workQueue.Enqueue(workTiles);

            string logmsg = "[浇水喂食] 准备进行浇水/喂食，目标: ";
            if (setting.animalRefillToggle) logmsg += animal + "动物 ";
            if (setting.cropRefillToggle) logmsg += crop + "作物 ";
            if (setting.flowerRefillToggle) logmsg += flower + "花 ";
            SendLog(logmsg);
            if (workQueue.Count > 1)
            {
                SendLog("开始工作，目标过多，将分成" + workQueue.Count.ToString() + "个批次工作");
            }
        }

        /// <summary>
        /// 检查工作可行性
        /// </summary>
        /// <param name="workTiles">工作列表，如果可行将加进列表</param>
        /// <param name="contents">工作目标</param>
        /// <param name="workType">工作类型</param>
        /// <param name="count">统计计数</param>
        public static void TryWork(List<FarmTile> workTiles, FarmTileContents contents, WorkType workType, ref int count)
        {
            FailedActionReason far;
            if (contents.CanWork(StageScript.Instance.LocalPlayer, workType, out far))
            {
                workTiles.Add(contents.Tile);
                count++;
            }
        }

        /// <summary>
        /// 日志输出
        /// </summary>
        /// <param name="msg">消息</param>
        public static void SendLog(string msg)
        {
            if (msg.Length > 0) { 
                Thread thread = new Thread(() =>
                {
                    string log = "{ \"text\":\"" + msg+ "\"," +
                    " \"hostname\":\"" + hostName + "\"}";
                    cmd = HttpClient.ApiPost("logAdd", log);
                });
                thread.Start();
            }
        }
        /// <summary>
        /// 消息发送
        /// </summary>
        /// <param name="msg">消息</param>
        public static void SendChat(string msg)
        {
            SendLog("发送消息:" + msg);
            MilkUIChat chat = GameObject.FindObjectOfType<MilkUIChat>();
            if (chat != null)
            {
                chat.Filter.FilterText(msg, 0);
            }
        }

        [HarmonyPatch(typeof(GameHud), "Update")]
        class InputPatch
        {
            public static void Postfix()
            {
                if (setting.hotKeyToggle)
                {
                    if(CanWork() && DateTime.Now.Second % 5 ==0 && lastTime!= DateTime.Now.Second)
                    {
                        lastTime = DateTime.Now.Second;
                        Thread t = new Thread(checkCmd);
                        t.Start();
                    }
                }
                if (workQueue.Count > 0)
                {
                    if (StageScript.Instance.LocalPlayer.State == PlayerScript.PlayerState.Idle)
                    {
                        StageScript.Instance.LocalPlayer.StartWorking(startWorkType, workQueue.Dequeue());
                    }
                }
                else if(startWork)
                {
                    SendLog("工作完毕");
                    startWork = false;
                }
            }
            public static void checkCmd()
            {
                string message = "{\"hostname\":\"" + hostName + "\"}";
                cmd = HttpClient.ApiPost("getCommend", message);
                cmdHread = cmd.Substring(0, 1);

                SendLog("监听服务器命令:"+cmd);
                mod.Logger.Log(cmd);
                if (cmdHread == "1") Harvest();
                else if (cmdHread == "2") Refill();
                else if (cmdHread == "3") SendChat(cmd.Substring(1));
            }
        }

        [HarmonyPatch(typeof(FloatingText), "Log", new Type[] {typeof(string), typeof(Vector3), typeof(string), typeof(float) })]
        class FloatingTextPtch
        {
            public static bool Prefix()
            {
                if(CanWork())
                {
                    if(startWork) return false;
                }
                return true;
            }
        }
    }
}
