## LRCLIBee - LRCLIB for MusicBee
It's just a lyrics provider.

### Features
Using [LRCLIB API](https://lrclib.net/docs) to get lyrics. Synced lyrics are supported.
Pulling metadata from the file itself for lookup by signature for MusicBee 3.6.8922+ which is not the main release at the moment. More on that below.

### Installation
Get a release and extract all .dll files into `%APPDATA%\MusicBee\Plugins\` directory. For portable version use `Plugins` directory.

### Activation
Preferences -> Plugins -> Enable LRCLIBee.  
Preferences -> Tags (2) -> Lyrics -> LRCLIB via LRCLIBee.

### Configuration
Create lrclibee.conf in the `%APPDATA%\MusicBee\` directory (`AppData` for portable) and use this template:

    {
        "allowedDistance": 5,
        "delimiters": ["&", ";", ","],
        "verifyAlbum": false,
        "addLyricsSource": false,
        "trimTitle": false,
        "preferSyncedLyrics": false,
        "onlySyncedLyrics": false
    }

lrclibee.conf includes several options. You are allowed to use only ones you need, just omit the line and don't forget about commas in JSON.
1. Configurable title distance for minor differences. Defaults to 5. This means that a present N-character difference in search results won't affect the filtering and be considered a hit.
2. Configurable artist delimiters ("A & B, C" => "A"). Defaults to none. Useful when you have several artists for the track but Musixmatch includes only the main one.
3. Configurable album verification. Plugin will check if the album is the same. Names must be identical.
4. Configurable lyrics source marker. Plugin will append "Source: Musixmatch via Museexmatch" to the lyrics' beginning if enabled.
5. Configurable title trim. This option will remove all content in brackets from the title. By default MusicBee removes only features in the round brackets, this option will remove all content in `[]`, `{}`, `<>` and `()`.
6. Configurable synced lyrics preference. Plugin will return synced lyrics in LRC format. Advanced LRC (split by words) is not supported by MusicBee.
7. Configurable synced lyrics preference (forced). Plugin will only return synced lyrics in LRC format and pass the request to another plugin if not found. This allows to choose synced Musixmatch first and text Genius second, for example.
Restart MusicBee to apply changes.

### Logic
1. Plugin tries to get metadata from the file. If it's available, plugin will attempt to search by signature first to get the best result.
2. If no success, plugin queries the search for results with artist and title. Results (artist + title) are allowed to differ no more than `allowedDistance` characters. If there's a file and the track duration is known, it will be matched as well.
3. Plugin strips down the artist using the delimiters (if provided) and searches again.

### Notes
1. In order to benefit from signature search, MusicBee must be updated manually with a patch. Install a [release](https://www.getmusicbee.com/downloads/) and apply a [patch](https://getmusicbee.com/patches/) from MusicBee36_Patched.zip by moving and replacing everything to the installation folder.
2. Since title tag in the file will contain `(feat. Whoever)` as well, by now the plugin will copy MusicBee's approach by just removing this part if `trimTitle` is `true`. In future this will be upgraded to only remove features and leave crucial parts of the track title intact.
3. LRCLIB database contains a lot of questionable entries where track durations mismatch by a lot, like [5-6 seconds](https://lrclib.net/api/search?artist_name=$uicideboy$&track_name=Paris). The plugin will only get a match if the duration is both present and equal, if there's no duration (old MusicBee or radio) -- the first valid track will be returned and synced lyrics might be incorrectly timed. We'll see.

### Log
You can find log at `%APPDATA%\MusicBee\lrclibee.log`.

### Shoutouts
https://lrclib.net/

https://github.com/mono/taglib-sharp

https://github.com/toptensoftware/JsonKit

https://nlog-project.org/
