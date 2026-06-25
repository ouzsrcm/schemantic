# LLM Yorumlama Katmanı (v0.5)

Yorumlama katmanı **opsiyoneldir**: `--interpret` verilmezse araç eskisi gibi
çalışır. Verildiğinde, model render edilmeden önce bir `IInterpreter` çalışır ve
modeldeki `Interpretation` alanlarını AI üretimi özetlerle doldurur. Metadata
(`Description`) asla üzerine yazılmaz — AI metni ayrı alanda durur.

## Mimari

```
DatabaseSchema ──▶ IInterpreter (LlmInterpreter) ──▶ IChatClient ──▶ LLM endpoint
                        │                                   │
                  prompt üretir                    Ollama | OpenAI-uyumlu
                  Interpretation doldurur
```

İki seviye soyutlama:

- **`IInterpreter`** (`Schemantic.Core/Abstractions`) — pipeline aşaması;
  provider/renderer ile aynı seviyede. `InterpretAsync(schema, ct)` şemayı
  zenginleştirir.
- **`IChatClient`** (`Schemantic.Interpreters`) — düşük seviye LLM seam'i.
  `CompleteAsync(systemPrompt, userPrompt, ct)`. Arka uç buraya gömülür, böylece
  `LlmInterpreter` backend'den bağımsız kalır.

## Parçalar (`Schemantic.Interpreters` projesi)

| Tip                  | Görev |
|----------------------|-------|
| `LlmInterpreter`     | `IInterpreter`; her tablo için prompt kurar, `IChatClient`'i çağırır, `table.Interpretation`'ı doldurur. |
| `IChatClient`        | Sohbet tarzı LLM ucu için soyutlama (pluggability seam'i). |
| `OllamaChatClient`   | Ollama `/api/chat` (stream kapalı). |
| `OpenAiChatClient`   | OpenAI-uyumlu `/v1/chat/completions`; `Bearer` API anahtarı. |
| `InterpreterPrompt`  | Deterministik prompt üretimi (system + tablo serileştirme). Test edilebilir. |
| `InterpreterFactory` | `LlmOptions`'tan doğru `IChatClient`'i seçip `LlmInterpreter` kurar. |
| `LlmOptions`         | CLI'dan gelen yapılandırma (provider, endpoint, model, api key). |

## CLI kullanımı

```bash
# Yerel Ollama ile (varsayılan)
schemantic --provider sqlite --connection "Data Source=schema.db" \
  --interpret --llm-model qwen2.5-coder --output schema.md

# OpenAI-uyumlu bir uç ile
schemantic --provider sqlserver --connection "..." \
  --interpret --llm-provider openai \
  --llm-endpoint https://api.openai.com --llm-model gpt-4o-mini \
  --llm-api-key "$OPENAI_API_KEY"
```

İlgili flag'ler: `--interpret`, `--llm-provider`, `--llm-endpoint`, `--llm-model`,
`--llm-api-key` (bkz. [`cli.md`](cli.md)).

## Çıktı

Tablo özeti, Markdown'da bir alıntı (`> **AI summary:** ...`), HTML'de mavi bir
callout (`.ai`) olarak görünür. Özet yoksa hiçbir şey eklenmez.

## Kapsam (iskelet) ve sonraki adımlar

Bu ilk increment **tablo seviyesinde** özet üretir. Bilinçli olarak dar tutuldu:

- `ColumnInfo.Interpretation` ve `ViewInfo.Interpretation` alanları modelde var
  ama iskelet henüz doldurmuyor — kolon/görünüm seviyesi yorum sonraki görev.
- **Örnek satır çekme** (PII riski nedeniyle opt-in olmalı) henüz yok.
- HTTP istemcileri canlı bir LLM gerektirir; bu yüzden birim testler `IChatClient`'i
  sahteleyerek (FakeChatClient) `LlmInterpreter` ve prompt mantığını doğrular.

## Yeni bir LLM arka ucu eklemek

1. `Schemantic.Interpreters` altında `IChatClient` implement et (ör. `AzureChatClient`).
2. `InterpreterFactory.Create` içindeki `switch`'e bir dal ekle.
3. `IChatClient`'i sahteleyerek değil, gerçek uçla manuel doğrula; prompt/parsing
   mantığını mümkünse saf metoda ayırıp test et.

İlişkili: [`architecture.md`](architecture.md), [`model.md`](model.md), [`cli.md`](cli.md).
