# Release Süreci (NuGet)

Schemantic, `Schemantic` paket adıyla bir **.NET aracı** (`PackAsTool`) olarak
NuGet'e yayınlanır. Komut adı: `schemantic`.

## Sürümleme ilkesi (SemVer)

- **0.x** — pre-1.0. API kırılabilir; LLM iskelet ve Oracle canlı doğrulama açık.
- Yeni özellik → MINOR (0.5 → 0.6). Geriye dönük kırılma → 1.0 öncesi MINOR, sonrası MAJOR.
- Yalnız hata düzeltme → PATCH (0.5.0 → 0.5.1).
- Tek doğruluk kaynağı **git tag**'idir: `v0.5.0` → paket `0.5.0` (workflow tag'den türetir).

## Otomatik yayın (önerilen yol)

Yayın, `v*` biçiminde bir tag push'uyla tetiklenir
(`.github/workflows/release.yml`): restore → build (Release) → test → pack → NuGet login (OIDC) → push.

Kimlik doğrulama **Trusted Publishing (OIDC)** ile yapılır — uzun ömürlü API key yok.
Workflow `id-token: write` izniyle GitHub OIDC token'ını `NuGet/login@v1` üzerinden
kısa ömürlü (1 saat) bir NuGet anahtarına çevirir.

Tek seferlik kurulum (nuget.org → Account → Trusted Publishing → yeni policy):

- **Package Owner:** `ouzsrcm`
- **Repository Owner:** `ouzsrcm`
- **Repository:** `schemantic`
- **Workflow File:** `release.yml` (sadece dosya adı)
- **Environment:** boş

Her yayın için:

```bash
# 1) Çalışma yeşil mi?
dotnet build && dotnet test

# 2) CHANGELOG.md'de [Unreleased] altını yeni sürüme taşı, tarih ver.

# 3) Tag at ve push et (sürüm numarası tag'den gelir):
git tag v0.6.0
git push origin v0.6.0
```

Workflow paketi otomatik üretir ve nuget.org'a gönderir. `--skip-duplicate`
sayesinde aynı sürüm iki kez gönderilse de hata vermez.

## Manuel yayın (fallback)

Workflow kullanılamıyorsa:

```bash
dotnet pack -c Release src/Schemantic.Cli/Schemantic.Cli.csproj \
  -p:Version=0.6.0 --output ./artifacts
dotnet nuget push "./artifacts/*.nupkg" \
  --api-key "<NUGET_API_KEY>" \
  --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Yayın öncesi kontrol listesi

- [ ] `dotnet build` ve `dotnet test` yeşil.
- [ ] `Cli.csproj` içindeki `<Version>` yerel geliştirme için güncel (yayın sürümü tag'den gelir).
- [ ] `<Description>`/`<PackageTags>` güncel özellikleri yansıtıyor.
- [ ] `CHANGELOG.md`'de yeni sürüm girişi var (tarih + Added/Changed/Fixed).
- [ ] README'deki "Features" listesi doğru.
- [ ] Notion'da ilgili roadmap görevleri güncellendi.
- [ ] nuget.org'da Trusted Publishing policy tanımlı (repo + `release.yml`).

## Yayın sonrası

- [ ] Tag'den GitHub Release oluştur (changelog notlarını yapıştır).
- [ ] `nuget.org/packages/Schemantic` üzerinde yeni sürümü doğrula.
- [ ] `dotnet tool install -g Schemantic` ile temiz kurulumu dene.
- [ ] `CHANGELOG.md`'ye yeni bir `[Unreleased]` başlığı aç.

İlişkili: [`contributing.md`](contributing.md), [`../CLAUDE.md`](../CLAUDE.md).
