# Şema Modeli Referansı

Ortak model `Schemantic.Core/Model` altında yaşar. Veritabanına bağlı değildir;
provider'lar onu doldurur, renderer'lar onu okur. Tüm tipler basit POCO'lardır
(get/set), koleksiyonlar boş liste ile başlatılır, string'ler `string.Empty`
ile başlar (null değil).

## `DatabaseSchema`

Kök kapsayıcı.

| Üye            | Tip                  | Açıklama |
|----------------|----------------------|----------|
| `DatabaseName` | `string`             | Veritabanının mantıksal adı. |
| `Tables`       | `IList<TableInfo>`   | Bulunan tablolar. |
| `Views`        | `IList<ViewInfo>`    | Bulunan görünümler. |

## `TableInfo`

Tek bir tablonun metadata'sı.

| Üye           | Tip                       | Açıklama |
|---------------|---------------------------|----------|
| `Schema`      | `string`                  | Şema adı (ör. `dbo`). |
| `Name`        | `string`                  | Şema ön eki olmadan tablo adı. |
| `Description` | `string?`                 | Metadata'dan okunabilen açıklama. |
| `Columns`     | `IList<ColumnInfo>`       | Kolonlar. |
| `ForeignKeys` | `IList<ForeignKeyInfo>`   | Dışa giden foreign key'ler. |
| `Indexes`     | `IList<IndexInfo>`        | Index'ler. |

## `ColumnInfo`

| Üye            | Tip       | Açıklama |
|----------------|-----------|----------|
| `Name`         | `string`  | Kolon adı. |
| `DataType`     | `string`  | Provider'a özel tip adı (ör. `nvarchar`, `int`). |
| `IsNullable`   | `bool`    | NULL kabul ediyor mu. |
| `IsPrimaryKey` | `bool`    | Primary key'in parçası mı. |
| `MaxLength`    | `int?`    | Değişken uzunluklu tipler için maksimum uzunluk. |
| `DefaultValue` | `string?` | Tanımlıysa default ifade/literal. |
| `Description`  | `string?` | Metadata'dan açıklama. |

## `ForeignKeyInfo`

| Üye                | Tip      | Açıklama |
|--------------------|----------|----------|
| `Name`             | `string` | Constraint adı. |
| `Column`           | `string` | FK'yi tutan yerel kolon. |
| `ReferencedSchema` | `string` | Referans verilen tablonun şeması. |
| `ReferencedTable`  | `string` | Referans verilen tablo. |
| `ReferencedColumn` | `string` | Referans verilen kolon. |

## `IndexInfo`

| Üye        | Tip               | Açıklama |
|------------|-------------------|----------|
| `Name`     | `string`          | Index adı. |
| `IsUnique` | `bool`            | Benzersizlik zorunluluğu var mı. |
| `Columns`  | `IList<string>`   | Index'teki kolonlar (sıralı). |

## `ViewInfo`

| Üye           | Tip                  | Açıklama |
|---------------|----------------------|----------|
| `Schema`      | `string`             | Şema adı. |
| `Name`        | `string`             | Görünüm adı. |
| `Description` | `string?`            | Metadata'dan açıklama. |
| `Definition`  | `string?`            | Mevcutsa görünümün SQL tanımı. |
| `Columns`     | `IList<ColumnInfo>`  | Görünümün kolonları. |

## Tasarım notları

- Modeli genişletirken **veritabanına özel kavram sızdırma.** Bir alan yalnızca
  tek bir motorda anlamlıysa, modele değil provider'a ait olmalı.
- Yeni alan eklersen: ilgili provider'larda doldur, renderer'larda göster,
  `model.md` ve testleri güncelle.
- Tipler değiştiğinde JSON çıktısı da değişir (camelCase serileştirme), bu
  sözleşme kabul edilebilir bir kırılma sayılır — değişikliği commit mesajında belirt.

İlişkili: [`architecture.md`](architecture.md), [`cli.md`](cli.md).
