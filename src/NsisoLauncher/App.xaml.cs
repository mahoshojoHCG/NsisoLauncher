﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Theming;
using NsisoLauncher.Config;
using NsisoLauncher.Core.Util;
using NsisoLauncher.Views.Windows;
using NsisoLauncherCore;
using NsisoLauncherCore.Modules;
using NsisoLauncherCore.Net;
using NsisoLauncherCore.Net.Mirrors;
using NsisoLauncherCore.Net.PhalAPI;
using NsisoLauncherCore.Util;
using Environment = System.Environment;
using Version = NsisoLauncherCore.Modules.Version;

namespace NsisoLauncher
{
    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static event EventHandler<AggregateExceptionArgs> AggregateExceptionCatched;

        public static void CatchAggregateException(object sender, AggregateExceptionArgs arg)
        {
            AggregateExceptionCatched?.Invoke(sender, arg);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeApplication(e);
        }

        private void InitializeApplication(StartupEventArgs e)
        {
            #region DEBUG初始化

            //debug
            LogHandler = new LogHandler();
            AggregateExceptionCatched += (a, b) => LogHandler.AppendFatal(b.AggregateException);
            if (e.Args.Contains("-debug"))
            {
                var debugWindow = new DebugWindow();
                debugWindow.Show();
                LogHandler.OnLog += (s, log) => debugWindow?.AppendLog(s, log);
            }

            #endregion

            Config = new ConfigHandler();

            #region DEBUG初始化（基于配置文件）

            if (Config.MainConfig.Launcher.Debug && !e.Args.Contains("-debug"))
            {
                var debugWindow = new DebugWindow();
                debugWindow.Show();
                LogHandler.OnLog += (s, log) => debugWindow?.AppendLog(s, log);
            }

            LogHandler.WriteToFile = Config.MainConfig.Launcher.WriteLog;

            #endregion

            #region 自定义主题初始化

            //todo 恢复自定义主题初始化
            var custom = Config.MainConfig.Customize;
            if (!string.IsNullOrWhiteSpace(custom.AppTheme))
            {
                LogHandler.AppendInfo("自定义->更改主题:" + custom.AppTheme);
                var theme = ThemeManager.Current.GetTheme(custom.AppTheme) ??
                            ThemeManager.Current.GetTheme("Light.Blue") ??
                            ThemeManager.Current.Themes.First();
                ThemeManager.Current.ChangeTheme(this, theme);
            }

            #endregion

            #region Nsiso反馈API初始化

#if DEBUG
            NsisoAPIHandler = new APIHandler(true);
#else
            NsisoAPIHandler = new NsisoLauncherCore.Net.PhalAPI.APIHandler(Config.MainConfig.Launcher.NoTracking);
#endif

            #endregion

            #region 数据初始化

            var env = Config.MainConfig.Environment;

            JavaList = Java.GetJavaList();

            //设置版本路径
            string gameroot = null;
            switch (env.GamePathType)
            {
                case GameDirEnum.ROOT:
                    gameroot = Path.GetFullPath(".minecraft");
                    break;
                case GameDirEnum.APPDATA:
                    gameroot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft";
                    break;
                case GameDirEnum.PROGRAMFILES:
                    gameroot = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\.minecraft";
                    break;
                case GameDirEnum.CUSTOM:
                    gameroot = env.GamePath + "\\.minecraft";
                    break;
                default:
                    throw new ArgumentException("判断游戏目录类型时出现异常，请检查配置文件中GamePathType节点");
            }

            LogHandler.AppendInfo("核心初始化->游戏根目录(默认则为空):" + gameroot);

            //设置JAVA
            Java java = null;
            if (env.AutoJava)
            {
                java = Java.GetSuitableJava(JavaList);
            }
            else
            {
                java = JavaList.Find(x => x.Path == env.JavaPath);
                if (java == null) java = Java.GetJavaInfo(env.JavaPath);
            }

            if (java != null)
            {
                env.JavaPath = java.Path;
                LogHandler.AppendInfo("核心初始化->Java路径:" + java.Path);
                LogHandler.AppendInfo("核心初始化->Java版本:" + java.Version);
                LogHandler.AppendInfo("核心初始化->Java位数:" + java.Arch);
            }
            else
            {
                LogHandler.AppendWarn("核心初始化失败，当前电脑未匹配到JAVA");
            }

            //设置版本独立
            var verIso = Config.MainConfig.Environment.VersionIsolation;

            #endregion

            #region 启动核心初始化

            Handler = new LaunchHandler(gameroot, java, verIso);
            Handler.GameLog += (s, log) => LogHandler.AppendLog(s, new Log {LogLevel = LogLevel.GAME, Message = log});
            Handler.LaunchLog += (s, log) => LogHandler.AppendLog(s, log);

            #endregion

            #region 下载核心初始化

            ServicePointManager.DefaultConnectionLimit = 10;

            var downloadCfg = Config.MainConfig.Download;
            Downloader = new MultiThreadDownloader();
            if (!string.IsNullOrWhiteSpace(downloadCfg.DownloadProxyAddress))
            {
                var proxy = new WebProxy(downloadCfg.DownloadProxyAddress, downloadCfg.DownloadProxyPort);
                if (!string.IsNullOrWhiteSpace(downloadCfg.ProxyUserName))
                {
                    var credential = new NetworkCredential(downloadCfg.ProxyUserName, downloadCfg.ProxyUserPassword);
                    proxy.Credentials = credential;
                }

                Downloader.Proxy = proxy;
            }

            if (Downloader.MirrorList == null) Downloader.MirrorList = new List<IMirror>();
            switch (Config.MainConfig.Download.DownloadSource)
            {
                case DownloadSource.Auto:
                    Downloader.MirrorList.Add(new McbbsMirror());
                    Downloader.MirrorList.Add(new BmclMirror());
                    break;
                case DownloadSource.Mojang:
                    break;
                case DownloadSource.BMCLAPI:
                    Downloader.MirrorList.Add(new BmclMirror());
                    break;
                case DownloadSource.MCBBS:
                    Downloader.MirrorList.Add(new McbbsMirror());
                    break;
            }

            Downloader.ProcessorSize = Config.MainConfig.Download.DownloadThreadsSize;
            Downloader.CheckFileHash = Config.MainConfig.Download.CheckDownloadFileHash;
            Downloader.DownloadLog += (s, log) => LogHandler?.AppendLog(s, log);

            #endregion

            var mainwindow = new MainWindow();
            MainWindow = mainwindow;
            mainwindow.Show();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogHandler.AppendFatal(e.Exception);
            e.Handled = true;
        }

