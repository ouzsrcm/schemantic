Bu repo "Schemantic" adlı açık kaynak bir araç. Amacı: herhangi bir veritabanına
(önce SQL Server, sonra Oracle ve Access) bağlanıp şemasını tarayan, dokümantasyon
üreten ve ileride opsiyonel olarak local LLM ile yorumlayan bir CLI aracı.

Mimari ilkeler — her zaman bunlara uy:
- Dil/platform: C#, .NET 8 (LTS). Nullable enabled, ImplicitUsings enabled.
- Çekirdek soyutlama "provider" desenidir: her veritabanı için ayrı bir
  IDatabaseProvider implementasyonu metadata'yı çeker ve DB-bağımsız bir iç
  modele (SchemaModel) map'ler. Provider dışındaki HİÇBİR kod belirli bir
  veritabanına bağımlı olmamalı; her şey SchemaModel ile konuşur.
- Yeni DB eklemek = yeni bir provider yazmak. Model, renderer ve CLI değişmez.
- Bağımlılıkları minimumda tut. Gereksiz abstraction/factory ekleme.
- Her public tip için kısa XML doc comment yaz.
- Kod İngilizce (identifier, comment). Açıklamaları bana Türkçe verebilirsin.

Solution yapısı:
- src/Schemantic.Core         -> SchemaModel + arayüzler (IDatabaseProvider, IRenderer)
- src/Schemantic.Providers.*  -> Veritabanı provider'ları (SqlServer, Oracle, Sqlite, ...)
- src/Schemantic.Renderers    -> Markdown (ileride HTML) renderer'ları
- src/Schemantic.Cli          -> konsol uygulaması (giriş noktası)
- tests/Schemantic.Tests      -> xUnit testleri
- samples/                    -> Örnek SQL ve veritabanı dosyaları

Henüz olmayan şeyleri (Oracle, Access, LLM, HTML, config) ÜRETME. Sadece
istenen adımı yap.