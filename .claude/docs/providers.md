# Provider'lar ve Yeni Provider Ekleme

Bir **provider**, tek bir veritabanı motorundan metadata okuyup ortak
`DatabaseSchema` modeline çeviren `IDatabaseProvider` implementasyonudur.
Her provider kendi `src/Schemantic.Providers.<Ad>` projesinde yaşar ve yalnızca
`Schemantic.Core`'a + kendi DB sürücüsüne bağlıdır.

## Mevcut provider'lar

| Provider   | Proje                              | DB sürücüsü (NuGet)                    | Okuduğu kaynaklar |
|------------|------------------------------------|---------------------------------------|-------------------|
| SQL Server | `Schemantic.Providers.SqlServer`   | `Microsoft.Data.SqlClient`            | `sys.*` katalog görünümleri |
| Oracle     | `Schemantic.Providers.Oracle`      | `Oracle.ManagedDataAccess.Core`       | `ALL_*` veri sözlüğü görünümleri |
| SQLite     | `Schemantic.Providers.Sqlite`      | `Microsoft.Data.Sqlite`               | `PRAGMA` + `sqlite_master` |

CLI adları (büyük/küçük harf duyarsız): `sqlserver`, `oracle`, `sqlite`.

### SQL Server (`SqlServerProvider`)

- `Name => "SqlServer"`.
- Sistem şemalarını (`sys`, `INFORMATION_SCHEMA`, `guest`) ve MS-shipped
  tabloları dışlar (`UserTableFilter`).
- Tablolar, kolonlar, primary key'ler, foreign key'ler, index'ler ve görünümler
  ile bunların kolonlarını okur.
- Açıklama olarak `MS_Description` extended property'sini kullanır.
- Tüm SQL ifadeleri `SqlServerSchemaQueries` içinde sabit olarak tutulur.

### Oracle (`OracleProvider`)

- `Name => "Oracle"`.
- Constructor opsiyonel `owner` (şema sahibi) alır; `null` ise bağlanınca
  `SELECT USER FROM DUAL` ile çözülür. CLI'da `--schema` ile geçilir.
- `ALL_TABLES`, `ALL_TAB_COLUMNS`, `ALL_TAB_COMMENTS`, vb. görünümlerden okur;
  sorgular `:owner` parametresiyle filtrelenir.
- SQL ifadeleri `OracleSchemaQueries` içindedir.

### SQLite (`SqliteProvider`)

- `Name => "Sqlite"`, sabit şema adı `main`.
- `sqlite_master` ile tablo/index listesini, `PRAGMA table_info`,
  `PRAGMA foreign_key_list`, `PRAGMA index_list/index_info` ile detayları okur.
- `DatabaseName` olarak `connection.DataSource` (dosya yolu) kullanılır.

## Yeni provider nasıl eklenir (adım adım)

Diyelim Microsoft Access ekliyoruz.

### 1. Proje oluştur

```bash
dotnet new classlib -n Schemantic.Providers.Access -o src/Schemantic.Providers.Access
dotnet sln add src/Schemantic.Providers.Access/Schemantic.Providers.Access.csproj
```

`.csproj` içinde Core referansı ve DB sürücüsünü ekle (TFM/Nullable/ImplicitUsings
`Directory.Build.props`'tan otomatik gelir):

```xml
<ItemGroup>
  <ProjectReference Include="..\Schemantic.Core\Schemantic.Core.csproj" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="<Access sürücüsü>" Version="x.y.z" />
</ItemGroup>
```

### 2. SQL/metadata sorgularını ayır

Mevcut desene uy: sorguları `AccessSchemaQueries` adlı `internal static` bir
sınıfta topla. Sorguları parametreli yaz, elle string birleştirme.

### 3. `IDatabaseProvider`'ı implement et

```csharp
public sealed class AccessProvider : IDatabaseProvider
{
    public string Name => "Access";

    public async Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await using var connection = /* aç */;
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var schema = new DatabaseSchema { DatabaseName = /* ... */ };
        // tablolar → kolonlar → PK → FK → index → görünümler
        return schema;
    }
}
```

Mevcut bir provider'ı (en sade olan `SqliteProvider`) şablon olarak al.

### 4. CLI'ya kaydet

`src/Schemantic.Cli/Program.cs` içindeki `providers` sözlüğüne bir satır ekle:

```csharp
["access"] = _ => new AccessProvider(),
```

`Schemantic.Cli.csproj`'a da yeni provider'ın `ProjectReference`'ını ekle.

### 5. Test yaz

`tests/Schemantic.Tests` altında, mümkünse dosya tabanlı/in-memory bir örnek
veritabanı ile uçtan uca bir okuma testi ekle. Bkz. [`testing.md`](testing.md).

## Provider yazım kuralları (kontrol listesi)

- [ ] `Name` motoru net tanımlıyor.
- [ ] Connection string `ArgumentException.ThrowIfNullOrWhiteSpace` ile doğrulanıyor.
- [ ] Tüm IO `async` + `CancellationToken` + `ConfigureAwait(false)`.
- [ ] Bağlantı `await using` ile kapatılıyor.
- [ ] Sistem/iç şemalar dışlanıyor (kullanıcı nesneleri).
- [ ] SQL sorguları ayrı bir `*SchemaQueries` sınıfında ve parametreli.
- [ ] Sonuç deterministik sırada (renderer zaten sıralar ama tutarlılık iyidir).
- [ ] Her public tip XML doc comment'li.
- [ ] `Program.cs` sözlüğüne + `Cli.csproj`'a kayıt eklendi.

İlişkili: [`architecture.md`](architecture.md), [`model.md`](model.md).
