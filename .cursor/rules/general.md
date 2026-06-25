Bu repo "Schemantic": herhangi bir veritabanına bağlanıp şemasını tarayan, dokümantasyon
üreten ve opsiyonel olarak local/uzak LLM ile yorumlayan bir .NET 8 CLI aracı. NuGet'te
yayında (`Schemantic`, komut: `schemantic`, ~0.5.x). Ayrıntılı rehberler `.claude/docs/`
altında; bu dosyayla tutarlıdır.

Mimari ilkeler — her zaman bunlara uy:
- Dil/platform: C#, .NET 8 (LTS). Nullable + ImplicitUsings açık. Ortak ayarlar
  `Directory.Build.props`'tan gelir.
- Çekirdek soyutlama "provider" desenidir: her veritabanı için ayrı bir
  `IDatabaseProvider` metadata'yı çeker ve DB-bağımsız iç modele (`DatabaseSchema`) map'ler.
  Provider dışındaki HİÇBİR kod belirli bir veritabanına bağımlı olmamalı; her şey
  `DatabaseSchema` ile konuşur.
- Yeni DB eklemek = yeni bir provider yazmak. Model, renderer ve CLI değişmez.
- Bağımlılıkları minimumda tut. Gereksiz abstraction/factory ekleme.
- Her public tip için kısa XML doc comment yaz.
- Kod İngilizce (identifier, comment). Açıklamaları Türkçe verebilirsin.
- Metadata sorguları parametreli olmalı (SQL injection yok).

Solution yapısı (mevcut):
- src/Schemantic.Core         -> DatabaseSchema + arayüzler (IDatabaseProvider, IRenderer, IInterpreter) + Filtering
- src/Schemantic.Providers.*  -> SqlServer, Oracle, Sqlite provider'ları
- src/Schemantic.Renderers    -> Markdown + JSON + HTML (Mermaid ER) renderer'ları
- src/Schemantic.Interpreters -> Opsiyonel LLM yorumlama (IChatClient: Ollama / OpenAI-uyumlu)
- src/Schemantic.Cli          -> konsol uygulaması (giriş noktası, composition root)
- tests/Schemantic.Tests      -> xUnit testleri
- samples/                    -> örnek SQL/config

Mevcut durum: SQL Server / Oracle / SQLite provider'ları; Markdown/JSON/HTML çıktı;
opsiyonel LLM tablo özeti (--interpret); include/exclude config filtresi (--config).
Provider/renderer kayıtları `src/Schemantic.Cli/Program.cs`'teki sözlüklerdedir; yeni
bir provider/renderer eklenince oraya da kaydedilmeli.

Yeni alt-ürün (TASARIMDA): `Schemantic.Api` — şemadan çalışma anında REST/GraphQL API +
Swagger UI. Ayrı kurallar: `.cursor/rules/schemantic-api.md`. Mimari rehber:
`.claude/docs/api.md`.

Henüz kapsamda olmayan şeyleri (Access provider, kolon-seviyesi LLM, çıktı temaları)
talep edilmeden ÜRETME. Sadece istenen adımı yap; her oturumda gözle görülür, dar bir
kazanım hedefle. `dotnet build` ve `dotnet test` yeşil kalmalı.
