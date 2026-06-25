# Changelog

Bu projedeki önemli değişiklikler burada tutulur. Format
[Keep a Changelog](https://keepachangelog.com/) ve sürümleme
[SemVer](https://semver.org/) temellidir.

## [Unreleased]

### Added

- **Schemantic.Api** — şemadan runtime read-only REST API + OpenAPI/Swagger UI (SQLite).
- **ISqlDialect** soyutlaması (parametreli SELECT, identifier quoting, sayfalama).
- **IDatabaseProvider.CreateConnection** — API'nin somut sürücüye bağlanmadan veri sorgusu çalıştırması.

## [0.5.0] - 2026-06-25

İlk herkese açık NuGet sürümü. 1.0 öncesi (pre-1.0): bazı parçalar iskelet veya
canlı doğrulama bekliyor, bu yüzden API kırılabilir.

### Added

- **Oracle provider** (`ALL_*` veri sözlüğü görünümleri). _Not: TNS alias (ORA-12154) nedeniyle canlı doğrulama hâlâ açık._
- **SQLite provider** (uçtan uca test edildi).
- **View desteği** — model, tüm provider'lar ve renderer'lar görünüm okur/gösterir.
- **JSON renderer** (camelCase, girintili).
- **HTML renderer** — arama, sidebar navigasyon ve foreign key'lerden **Mermaid ER diyagramı**.
- **Opsiyonel LLM yorumlama** (`--interpret`) — tablo özetleri; Ollama ve OpenAI-uyumlu uçlar. _İskelet: tablo seviyesi._
- **Config dosyası** (`--config`) — include/exclude şema/tablo filtresi, `*`/`?` wildcard.
- **Katkı + provider yazım rehberi** (`.claude/docs`).
- **Release otomasyonu** — `v*` tag push'unda NuGet'e yayın (GitHub Actions).

### Changed

- Paket `Description` ve etiketleri güncel özellikleri yansıtacak şekilde güncellendi.

## [0.1.0]

İlk iskele.

### Added

- SQL Server provider (tablolar, kolonlar, foreign key'ler, index'ler).
- Markdown renderer (tek dosya çıktı).
- CLI giriş noktası, README, MIT lisansı, GitHub Actions CI ve temel testler.

[Unreleased]: https://github.com/ouzsrcm/schemantic/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/ouzsrcm/schemantic/releases/tag/v0.5.0
[0.1.0]: https://github.com/ouzsrcm/schemantic/releases/tag/v0.1.0
