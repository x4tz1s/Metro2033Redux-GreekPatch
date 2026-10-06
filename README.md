# Metro 2033 Redux — Ελληνική μετάφραση (πρόγραμμα εγκατάστασης) / Greek translation installer

Πρόγραμμα εγκατάστασης της ελληνικής μετάφρασης για τις εκδόσεις **Windows** του **Metro 2033 Redux**: **Steam, Epic Games Store, GOG** (και για Linux / Steam Deck όταν το παιχνίδι τρέχει με Proton ή Wine).

Η μετάφραση περιλαμβάνει όλους τους υπότιτλους του 2033 Redux, το ημερολόγιο του Αρτιόμ, στόχους, όπλα, μενού, συμβουλές, και νέες γραμματοσειρές με ελληνικά γράμματα. Η ομιλία μένει ως έχει.

## Γιατί χρειάζεται πρόγραμμα

Στα Windows το παιχνίδι δεν μπορεί να φορτώσει αρχεία από `patch.vfx`, οπότε η μετάφραση πρέπει να δηλωθεί μέσα στο `content.vfx` του παιχνιδιού. Αυτό το αρχείο είναι **διαφορετικό σε κάθε κατάστημα** (Steam / Epic / GOG), άρα δεν γίνεται να μοιραστεί ένα έτοιμο. Το πρόγραμμα τροποποιεί το **δικό σου** `content.vfx`:

- παίρνει το `config.bin` της δικής σου εγκατάστασης και αλλάζει μόνο τους πίνακες των γραμματοσειρών,
- περνά τα ελληνικά στο δικό σου αρχείο κειμένων ανά κλειδί (ό,τι δεν υπάρχει στη μετάφραση μένει όπως είναι),
- γράφει ένα νέο πακέτο `content00_greek.vfs` και το προσθέτει στη λίστα του `content.vfx`,
- κρατά αντίγραφο του αρχικού ως `content.vfx.greek_backup`.

## Εγκατάσταση

1. Κατέβασε το zip από τα [Releases](../../releases/latest).
2. Κλείσε το παιχνίδι και αντίγραψε τα αρχεία του zip στον φάκελο του παιχνιδιού (εκεί που είναι το `metro.exe` και το `content.vfx`).
3. Τρέξε το `Install.bat` και περίμενε το μήνυμα «ΕΤΟΙΜΟ». Αν το τρέξεις από άλλο φάκελο, θα σου ζητήσει τον φάκελο του παιχνιδιού.
4. Στο παιχνίδι: γλώσσα κειμένου **ENGLISH**, υπότιτλοι ανοιχτοί.

**Απεγκατάσταση:** τρέξε το `Uninstall.bat`.

Αν το launcher κάνει «επαλήθευση αρχείων» ή ενημέρωση, ξανατρέξε το `Install.bat`.

## Σημειώσεις

- Στο Steam το αποτέλεσμα είναι ίδιο byte-προς-byte με το πακέτο που έχει δοκιμαστεί μέσα στο παιχνίδι. **Σε Epic και GOG δεν έχει δοκιμαστεί από εμένα σε πραγματική εγκατάσταση** — αν κάτι δεν πάει καλά, το `Uninstall.bat` τα επαναφέρει όλα· γράψε ένα issue με το τι είδες.
- **Native Linux έκδοση του Steam** (αρχείο `metro` χωρίς `.exe`): δεν χρειάζεται αυτό το πρόγραμμα· εκεί μπαίνουν τα `patch.vfx1` + `patch.vfs1` από τη σελίδα του mod στο Nexus.
- Το κοινό αρχείο κειμένων περιέχει και κείμενα του Metro: Last Light· αυτά δεν έχουν μεταφραστεί εδώ.
- Το Windows μπορεί να δείξει προειδοποίηση SmartScreen, γιατί το exe δεν έχει ψηφιακή υπογραφή.

---

## English

Installer for the Greek translation of **Metro 2033 Redux** — Windows builds from **Steam, Epic Games Store and GOG** (also Linux / Steam Deck through Proton or Wine).

The Windows `metro.exe` cannot load files from a `patch.vfx`, so the translation has to be registered in the game's own index, `content.vfx`, which differs per store. This tool edits **your own** `content.vfx`: it takes your `config.bin` and swaps only the font tables, merges the Greek text into your text file by key, writes a new pack `content00_greek.vfs`, adds it to the index, and keeps a backup as `content.vfx.greek_backup`.

**Install:** download the zip from [Releases](../../releases/latest), close the game, copy the files into the game folder (next to `metro.exe` and `content.vfx`), run `Install.bat`. Keep the in-game text language on **ENGLISH** and turn subtitles on. **Uninstall:** run `Uninstall.bat`. Run `Install.bat` again after a launcher "verify files" or update.

On Steam the output is byte-identical to the package tested in game. **Epic and GOG were not tested by the author on a real install** — `Uninstall.bat` reverts everything; please open an issue if something goes wrong. The native Linux Steam build uses the `patch.vfx1` + `patch.vfs1` files from the Nexus page instead.

## Build

```
dotnet publish Metro2033ReduxGreek -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o out
```

`Metro2033ReduxGreek/payload/` (embedded into the exe) holds the translated text file, the rebuilt font tables and the six font atlases. Fonts: Liberation Sans Narrow Bold, Noto Sans Light, Mynerve — SIL Open Font License.
