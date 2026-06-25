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
| `--format`     | Hayır   | `markdown`                  | `markdown` \| `json` \| `html`. |
| `--output`     | Hayır   | formata göre (`schema.md`/`schema.json`/`schema.html`) | Çıktı dosyası yolu. Verilmezse formata göre seçilir. |
| `--schema`     | Hayır   | bağlı kullanıcı             | Yalnızca Oracle: okunacak şema sahibi (owner). |
| `--config`     | Hayır   | —                           | include/exclude şema-tablo filtresi içeren JSON config yolu. |
| `--interpret`  | Hayır   | kapalı                      | LLM ile tablo özetleri ekler (opt-in). |
| `--llm-provider` | Hayır | `ollama`                    | `ollama` \| `openai` (OpenAI-uyumlu). |
| `--llm-endpoint` | Hayır | `http://localhost:11434`    | LLM endpoint base URL. |
| `--llm-model`  | Hayır   | `qwen2.5-coder`             | Model adı. |
| `--llm-api-key`| Hayır   | —                           | OpenAI-uyumlu uçlar için API anahtarı (Ollama yok sayar). |

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

## Config dosyası (filtreleme)

`--config <yol>` ile bir JSON dosyası verilebilir. Şema okunduktan **sonra**, LLM
yorumlamasından **önce** uygulanır; böylece hariç tutulan tablolar için LLM çağrısı
yapılmaz. Provider'lar değişmez — filtre merkezi `SchemaFilter` ile modelde işler.

```json
{
  "include": { "schemas": ["dbo"], "tables": [] },
  "exclude": { "schemas": ["audit_*"], "tables": ["*_tmp", "dbo.Stg*"] }
}
```

Kurallar:

- **Desen eşleşmesi**: `*` (herhangi bir dizi) ve `?` (tek karakter), büyük/küçük harf duyarsız.
- **Tablo desenleri** hem yalın ada (`Orders`) hem şema-nitelikli ada (`dbo.Orders`) karşı denenir.
- **include** boş/verilmemişse her şey dahildir; doluysa yalnızca eşleşenler kalır (allow-list).
- **exclude** eşleşenleri çıkarır ve **include'a üstün gelir** (deny-list).

Örnek: `samples/schemantic.config.json`.

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

### HTML (`HtmlRenderer`)

Tek dosyalık, kendine yeten (self-contained) bir HTML belgesi üretir:

- Üstte veritabanı adı, üretim zaman damgası ve tablo/görünüm sayıları.
- Sol tarafta **arama kutusu** ve gezinme listesi (tablo/görünüm adına göre canlı filtre).
- Foreign key ilişkilerinden üretilen bir **Mermaid ER diyagramı** (`erDiagram`), Mermaid CDN ile çizilir.
- Her tablo için kolon tablosu, foreign key ve index alt bölümleri; görünümler için tanım `<details>` içinde.

Tüm dinamik metin HTML-escape edilir; çıktı deterministiktir (şema+ad sıralı).

## Yeni bir format eklemek

1. `Schemantic.Renderers` altında `IRenderer` implement et (`FormatName` + `Render`).
2. `Program.cs`'teki `renderers` sözlüğüne kaydet.
3. Gerekirse `GetDefaultOutputPath`'i yeni uzantı için güncelle.
4. Test ekle (bkz. [`testing.md`](testing.md)).

İlişkili: [`architecture.md`](architecture.md), [`model.md`](model.md).
