# Test

Testler `tests/Schemantic.Tests` projesinde, **xUnit** ile yazılır. Proje
`Schemantic.Core` ve `Schemantic.Renderers`'a referans verir (provider'lar
gerçek veritabanı gerektirdiği için şu an doğrudan test edilmez).

## Çalıştırma

```bash
dotnet test                                  # tüm testler
dotnet test --configuration Release          # CI ile aynı yapılandırma
dotnet test --filter MarkdownRendererTests   # tek bir sınıf
```

CI (`.github/workflows/ci.yml`) `main`'e push ve PR'larda `restore → build
(Release) → test` çalıştırır.

## Mevcut testler

| Dosya                       | Kapsam |
|-----------------------------|--------|
| `SchemaModelTests.cs`       | Model varsayılanları (koleksiyonlar boş liste başlar, string'ler boş). |
| `MarkdownRendererTests.cs`  | Markdown çıktısının başlık/satır/FK/açıklama içermesi. |
| `JsonRendererTests.cs`      | JSON'un geçerli ve tablo adını içermesi. |

## Yeni test yazma

- Sınıf adı `<Birim>Tests`, metot adı davranışı anlatsın (mevcut snake_case
  stiline uy: `Render_includes_headers_table_rows...`).
- `[Fact]` kullan; parametreli senaryolarda `[Theory]` + `[InlineData]`.
- Renderer testleri için elle bir `DatabaseSchema` kur, `Render` çağır,
  çıktı string'inde beklenen parçaları doğrula.
- Mümkün olduğunca saf/deterministik tut; zaman damgası gibi değişkenleri
  doğrudan eşitleme yerine "içeriyor mu" şeklinde kontrol et.

## Provider'ları test etmek

Provider'lar canlı bağlantı gerektirir; bu yüzden birim testte değil, manuel
veya entegrasyon olarak doğrulanır. SQLite en kolayıdır çünkü dosya tabanlıdır:

```bash
# samples/seed-sqlite.sql ile örnek bir veritabanı kur
sqlite3 sample.db < samples/seed-sqlite.sql

# provider'ı uçtan uca çalıştır
dotnet run --project src/Schemantic.Cli -- \
  --provider sqlite --connection "Data Source=sample.db" --output sample.md
```

İleride SQLite provider için in-memory/dosya tabanlı bir entegrasyon test
projesi eklenebilir (sürücüsü dış servise ihtiyaç duymaz).

İlişkili: [`providers.md`](providers.md), [`contributing.md`](contributing.md).
