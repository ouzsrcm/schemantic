# CLI Kullanımı

Giriş noktası: `src/Schemantic.Cli/Program.cs`. `System.CommandLine` ile bir
`RootCommand` kurulur; provider ve renderer seçimi iki sözlükten yapılır.

## Çalıştırma

Kaynaktan:

```bash
dotnet run --project src/Schemantic.Cli -- \
  --provider sqlite \
  --connection "Data Source=schema.db" \
  --format markdown \
  --output schema.md
```

Kurulu .NET aracı olarak:

```bash
schemantic --provider sqlserver \
  --connection "Server=localhost;Database=MyDb;Trusted_Connection=True;" \
  --output schema.md
```

## Seçenekler

| Seçenek        | Zorunlu | Varsayılan                  | Açıklama |
|----------------|---------|-----------------------------|----------|
| `--connection` | Evet    | —                           | Hedef veritabanı connection string'i. |
| `--provider`   | Hayır   | `sqlserver`                 | `sqlserver` \| `oracle` \| `sqlite`. |
| `--format`     | Hayır   | `markdown`                  | `markdown` \| `json`. |
| `--output`     | Hayır   | `schema.md` veya `schema.json` | Çıktı dosyası yolu. Verilmezse formata göre seçilir. |
| `--schema`     | Hayır   | bağlı kullanıcı             | Yalnızca Oracle: okunacak şema sahibi (owner). |

Bilinmeyen bir `--provider` veya `--format` verilirse, mevcut seçenekleri
listeleyen bir hata `stderr`'e yazılır ve çıkış kodu `1` olur.

## Çıkış davranışı

Başarıda `stdout`'a şunlar yazılır:

```
Tables found: <n>
Output written to: <tam yol>
Elapsed: <saniye>s
```

Çıkış kodları: başarı `0`, hata `1` (mesaj `stderr`'e gider).

## Bağlantı dizesi örnekleri

| Provider   | Örnek |
|------------|-------|
| SQL Server | `Server=localhost;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| Oracle     | `User Id=app;Password=***;Data Source=//host:1521/ORCLPDB1;` (gerekirse `--schema OWNER`) |
| SQLite     | `Data Source=schema.db` |

## Çıktı formatları

### Markdown (`MarkdownRenderer`)

Üretilen belge: başlık (`# <DatabaseName>`), üretim zaman damgası, tablo/görünüm
sayıları, bir İçindekiler (Table of Contents) bağlantı listesi, sonra her tablo
için bir bölüm:

- Kolon tablosu: `Column | Type | Nullable | PK | Default | Description`.
- Varsa **Foreign Keys** ve **Indexes** alt başlıkları.
- Görünümler ayrı bir **Views** bölümünde; tanım varsa `<details>` içinde SQL olarak.

Çıktı deterministiktir (şema+ad sıralı), hücrelerdeki `|` ve satır sonları kaçışlanır.

### JSON (`JsonRenderer`)

`DatabaseSchema`'yı girintili (indented), **camelCase** anahtarlarla serileştirir.
Programatik tüketim veya başka araçlara besleme için uygundur.

## Yeni bir format eklemek

1. `Schemantic.Renderers` altında `IRenderer` implement et (`FormatName` + `Render`).
2. `Program.cs`'teki `renderers` sözlüğüne kaydet.
3. Gerekirse `GetDefaultOutputPath`'i yeni uzantı için güncelle.
4. Test ekle (bkz. [`testing.md`](testing.md)).

İlişkili: [`architecture.md`](architecture.md), [`model.md`](model.md).
