# Mimari

Schemantic, veritabanına-özel mantığı çıktı üretiminden iki soyutlama ile ayırır:
**provider** (okuma) ve **renderer** (yazma). Aralarında, hiçbir veritabanına
bağlı olmayan ortak `DatabaseSchema` modeli durur.

## Genel görünüm

```
                 ┌──────────────────────┐
                 │   Schemantic.Cli     │  args parse + dosyaya yazma
                 │     (Program.cs)     │
                 └─────────┬────────────┘
            createProvider │ renderer seçimi
            ┌──────────────┴───────────────┐
            ▼                              ▼
   IDatabaseProvider                  IRenderer
   ┌───────────────┐                 ┌──────────────┐
   │ SqlServer     │   DatabaseSchema│ Markdown     │
   │ Oracle        │ ───────────────▶│ Json         │
   │ Sqlite        │   (ortak model) │              │
   └──────┬────────┘                 └──────┬───────┘
          ▼                                 ▼
     (Veritabanı)                      string içerik → dosya
```

Akış (`Program.cs` içindeki `SetAction`):

1. `--provider` adına göre sözlükten bir `IDatabaseProvider` örneği üretilir.
2. `--format` adına göre sözlükten bir `IRenderer` seçilir.
3. `provider.ReadSchemaAsync(connectionString, ct)` → `DatabaseSchema` döner.
4. `--config` verildiyse, `SchemaFilter.Apply(schema, options)` include/exclude kurallarıyla tablo/görünümleri budar (opsiyonel).
5. `--interpret` verildiyse, `IInterpreter.InterpretAsync(schema, ct)` modeli AI özetleriyle zenginleştirir (opsiyonel).
6. `renderer.Render(schema)` → `string` döner.
7. İçerik `--output` yoluna (`File.WriteAllTextAsync`) yazılır.
8. Konsola tablo sayısı, çıktı yolu ve geçen süre yazılır.

Hata olursa mesaj `stderr`'e yazılır ve çıkış kodu `1` olur; başarıda `0`.

## İki çekirdek soyutlama

### `IDatabaseProvider` (`Schemantic.Core/Abstractions`)

```csharp
public interface IDatabaseProvider
{
    string Name { get; }
    Task<DatabaseSchema> ReadSchemaAsync(string connectionString, CancellationToken ct = default);
}
```

Her veritabanı motoru için bir implementasyon yazılır. Görev: bağlan, metadata
oku, ortak modele map'le. Veritabanına özel **tüm** kod (SQL sorguları, sürücü
tipleri) bu projenin içinde kalır.

### `IRenderer` (`Schemantic.Core/Abstractions`)

```csharp
public interface IRenderer
{
    string FormatName { get; }
    string Render(DatabaseSchema schema);
}
```

`DatabaseSchema`'yı bir hedef formata (Markdown, JSON) çeviren saf fonksiyon.
Renderer'lar veritabanını veya provider'ı tanımaz; sadece modelle çalışır.

### `IInterpreter` (`Schemantic.Core/Abstractions`)

```csharp
public interface IInterpreter
{
    string Name { get; }
    Task<DatabaseSchema> InterpretAsync(DatabaseSchema schema, CancellationToken ct = default);
}
```

**Opsiyonel** pipeline aşaması. Modeldeki `Interpretation` alanlarını AI üretimi
özetlerle doldurur, metadata'ya (provider'ın okuduğu `Description`) dokunmaz.
`Schemantic.Interpreters` projesindeki `LlmInterpreter` bunu, arka uçtan bağımsız
bir `IChatClient` (Ollama / OpenAI-uyumlu) üzerinden yapar. Bkz. [`interpreters.md`](interpreters.md).

## Bağımlılık kuralı

```
Cli ──▶ Providers.*      ──▶ Core
   ├──▶ Renderers        ──▶ Core
   └──▶ Interpreters     ──▶ Core
```

- `Core` hiçbir projeye bağlı değildir, hiçbir DB sürücüsü içermez.
- `Providers.*` yalnızca `Core`'a ve kendi DB sürücüsüne bağlıdır.
- Provider'lar **birbirini tanımaz**; biri değişince diğeri etkilenmez.
- `Renderers` ve `Interpreters` yalnızca `Core`'a bağlıdır. `Interpreters` ağ için
  `HttpClient` kullanır; LLM yoksa katman hiç çalışmaz.
- `Cli` hepsini birleştirir (composition root). Provider/renderer kaydı yalnızca
  burada, iki `Dictionary` içinde yapılır.

Bu kuralın pratik sonucu: **yeni bir veritabanı eklemek tek bir yeni proje
yazmaktır.** Model, renderer ve CLI'nin mantığı değişmez (sadece `Cli`'deki
sözlüğe bir satır kayıt eklenir).

## Tasarım notları

- Tüm IO `async`'tir ve `CancellationToken` taşır; provider'lar `await using`
  ile bağlantıyı düzgün kapatır ve `ConfigureAwait(false)` kullanır.
- Provider'lar girişte `ArgumentException.ThrowIfNullOrWhiteSpace(connectionString)`
  ile connection string'i doğrular.
- Metadata sorguları parametrelidir (ör. Oracle `:owner`), elle string birleştirme yok.
- Renderer'lar deterministiktir: tablo/görünümler `Schema` sonra `Name`'e göre
  sıralanır, böylece çıktı kararlı (diff-dostu) olur.

İlişkili: [`providers.md`](providers.md), [`model.md`](model.md), [`cli.md`](cli.md).
