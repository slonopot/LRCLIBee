using MusicBeePlugin;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Topten.JsonKit;

namespace LRCLIBee
{
    public class LRCLIBClient
    {
        private static Logger Logger = LogManager.GetLogger(Plugin.name);

        private HttpClient client = new HttpClient();

        private string LyricsProviderName;

        private string ApiURL = "https://lrclib.net/api/";

        private int AllowedDistance = 5; //a number of edits needed to get from one title to another
        private char[] Delimiters = { }; //delimiters to remove additional authors from the string
        private bool VerifyAlbum = false;
        private bool AddLyricsSource = false;
        private bool TrimTitle = false;
        private bool PreferSyncedLyrics = true;
        private bool OnlySyncedLyrics = false;
        public LRCLIBClient(string lyricsProviderName = null)
        {
            LyricsProviderName = lyricsProviderName;

            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "LRCLIBee");
  
            if (File.Exists(Plugin.configFile))
            {
                string data = File.ReadAllText(Plugin.configFile);
                dynamic config = Json.Parse<object>(data);
                if (Util.PropertyExists(config, "allowedDistance"))
                    AllowedDistance = (int)config.allowedDistance;
                if (Util.PropertyExists(config, "delimiters"))
                    Delimiters = ((List<object>)config.delimiters).Select(x => char.Parse(x.ToString())).ToArray();
                if (Util.PropertyExists(config, "verifyAlbum"))
                    VerifyAlbum = (bool)config.verifyAlbum;
                if (Util.PropertyExists(config, "addLyricsSource"))
                    AddLyricsSource = (bool)config.addLyricsSource;
                if (Util.PropertyExists(config, "trimTitle"))
                    TrimTitle = (bool)config.trimTitle;
                if (Util.PropertyExists(config, "preferSyncedLyrics"))
                    PreferSyncedLyrics = (bool)config.preferSyncedLyrics;
                if (Util.PropertyExists(config, "onlySyncedLyrics"))
                    OnlySyncedLyrics = (bool)config.onlySyncedLyrics;

                if (Util.PropertyExists(config, "apiURL"))
                    ApiURL = config.apiURL;

                Logger.Info("Configuration file was used: allowedDistance={allowedDistance}, delimiters={delimiters}, verifyAlbum={verifyAlbum}, addLyricsSource={addLyricsSource}, trimTitle={trimTitle}, preferSyncedLyrics={preferSyncedLyrics}, onlySyncedLyrics={onlySyncedLyrics}", AllowedDistance, Delimiters, VerifyAlbum, AddLyricsSource, TrimTitle, PreferSyncedLyrics, OnlySyncedLyrics);
            }
            else { Logger.Info("No configuration file was provided, defaults were used"); }
        }

        private dynamic LRCLIBRequest(string path, NameValueCollection parameters = null)
        {
            string url = this.ApiURL + path;
            if (parameters == null)
                parameters = new NameValueCollection();

            url += "?" + Util.ToQueryString(parameters);
            HttpResponseMessage response = null;
            try
            {
                var task = Task.Run(() => client.GetAsync(url));
                task.Wait();
                response = task.Result;
            }
            catch (Exception)
            {
                throw;
            }
            dynamic result = null;
            try
            {
                string content = string.Empty;
                var task = Task.Run(() => response.Content.ReadAsStringAsync());
                task.Wait();
                content = task.Result;
                result = Json.Parse<object>(content);
            }
            catch { throw; }
            return result;
        }

        private dynamic bySignature(string artist, string title, string album, int duration)
        {
            var parameters = new NameValueCollection();
            parameters.Add("track_name", title);
            parameters.Add("artist_name", artist);
            parameters.Add("album_name", album);
            parameters.Add("duration", duration.ToString());

            dynamic response = this.LRCLIBRequest("get", parameters);

            if (Util.PropertyExists(response, "statusCode")) return null;
            return response;
        }

        private dynamic search(string artist, string title, string album = null)
        {
            var parameters = new NameValueCollection();
            parameters.Add("track_name", title);
            parameters.Add("artist_name", artist);
            if (!string.IsNullOrEmpty(album)) parameters.Add("album_name", album);

            dynamic response = this.LRCLIBRequest("search", parameters);

            return response;
        }

        public string getLyrics(string artist, string title, string album, int duration = 0)
        {
            artist = artist.Trim();
            title = title.Trim();
            album = album.Trim();

            if (TrimTitle) { title = Util.Trim(title); }

            Logger.Info("Attempting to look for {aritst} - {title} ({album}) [{duration} s.]", artist, title, album, duration);

            dynamic match = null;

            if (duration != 0) {
                Logger.Info("Trying to find by signature");
                match = null; //bySignature(artist, title, album, duration);
                if (match == null) Logger.Info("Not found");
            }
            
            if (match == null)
            {
                Logger.Info("Trying to search");
                match = findInMatches(search(artist, title), artist, title, album, duration);
            }

            if (match == null && Delimiters.Length > 0)
            {
                var editedArtist = artist;

                foreach (char delimiter in Delimiters) editedArtist = editedArtist.Split(delimiter)[0].Trim();

                if (editedArtist != artist)
                {
                    Logger.Info("Attempting to search for {aritst} - {title} ({album}) [{duration} s.]", editedArtist, title, album, duration);

                    match = findInMatches(search(editedArtist, title), artist, title, album, duration);
                }
            }

            if (match == null) { 
                Logger.Info("Nothing found at all");
                return null;
            } else Logger.Info("Got a hit");

            string result = null;

            if ((PreferSyncedLyrics || OnlySyncedLyrics) && match.syncedLyrics != null)
                result = match.syncedLyrics;
           
            if (!OnlySyncedLyrics)
                result = match.plainLyrics;

            if (result == null)
                Logger.Info("Match was found but no lyrics");

            if (AddLyricsSource)
                result = $"Source: {LyricsProviderName}\n\n" + result;
            
            return result;
        }

        private dynamic findInMatches(dynamic matches, string artist, string title, string album, int duration = 0)
        {
            foreach (var match in matches)
            {
                if (VerifyAlbum && match.albumName.ToLower() != album.ToLower()) continue;

                if (Util.ValidateResult(artist, title, match.artistName, match.trackName, AllowedDistance))
                    if (duration == 0 || match.duration == duration)
                        return match;
                    else Logger.Info("Mismatching durations: {duration} and {resultduration}", duration, match.duration);
            }
         
            Logger.Info("No results for this search");

            return null;
        }
    }
}
