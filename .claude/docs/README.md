# .claude/docs — Doküman Dizini

Schemantic için yapılandırılmış proje dokümantasyonu. Başlangıç noktası bir üst
dizindeki [`CLAUDE.md`](../CLAUDE.md)'dir.

| Doküman | İçerik |
|---------|--------|
| [`../CLAUDE.md`](../CLAUDE.md)         | Ana rehber: genel bakış, mimari ilkeler, komutlar, kurallar. |
| [`architecture.md`](architecture.md)  | Provider/renderer mimarisi, veri akışı, bağımlılık kuralı. |
| [`providers.md`](providers.md)        | Mevcut provider'lar ve **yeni provider ekleme rehberi**. |
| [`model.md`](model.md)                | `DatabaseSchema` ve ilişkili tiplerin alan-alan referansı. |
| [`cli.md`](cli.md)                    | CLI seçenekleri, çıkış davranışı, çıktı formatları. |
| [`interpreters.md`](interpreters.md)  | Opsiyonel LLM yorumlama katmanı (Ollama / OpenAI-uyumlu). |
| [`testing.md`](testing.md)            | Test yapısı, çalıştırma, yeni test yazma. |
| [`contributing.md`](contributing.md)  | Geliştirme ortamı, kod kuralları, yol haritası. |

> Not: Bu klasör Claude/AI asistanları içindir ama insan geliştiriciler için de
> okunabilir tutulmuştur. Kök `README.md` son kullanıcıya, bu klasör katkı
> sağlayanlara ve asistanlara yöneliktir. `.cursor/rules/general.md` ile aynı
> mimari ilkeleri paylaşır.
