using LRCLIBee;
using NLog;
using System;
using System.Configuration.Provider;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private static Logger Logger;

        public static string configFile;
        public static string logFile;
        public static string name = "LRCLIBee";

        private MusicBeeApiInterface musicBee;
        private PluginInfo info = new PluginInfo();
        private LRCLIBClient musixmatchClient;

        public PluginInfo Initialise(IntPtr apiPtr)
        {
            musicBee = new MusicBeeApiInterface();
            musicBee.Initialise(apiPtr);

            configFile = Path.Combine(musicBee.Setting_GetPersistentStoragePath(), @"lrclibee.conf");
            logFile = Path.Combine(musicBee.Setting_GetPersistentStoragePath(), @"lrclibee.log");

            info.PluginInfoVersion = PluginInfoVersion;
            info.Name = name;

            info.VersionMajor = 1;
            info.VersionMinor = 0;
            info.Revision = 1;

            info.Description = $"LRCLIB support for MusicBee [{info.VersionMajor}.{info.VersionMinor}.{info.Revision}]";
            info.Author = "slonopot";
            info.TargetApplication = "MusicBee";
            info.Type = PluginType.LyricsRetrieval;

            info.MinInterfaceVersion = 20;
            info.MinApiRevision = 25;
            info.ReceiveNotifications = ReceiveNotificationFlags.StartupOnly;
            info.ConfigurationPanelHeight = 20;

            try
            {
                var target = new NLog.Targets.FileTarget(name)
                {
                    FileName = logFile,
                    Layout = "${date} | ${level} | ${callsite} | ${message}",
                    DeleteOldFileOnStartup = true,
                    Name = name
                };
                if (LogManager.Configuration == null)
                {
                    var config = new NLog.Config.LoggingConfiguration();
                    config.AddTarget(target);
                    config.AddRuleForAllLevels(target, name);
                    LogManager.Configuration = config;

                }
                else
                {
                    LogManager.Configuration.AddTarget(target);
                    LogManager.Configuration.AddRuleForAllLevels(target, name);
                }

                LogManager.ReconfigExistingLoggers();

                Logger = LogManager.GetLogger(name);

                musixmatchClient = new LRCLIBClient(LRCLIBeeLyricsProvider);
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred during LRCLIBee startup: " + e.Message);
                throw;
            }

            return info;
        }

        private string LRCLIBeeLyricsProvider = "LRCLIB via LRCLIBee";

        public String[] GetProviders()
        {
            return new string[] { LRCLIBeeLyricsProvider };
        }

        private (string, string, string, int) TryGetFileMetadata(String source)
        {
            var tfile = TagLib.File.Create(source);
            string title = tfile.Tag.Title;
            string artist = String.Join(" & ", tfile.Tag.AlbumArtists);
            string album = tfile.Tag.Album;
            int duration = (int)tfile.Properties.Duration.TotalSeconds;
            Logger.Debug("Extracted metadata from {source}: artist={artist}, title={title}, album={album}, duration={duration}", source, artist, title, album, duration);
            return (artist, title, album, duration);
        }

        public String RetrieveLyrics(String source, String artist, String title, String album, bool preferSynced, String providerName)
        {
            Logger.Debug("source={source}, artist={artist}, title={title}, album={album}, preferSynced={preferSynced}, providerName={providerName}", source, artist, title, album, preferSynced, providerName);

            if (providerName != LRCLIBeeLyricsProvider) return null;

            int duration = 0;

            if (source != string.Empty)
            {
                try { (artist, title, album, duration) = TryGetFileMetadata(source); }
                catch { Logger.Debug("Failed to extract metadata from {source}", source); }
            }

            var lyrics = musixmatchClient.getLyrics(artist, title, album, duration);
            return lyrics;
        }

        public void ReceiveNotification(String source, NotificationType type) { }
        public void SaveSettings() { }

        public bool Configure(IntPtr panelHandle) { return false; } //fixes the popup
        
        //public bool Configure(IntPtr panelHandle) {
        //    string dataPath = musicBee.Setting_GetPersistentStoragePath();
        //    // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
        //    // keep in mind the panel width is scaled according to the font the user has selected
        //    // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
        //    if (panelHandle != IntPtr.Zero)
        //    {
        //        Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
        //        configPanel.Size = new Size(300, 300);
        //        Label prompt = new Label();
        //        prompt.AutoSize = true;
        //        prompt.Location = new Point(0, 0);
        //        prompt.Text = "prompt:";
        //        TextBox textBox = new TextBox();
        //        textBox.Bounds = new Rectangle(100, 0, 300, textBox.Height);
        //        configPanel.Controls.AddRange(new Control[] { prompt, textBox });
        //    }
        //    return false;
        //}
        public void Uninstall() { MessageBox.Show("Just delete the plugin files from the Plugins folder yourself, this plugin is not very sophisticated to handle it itself."); }

    }
}
