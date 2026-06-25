# Schemantic — Claude Proje Rehberi

> Bu dosya, Claude'un (ve diğer yapay zekâ asistanlarının) bu projede tutarlı
> çalışması için ana referanstır. Mimari ilkeler, komutlar ve kurallar burada
> özetlenir. Daha derin konular için `.claude/docs/` altındaki dosyalara bakın.

## Proje nedir?

**Schemantic**, herhangi bir veritabanına bağlanıp şemasını tarayan ve
dokümantasyon üreten bir **.NET 8 CLI aracıdır**. Opsiyonel olarak local/uzak
LLM ile şemayı yorumlayıp tablo özetleri ekleyebilir.

- **Mevcut:** SQL Server, Oracle, SQLite provider'ları; Markdown, JSON ve HTML (Mermaid ER diyagramlı) çıktı; opsiyonel LLM tablo yorumlama (Ollama / OpenAI-uyumlu) — iskelet.
- **Planlanan:** Microsoft Access provider'ı; kolon/görünüm seviyesinde LLM yorumu; çıktı temaları.
- **Tasarımda (yeni alt-ürün):** `Schemantic.Api` — şemadan çalışma anında REST/GraphQL API + Swagger UI (bkz. [`docs/api.md`](docs/api.md)). Mevcut `DatabaseSchema` + provider'ları yeniden kullanır.

NuGet aracı olarak paketlenir (`PackAsTool`), komut adı: **`schemantic`**.

## Mimari ilkeler — her zaman bunlara uy

1. **Dil/platform:** C#, .NET 8 (LTS). `Nullable` ve `ImplicitUsings` açık.
   Tüm projeler `Directory.Build.props`'tan miras alır.
2. **Provider deseni çekirdektir.** Her veritabanı için ayrı bir
   `IDatabaseProvider` implementasyonu metadata'yı çeker ve DB-bağımsız iç
   modele (`DatabaseSchema`) map'ler. **Provider dışındaki hiçbir kod belirli bir
   veritabanına bağımlı olmamalı; her şey `DatabaseSchema` ile konuşur.**
3. **Yeni DB eklemek = yeni bir provider yazmak.** Model, renderer ve CLI değişmez.
4. **Bağımlılıkları minimumda tut.** Gereksiz abstraction/factory ekleme.
5. **Her public tip için kısa XML doc comment yaz.**
6. **Kod İngilizce** (identifier, comment). Açıklamalar Türkçe olabilir.
7. **Henüz olmayan şeyleri (Access, config dosyası, kolon-seviyesi LLM) üretme.** Sadece istenen adımı yap.

## Solution yapısı

```
schemantic/
├── src/
│   ├── Schemantic.Core/                 # DatabaseSchema modeli + arayüzler (IDatabaseProvider, IRenderer)
│   ├── Schemantic.Providers.SqlServer/  # SQL Server provider'ı
│   ├── Schemantic.Providers.Oracle/     # Oracle provider'ı
│   ├── Schemantic.Providers.Sqlite/     # SQLite provider'ı
│   ├── Schemantic.Renderers/            # Markdown + JSON + HTML renderer'ları
│   ├── Schemantic.Interpreters/         # Opsiyonel LLM yorumlama (IChatClient: Ollama/OpenAI)
│   └── Schemantic.Cli/                  # Konsol uygulaması (giriş noktası)
├── tests/
│   └── Schemantic.Tests/                # xUnit testleri
├── samples/                             # Örnek SQL ve veritabanı dosyaları
├── Schemantic.sln
├── Directory.Build.props                # Ortak build ayarları
└── global.json                          # SDK pin (8.0.0, rollForward latestFeature)
```

Bağımlılık yönü tek yönlüdür: `Cli → (Providers + Renderers + Interpreters) → Core`.
`Core` hiçbir şeye bağlı değildir. Provider'lar birbirini tanımaz.

