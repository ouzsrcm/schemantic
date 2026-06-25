# Schemantic.Api — Şemadan Canlı REST/GraphQL API (TASARIM / PLANLANAN)

> **Durum: planlama.** Bu doküman, henüz kodu yazılmamış `Schemantic.Api` alt-ürünü
> için mimari rehberdir. Kod **Cursor**'da, milestone sırasına göre artımlı yazılacak.
> "Henüz olmayanı üretme" ilkesi gereği, sadece **o an çalışılan milestone** kodlanır.
> Notion planı: "Schemantic.Api — Planlama" sayfası + "Schemantic.Api — Görevler (Roadmap)".

## Ne yapar?

Bir veritabanına bağlanır, Schemantic'in mevcut introspection'ıyla şemayı çıkarır ve
**çalışma anında** her tablo için ilişkisel bir REST (sonra GraphQL) API + **Swagger UI**
ayağa kaldırır. PostgREST/Hasura ruhu, ama Schemantic'in çok-provider mimarisiyle.

## Kilitlenen kararlar (değiştirme)

1. **Önce REST + Swagger, sonra GraphQL.**
2. **Önce read-only, sonra CRUD.** (Yazma opt-in; salt-okunur varsayılan.)
3. **Runtime-dynamic** — şemadan çalışma anında endpoint üretir; **kod üretimi yok.**
4. **FK-farkında ilişkiler** — REST'te `?expand=`, GraphQL'de iç içe ilişkiler.

## Çekirdek mimari ilke (bunu koru)

`Schemantic.Api` **provider deseninin dışında bir bileşendir**; tıpkı Renderer'lar gibi
yalnızca `Schemantic.Core` (DatabaseSchema + arayüzler) ile konuşmalıdır. **API kodu
hiçbir DB kütüphanesine (`SqlClient`, `Oracle.*`, `Sqlite`) doğrudan bağlanmamalıdır.**

Bunun pratik sonucu: SQL üretimi DB'ye özgüdür, ama bu özgüllük bir **soyutlamanın
arkasında** olmalı:

- Yeni bir arayüz öner: **`ISqlDialect`** (`Schemantic.Core/Abstractions`).
  Görevi: `DatabaseSchema` + tablo/kolon adlarından **parametreli** SELECT (ve sonra
  INSERT/UPDATE/DELETE) üretmek; identifier quoting, paging (OFFSET/FETCH vs LIMIT),
  tip eşleme gibi dialect farklarını kapsar.
- Her provider projesi kendi dialect'ini implemente eder (ör. `SqlServerSqlDialect`,
  `SqliteSqlDialect`, `OracleSqlDialect`). Böylece **yeni DB = yeni provider + dialect**;
  Api, model ve dialect arayüzü değişmez.
- `Schemantic.Api`, `IDatabaseProvider` (introspection) + `ISqlDialect` (sorgu) + ADO.NET
  bağlantısı üzerinden çalışır; somut DB tipini asla tanımaz.

> Not: Bağlantıyı açmak için Api'nin bir `DbConnection`'a ihtiyacı var. Bunu da provider
> üzerinden ver (ör. `IDatabaseProvider.CreateConnection(connectionString)` eklenebilir)
> ki Api somut sürücüye bağlanmasın. Bu küçük genişletmeyi v0.6'da yap.

## Veri akışı

```
Connection + Provider → DatabaseSchema (mevcut) → SchemaFilter (mevcut)
        ↓
  Schemantic.Api (ASP.NET Core)
   ├─ ISqlDialect (parametreli SQL; provider'da implemente)
   ├─ Dinamik REST endpoint'leri (Minimal API)
   ├─ DatabaseSchema'dan üretilen OpenAPI + Swagger UI
   └─ (sonra) HotChocolate ile GraphQL
```

## Önerilen tech stack

- **ASP.NET Core 8 Minimal API** — endpoint'leri çalışma anında map'lemek için.
- **OpenAPI**: endpoint'ler dinamik olduğundan OpenAPI dokümanı `DatabaseSchema`'dan
  **elle** üretilir; **Swagger UI** statik servis edilir (Swashbuckle'ın reflection
  tabanlı keşfine güvenme — şema runtime'da geliyor).
- **GraphQL: HotChocolate** — dinamik şema kurmayı destekler, FK ilişkilerine oturur.
- **Veri erişimi**: ADO.NET / Dapper; tüm sorgular **parametreli**.
- **Güvenlik**: read-only varsayılan; yazma opt-in; API key/JWT; CORS; rate limit.

## REST sözleşmesi (v0.6–v0.7)

- Liste: `GET /api/{schema}/{table}` → sayfalı sonuç (`?page`, `?pageSize`, zorunlu limit).
- Tekil: `GET /api/{schema}/{table}/{id}` (PK ile).
- Filtre: `?col=eq.value` / `gt` / `lt` / `like` / `in` (kolon adı **şemadan whitelist**).
- Sıralama: `?sort=col,-col2`.
- İlişki (v0.7): `?expand=childTable` → FK'den ilişkili kaydı göm; ayrıca
  `GET /api/{schema}/{parent}/{id}/{childByFk}`.

## Güvenlik kuralları (pazarlık yok)

- **SQL injection yok:** değerler parametreli; tablo/kolon **adları** yalnızca
  `DatabaseSchema`'dan gelen whitelist'ten (asla serbest string interpolation).
- **Read-only varsayılan:** CRUD ayrı bir flag ile açılır.
- **Aşırı veri/PII:** zorunlu sayfalama; hassas tablolar `SchemaFilter` ile hariç.
- **Yazma:** auth guard arkasında; FK/nullability/tip validasyonu şemadan.

## Kod kuralları (mevcut projeyle aynı)

- Kod İngilizce; her public tip için XML doc comment.
- Bağımlılıkları minimumda tut; gereksiz abstraction ekleme.
- `Schemantic.Api` yalnızca `Core`'a (ve ASP.NET Core'a) bağlı; provider/dialect
  kayıtları yine `Program.cs` benzeri tek bir composition root'ta.
- Testler: SQL builder/dialect'i saf metotlarla test et; REST'i `WebApplicationFactory`
  ile entegrasyon testiyle doğrula (SQLite kurulumsuz olduğu için ideal hedef).

## Milestone sırası (Notion roadmap ile birebir)

1. **API v0.6 — REST (read-only):** proje + introspection/filtre + `ISqlDialect` read +
   GET liste/by-PK + OpenAPI/Swagger UI.
2. **API v0.7 — İlişkiler:** FK expand + nested route + filtre/sayfalama/sıralama.
3. **API v0.8 — CRUD:** yazma + validasyon + auth guard.
4. **API v0.9 — GraphQL:** HotChocolate tipleri + query/mutation + GraphiQL.
5. **API v1.0 — Olgunluk:** Postman export, AuthN/AuthZ, hata/log/perf, docs+test+paketleme.

İlişkili: [`architecture.md`](architecture.md), [`providers.md`](providers.md),
[`model.md`](model.md), [`../CLAUDE.md`](../CLAUDE.md).
