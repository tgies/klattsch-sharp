using System;
using System.Collections.Generic;

namespace KlattschSharp.Text;

/// <summary>
/// Built-in banks: klatt1980-en (Klatt 1980 Tables II-III, ARPABET keys) and
/// two Japanese banks extending it.
/// </summary>
public static class PhonemeBanks
{
    private static readonly PhonemeBankRegistry _registry;

    static PhonemeBanks()
    {
        _registry = new PhonemeBankRegistry();
        _registry.Register(new PhonemeBankRegistry.BankDefinition(
            name: "klatt1980-en",
            entries: EnglishEntries,
            extends: null,
            language: "en-US"));
        _registry.Register(new PhonemeBankRegistry.BankDefinition(
            name: "ja-mokhtari-2000",
            entries: JapaneseMokhtariEntries,
            extends: "klatt1980-en",
            language: "ja-JP"));
        _registry.Register(new PhonemeBankRegistry.BankDefinition(
            name: "ja-hecko-2026",
            entries: JapaneseHeckoEntries,
            extends: "klatt1980-en",
            language: "ja-JP"));
    }

    /// <summary>The English ARPABET bank: 39 phonemes plus <c>"_"</c> silence.</summary>
    public static IPhonemeBank English => Get("klatt1980-en");

    /// <summary>The resolved <see cref="IPhonemeBank"/> for <paramref name="name"/>.</summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">
    /// No bank with <paramref name="name"/> is registered.
    /// </exception>
    public static IPhonemeBank Get(string name) => _registry.Resolve(name);

    /// <summary>All bank names registered in the built-in registry.</summary>
    public static IReadOnlyList<string> Names => _registry.Names;

    /// <summary>
    /// The built-in registry <c>[bank=NAME]</c> resolves against unless a
    /// compile supplies its own via <see cref="CompileOptions.Registry"/>.
    /// Registering into it affects every later compile.
    /// </summary>
    public static PhonemeBankRegistry Registry => _registry;

    private static GlideTo? Glide(double f1, double f2, double f3) => new GlideTo(f1, f2, f3);

    /// <summary>Table order becomes the bank's name order.</summary>
    private static IReadOnlyList<KeyValuePair<string, PhonemeParams>> ToEntries(
        (string Name, PhonemeParams Params)[] table)
    {
        var entries = new KeyValuePair<string, PhonemeParams>[table.Length];
        for (int i = 0; i < table.Length; i++)
        {
            entries[i] = new KeyValuePair<string, PhonemeParams>(table[i].Name, table[i].Params);
        }
        return Array.AsReadOnly(entries);
    }

    private static readonly IReadOnlyList<KeyValuePair<string, PhonemeParams>> EnglishEntries =
        BuildEnglishEntries();

    private static readonly IReadOnlyList<KeyValuePair<string, PhonemeParams>> JapaneseMokhtariEntries =
        BuildJapaneseMokhtariEntries();

    private static readonly IReadOnlyList<KeyValuePair<string, PhonemeParams>> JapaneseHeckoEntries =
        BuildJapaneseHeckoEntries();