## Sık kullanılan komutlar

```bash
# Derle
dotnet build

# Testleri çalıştır
dotnet test

# CLI'yi kaynaktan çalıştır (SQLite örneği)
dotnet run --project src/Schemantic.Cli -- \
  --provider sqlite \
  --connection "Data Source=schema.db" \
  --format markdown \
  --output schema.md

# Aracı yerel olarak paketle ve kur
dotnet pack -c Release src/Schemantic.Cli/Schemantic.Cli.csproj
dotnet tool install -g --add-source ./src/Schemantic.Cli/bin/Release Schemantic
```

### CLI seçenekleri

| Seçenek        | Zorunlu | Varsayılan                  | Açıklama |
|----------------|---------|-----------------------------|----------|
| `--connection` | Evet    | —                           | Hedef veritabanı connection string'i |
| `--provider`   | Hayır   | `sqlserver`                 | `sqlserver` \| `oracle` \| `sqlite` |
| `--format`     | Hayır   | `markdown`                  | `markdown` \| `json` \| `html` |
| `--output`     | Hayır   | formata göre (`schema.md`/`.json`/`.html`) | Çıktı dosyası yolu |
| `--schema`     | Hayır   | bağlı kullanıcı             | Sadece Oracle: okunacak şema sahibi |
| `--config`     | Hayır   | —                           | include/exclude şema-tablo filtresi (JSON) |
| `--interpret`  | Hayır   | kapalı                      | LLM ile tablo özetleri ekler (opt-in) |
| `--llm-provider` | Hayır | `ollama`                    | `ollama` \| `openai` (OpenAI-uyumlu) |
| `--llm-endpoint` | Hayır | `http://localhost:11434`    | LLM endpoint base URL |
| `--llm-model`  | Hayır   | `qwen2.5-coder`             | Model adı |
| `--llm-api-key`| Hayır   | —                           | OpenAI-uyumlu uçlar için API anahtarı |

Provider ve renderer kayıtları `src/Schemantic.Cli/Program.cs` içindeki iki
`Dictionary`'de tutulur (büyük/küçük harf duyarsız). Yeni bir provider/renderer
eklendiğinde buraya da kaydedilmelidir.

## Detaylı dokümanlar

- [`docs/architecture.md`](docs/architecture.md) — Mimari, veri akışı, çekirdek model.
- [`docs/providers.md`](docs/providers.md) — Provider'lar ve **yeni provider ekleme rehberi**.
- [`docs/model.md`](docs/model.md) — `DatabaseSchema` ve ilişkili tiplerin referansı.
- [`docs/cli.md`](docs/cli.md) — CLI kullanımı ve çıktı formatları.
- [`docs/interpreters.md`](docs/interpreters.md) — Opsiyonel LLM yorumlama katmanı.
- [`docs/testing.md`](docs/testing.md) — Test yapısı ve yeni test yazma.
- [`docs/contributing.md`](docs/contributing.md) — Katkı akışı ve kurallar.
- [`docs/releasing.md`](docs/releasing.md) — NuGet yayın süreci ve sürümleme.
- [`docs/api.md`](docs/api.md) — **Tasarımda:** Schemantic.Api (şemadan REST/GraphQL + Swagger) mimari rehberi.

## Hızlı kurallar özeti (yaparken kontrol et)

- [ ] Yeni public tip → XML doc comment var mı?
- [ ] Provider dışı kod hiçbir DB kütüphanesine (`SqlClient`, `Oracle.*`, `Sqlite`) referans vermiyor mu?
- [ ] Yeni provider → `Program.cs`'teki `providers` sözlüğüne eklendi mi?
- [ ] Metadata sorguları parametreli mi (SQL injection yok)?
- [ ] `dotnet build` ve `dotnet test` yeşil mi?
- [ ] Henüz istenmemiş özelliği (Access/LLM/HTML/config) eklemedim, değil mi?