        public static string GetResourceString(string key)
        {
            return (string) Current.FindResource(key);
        }

        /// <summary>
        ///     重启启动器
        /// </summary>
        /// <param name="admin">是否用管理员模式重启</param>
        public static void Reboot(bool admin)
        {
            var info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
            var args = Environment.GetCommandLineArgs();
            foreach (var item in args) info.Arguments += item + ' ';
            if (admin) info.Verb = "runas";
            info.Arguments += "-reboot";
            Process.Start(info);
            Current.Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                Config.Save();
            }
            catch (Exception)
            {
            }
        }

        public static async Task RefreshVersionListAsync()
        {
            if (VersionList == null) VersionList = new ObservableCollection<Version>();
            var list = await Handler.GetVersionsAsync();
            VersionList.Clear();
            foreach (var item in list) VersionList.Add(item);
        }

        public static void RefreshVersionList()
        {
            if (VersionList == null) VersionList = new ObservableCollection<Version>();
            var list = Handler.GetVersions();
            VersionList.Clear();
            foreach (var item in list) VersionList.Add(item);
        }

        #region 全局属性

        /// <summary>
        ///     启动主模块
        /// </summary>
        public static LaunchHandler Handler { get; private set; }

        /// <summary>
        ///     应用配置文件
        /// </summary>
        public static ConfigHandler Config { get; private set; }

        /// <summary>
        ///     下载核心
        /// </summary>
        public static MultiThreadDownloader Downloader { get; private set; }

        /// <summary>
        ///     日志处理器
        /// </summary>
        public static LogHandler LogHandler { get; private set; }

        /// <summary>
        ///     应用API
        /// </summary>
        public static APIHandler NsisoAPIHandler { get; private set; }

        #region 全局数据属性

        /// <summary>
        ///     JAVA本机列表
        /// </summary>
        public static List<Java> JavaList { get; private set; }

        public static ObservableCollection<Version> VersionList { get; private set; }

        #endregion

        #endregion
    }

    //定义异常参数处理
    public class AggregateExceptionArgs : EventArgs
    {
        public AggregateException AggregateException { get; set; }
    }
}