    private static IReadOnlyList<KeyValuePair<string, PhonemeParams>> BuildEnglishEntries()
    {
        var table = new (string Name, PhonemeParams Params)[]
        {
            // Vowels (Klatt 1980 Table II)
            ("IY", new PhonemeParams(310.0, 2020.0, 2960.0, 45.0, 200.0, 400.0, 1.0, 0.9, 0.7, 1.0, Glide(290.0, 2070.0, 2960.0))),
            ("IH", new PhonemeParams(400.0, 1800.0, 2570.0, 50.0, 100.0, 140.0, 1.0, 0.9, 0.7, 1.0, Glide(470.0, 1600.0, 2600.0))),
            ("EH", new PhonemeParams(530.0, 1680.0, 2500.0, 60.0, 90.0, 200.0, 1.0, 0.9, 0.7, 1.0, Glide(620.0, 1530.0, 2530.0))),
            ("AE", new PhonemeParams(620.0, 1660.0, 2430.0, 70.0, 150.0, 320.0, 1.0, 0.9, 0.7, 1.0, Glide(650.0, 1490.0, 2470.0))),
            ("AA", new PhonemeParams(700.0, 1220.0, 2600.0, 130.0, 70.0, 160.0, 1.0, 0.9, 0.7, 1.0)),
            ("AO", new PhonemeParams(600.0, 990.0, 2570.0, 90.0, 100.0, 80.0, 1.0, 0.9, 0.7, 1.0, Glide(630.0, 1040.0, 2600.0))),
            ("AH", new PhonemeParams(620.0, 1220.0, 2550.0, 80.0, 50.0, 140.0, 1.0, 0.9, 0.7, 1.0)),
            ("UH", new PhonemeParams(450.0, 1100.0, 2350.0, 80.0, 100.0, 80.0, 1.0, 0.9, 0.7, 1.0, Glide(500.0, 1180.0, 2390.0))),
            ("UW", new PhonemeParams(350.0, 1250.0, 2200.0, 65.0, 110.0, 140.0, 1.0, 0.9, 0.7, 1.0, Glide(320.0, 900.0, 2200.0))),
            ("ER", new PhonemeParams(470.0, 1270.0, 1540.0, 100.0, 60.0, 110.0, 1.0, 0.9, 0.7, 1.0, Glide(420.0, 1310.0, 1540.0))),

            // Diphthongs (Klatt 1980 Table II)
            ("AY", new PhonemeParams(660.0, 1200.0, 2550.0, 100.0, 70.0, 200.0, 1.0, 0.9, 0.7, 1.0, Glide(400.0, 1880.0, 2500.0))),
            ("AW", new PhonemeParams(640.0, 1230.0, 2550.0, 80.0, 70.0, 140.0, 1.0, 0.9, 0.7, 1.0, Glide(420.0, 940.0, 2350.0))),
            ("EY", new PhonemeParams(480.0, 1720.0, 2520.0, 70.0, 100.0, 200.0, 1.0, 0.9, 0.7, 1.0, Glide(330.0, 2020.0, 2600.0))),
            ("OW", new PhonemeParams(540.0, 1100.0, 2300.0, 80.0, 70.0, 70.0, 1.0, 0.9, 0.7, 1.0, Glide(450.0, 900.0, 2300.0))),
            ("OY", new PhonemeParams(550.0, 960.0, 2400.0, 80.0, 50.0, 130.0, 1.0, 0.9, 0.7, 1.0, Glide(360.0, 1820.0, 2450.0))),

            // Sonorants (Klatt 1980 Table III)
            ("W", new PhonemeParams(290.0, 610.0, 2150.0, 50.0, 80.0, 60.0, 0.8, 0.7, 0.5, 1.0)),
            ("Y", new PhonemeParams(260.0, 2070.0, 3020.0, 40.0, 250.0, 500.0, 0.8, 0.7, 0.5, 1.0)),
            ("R", new PhonemeParams(310.0, 1060.0, 1380.0, 70.0, 100.0, 120.0, 0.8, 0.7, 0.5, 1.0)),
            ("L", new PhonemeParams(310.0, 1050.0, 2880.0, 50.0, 100.0, 280.0, 0.8, 0.7, 0.5, 1.0)),

            // Nasals (Klatt 1980 Table III, approximated)
            ("M", new PhonemeParams(270.0, 1270.0, 2130.0, 40.0, 200.0, 200.0, 0.7, 0.18, 0.1, 1.0)),
            ("N", new PhonemeParams(270.0, 1340.0, 2470.0, 40.0, 300.0, 300.0, 0.7, 0.2, 0.12, 1.0)),
            ("NG", new PhonemeParams(270.0, 2000.0, 2700.0, 40.0, 300.0, 300.0, 0.7, 0.2, 0.12, 1.0)),

            // Voiceless fricatives (Klatt 1980 Table III)
            ("F", new PhonemeParams(340.0, 1100.0, 2080.0, 200.0, 200.0, 1000.0, 0.0, 0.1, 0.15, 0.0)),
            ("TH", new PhonemeParams(320.0, 1290.0, 2540.0, 200.0, 200.0, 1000.0, 0.0, 0.08, 0.18, 0.0)),
            ("S", new PhonemeParams(320.0, 1390.0, 5500.0, 200.0, 200.0, 1000.0, 0.0, 0.0, 0.95, 0.0)),
            ("SH", new PhonemeParams(300.0, 1840.0, 2750.0, 200.0, 200.0, 1000.0, 0.0, 0.55, 0.65, 0.0)),

            // Voiced fricatives
            ("V", new PhonemeParams(220.0, 1100.0, 2080.0, 80.0, 100.0, 800.0, 0.4, 0.12, 0.18, 0.45)),
            ("DH", new PhonemeParams(270.0, 1290.0, 2540.0, 80.0, 100.0, 800.0, 0.4, 0.1, 0.2, 0.45)),
            ("Z", new PhonemeParams(240.0, 1390.0, 5500.0, 80.0, 100.0, 800.0, 0.4, 0.0, 0.65, 0.45)),
            ("ZH", new PhonemeParams(270.0, 1840.0, 2750.0, 80.0, 100.0, 800.0, 0.4, 0.45, 0.55, 0.45)),

            // /h/ aspirated fricative
            ("HH", new PhonemeParams(500.0, 1500.0, 2500.0, 300.0, 200.0, 300.0, 0.4, 0.4, 0.3, 0.0)),

            // Plosives (burst spectra from Klatt 1980 Table III)
            ("P", new PhonemeParams(400.0, 1100.0, 2150.0, 300.0, 150.0, 220.0, 0.1, 0.2, 0.25, 0.0, null, true)),
            ("B", new PhonemeParams(200.0, 1100.0, 2150.0, 60.0, 110.0, 130.0, 0.5, 0.2, 0.2, 0.6, null, true)),
            ("T", new PhonemeParams(400.0, 1600.0, 2600.0, 300.0, 120.0, 250.0, 0.0, 0.3, 0.55, 0.0, null, true)),
            ("D", new PhonemeParams(200.0, 1600.0, 2600.0, 60.0, 100.0, 170.0, 0.5, 0.4, 0.5, 0.6, null, true)),
            ("K", new PhonemeParams(300.0, 1990.0, 2850.0, 250.0, 160.0, 330.0, 0.0, 0.5, 0.4, 0.0, null, true)),
            ("G", new PhonemeParams(200.0, 1990.0, 2850.0, 60.0, 150.0, 280.0, 0.5, 0.5, 0.4, 0.6, null, true)),

            // Affricates
            ("CH", new PhonemeParams(350.0, 1800.0, 2820.0, 200.0, 90.0, 300.0, 0.0, 0.4, 0.55, 0.0, null, true)),
            ("JH", new PhonemeParams(260.0, 1800.0, 2820.0, 60.0, 80.0, 270.0, 0.4, 0.4, 0.5, 0.5, null, true)),

            // Silence
            ("_", new PhonemeParams(500.0, 1500.0, 2500.0, 80.0, 120.0, 160.0, 0.0, 0.0, 0.0, 0.0)),
        };

        return ToEntries(table);
    }

