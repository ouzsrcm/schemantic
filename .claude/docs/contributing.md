# Katkı

## Geliştirme ortamı

- **.NET 8 SDK** gerekir (`global.json` sürümü 8.0.0'a sabitler, `rollForward:
  latestFeature`).
- Ortak build ayarları `Directory.Build.props`'tan gelir: `net8.0`, `LangVersion
  latest`, `Nullable enable`, `ImplicitUsings enable`, `EnforceCodeStyleInBuild`.
- Kod stili `.editorconfig` ile zorlanır.

## Akış

```bash
git clone https://github.com/ouzsrcm/schemantic.git
cd schemantic
dotnet build
dotnet test
```

1. Bir branch aç, küçük ve odaklı bir değişiklik yap.
2. `dotnet build` ve `dotnet test` yeşil olsun.
3. PR aç; CI `main` hedefli PR'larda otomatik çalışır.

## Kod kuralları

- **Kod İngilizce** (identifier ve comment). Açıklama/PR metni Türkçe olabilir.
- Her public tip ve üye için kısa **XML doc comment**.
- Bağımlılıkları minimumda tut; gereksiz abstraction/factory ekleme.
- Mimari kuralı bozma: provider dışındaki hiçbir kod belirli bir veritabanına
  bağlı olmamalı (bkz. [`architecture.md`](architecture.md)).
- Henüz kapsamda olmayan özellikleri (Access, LLM, HTML, config) talep edilmeden ekleme.

## Sık görevler

| Görev                  | Bakılacak doküman |
|------------------------|-------------------|
| Yeni veritabanı eklemek | [`providers.md`](providers.md) |
| Yeni çıktı formatı      | [`cli.md`](cli.md) (sonundaki bölüm) |
| Modeli genişletmek      | [`model.md`](model.md) |
| Test yazmak             | [`testing.md`](testing.md) |

## Aracı yerel paketleyip denemek

```bash
dotnet pack -c Release src/Schemantic.Cli/Schemantic.Cli.csproj
dotnet tool install -g --add-source ./src/Schemantic.Cli/bin/Release Schemantic
schemantic --provider sqlite --connection "Data Source=schema.db" --output schema.md
```

## Yol haritası

| Sürüm | Kapsam |
|-------|--------|
| MVP   | SQL Server → Markdown |
| v0.2  | Oracle provider |
| v0.3  | Access provider |
| v0.4  | HTML çıktı + ER diyagramları |
| v0.5  | Local LLM şema yorumu |
| v1.0  | Stabil CLI, dokümante provider API |

İlişkili: [`../CLAUDE.md`](../CLAUDE.md).
