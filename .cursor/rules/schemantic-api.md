Schemantic.Api — şemadan çalışma anında REST/GraphQL API + Swagger UI (yeni alt-ürün).
Tam mimari rehber: `.claude/docs/api.md`. Bu dosya Cursor için kısa kural setidir.

DURUM: Planlama. Kod milestone sırasına göre ARTIMLI yazılır. Sadece o an çalışılan
milestone'u kodla; ileri milestone'ları (CRUD, GraphQL, auth) erken üretme.

Kilitli kararlar (değiştirme):
1. Önce REST + Swagger, sonra GraphQL.
2. Önce read-only, sonra CRUD (yazma opt-in; salt-okunur varsayılan).
3. Runtime-dynamic: şemadan çalışma anında endpoint üret; KOD ÜRETİMİ YOK.
4. FK-farkında ilişkiler: REST'te ?expand=, GraphQL'de iç içe ilişkiler.

Çekirdek mimari ilke (pazarlık yok):
- `Schemantic.Api` provider deseninin DIŞINDADIR; tıpkı renderer'lar gibi yalnızca
  `Schemantic.Core` (DatabaseSchema + arayüzler) ile konuşur.
- API kodu hiçbir DB kütüphanesine (SqlClient, Oracle.*, Sqlite) DOĞRUDAN bağlanmaz.
- DB'ye özgü SQL bir soyutlamanın arkasında: `ISqlDialect` (Core/Abstractions) tanımla;
  her provider kendi dialect'ini implemente eder (quoting, paging, tip eşleme). Böylece
  "yeni DB = yeni provider + dialect"; Api ve model değişmez.
- Bağlantı için provider'a küçük bir genişletme (ör. IDatabaseProvider.CreateConnection)
  ekle ki Api somut sürücüye bağlanmasın.

Güvenlik (pazarlık yok):
- Tüm sorgular parametreli. Tablo/kolon ADLARI yalnızca DatabaseSchema'dan whitelist;
  asla serbest string interpolation.
- Read-only varsayılan; CRUD ayrı flag. Zorunlu sayfalama. Hassas tablolar SchemaFilter
  ile hariç bırakılabilir.

Tech stack: ASP.NET Core 8 Minimal API; OpenAPI dokümanı DatabaseSchema'dan ELLE üretilir
(Swashbuckle reflection'ına güvenme) + Swagger UI statik; GraphQL için HotChocolate;
veri erişimi ADO.NET/Dapper (parametreli).

Kod kuralları: İngilizce kod, XML doc comment, minimum bağımlılık, composition root tek
yerde. Testler: SQL builder/dialect saf metotla; REST WebApplicationFactory ile (SQLite
hedefli). `dotnet build` + `dotnet test` yeşil kalmalı.

Milestone sırası (Notion: "Schemantic.Api — Görevler"):
v0.6 REST(read-only) -> v0.7 İlişkiler -> v0.8 CRUD -> v0.9 GraphQL -> v1.0 Olgunluk.


dotnet run --project src/Schemantic.Api -- --provider sqlite --connection "Data Source=C:/projects/schemantic/sample.db"