    private static IReadOnlyList<KeyValuePair<string, PhonemeParams>> BuildJapaneseHeckoEntries()
    {
        var table = new (string Name, PhonemeParams Params)[]
        {
            // Japanese vowels (.hecko, 2026)
            ("I", new PhonemeParams(350.0, 2000.0, 3000.0, 60.0, 130.0, 140.0, 1.0, 0.9, 0.7, 1.0)),
            ("E", new PhonemeParams(500.0, 1700.0, 2300.0, 60.0, 100.0, 250.0, 1.0, 0.9, 0.7, 1.0)),
            ("A", new PhonemeParams(750.0, 1250.0, 2250.0, 180.0, 110.0, 220.0, 1.0, 0.9, 0.7, 1.0)),
            ("O", new PhonemeParams(500.0, 850.0, 2250.0, 60.0, 110.0, 130.0, 1.0, 0.9, 0.7, 1.0)),
            ("U", new PhonemeParams(450.0, 1250.0, 2150.0, 50.0, 120.0, 130.0, 1.0, 0.9, 0.7, 1.0)),

            // Alveolar tap
            ("DX", new PhonemeParams(255.0, 1325.0, 2740.0, 55.0, 100.0, 225.0, 0.65, 0.55, 0.50, 0.6, null, true)),
        };

        return ToEntries(table);
    }

    private static IReadOnlyList<KeyValuePair<string, PhonemeParams>> BuildJapaneseMokhtariEntries()
    {
        var table = new (string Name, PhonemeParams Params)[]
        {
            // Japanese vowels (Mokhtari & Tanaka 2000)
            ("I", new PhonemeParams(298.0, 2083.0, 2954.0, 67.0, 130.0, 145.0, 1.0, 0.9, 0.7, 1.0)),
            ("E", new PhonemeParams(480.0, 1857.0, 2437.0, 65.0, 97.0, 257.0, 1.0, 0.9, 0.7, 1.0)),
            ("A", new PhonemeParams(744.0, 1240.0, 2426.0, 182.0, 117.0, 222.0, 1.0, 0.9, 0.7, 1.0)),
            ("O", new PhonemeParams(460.0, 857.0, 2405.0, 61.0, 114.0, 134.0, 1.0, 0.9, 0.7, 1.0)),
            ("U", new PhonemeParams(355.0, 1282.0, 2233.0, 55.0, 129.0, 135.0, 1.0, 0.9, 0.7, 1.0)),

            // Alveolar tap (reproduced from ja-hecko-2026)
            ("DX", new PhonemeParams(255.0, 1325.0, 2740.0, 55.0, 100.0, 225.0, 0.65, 0.55, 0.50, 0.6, null, true)),
        };

        return ToEntries(table);
    }
}
