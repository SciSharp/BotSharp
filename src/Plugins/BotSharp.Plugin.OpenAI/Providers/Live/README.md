# OpenAI Live provider (`gpt-live-1`)

Implements `IRealTimeCompletion` against OpenAI's Live endpoint, `wss://api.openai.com/v1/live/sessions`.
It is registered alongside the existing realtime provider and selected by the provider key
**`openai-live`**, so `gpt-realtime` keeps working unchanged.

Reference: https://developers.openai.com/api/docs/guides/live

## Enabling it

```jsonc
// appsettings.json
"RealtimeModel": {
  "Provider": "openai-live",
  "Model": "gpt-live-1",
  "InputAudioFormat": "g711_ulaw",   // telephony; use "pcm16" for browser audio
  "OutputAudioFormat": "g711_ulaw"
},

"OpenAi": {
  "Live": {
    "Voice": "marin",                // Live has its own voice set; "alloy" is rejected
    "DelegationType": "responses",   // or "client"
    "BackendModel": "gpt-5.6-luna",  // model that does the reasoning and runs tools
    "TranscriptIdleMs": 800,
    "AudioIdleMs": 500
  }
}
```

Per agent, set `LlmConfig.Realtime.Provider` to `openai-live` instead.

Credentials are read from the `openai-live` entry in `LlmProviders` and fall back to the
`openai` entry, so an existing API key does not have to be duplicated.

## How the Live protocol maps onto `IRealTimeCompletion`

| Interface member | Live wire event |
| --- | --- |
| `Connect` | `session.start` (carries model, instructions, audio, delegation) |
| `AppenAudioBuffer` | `session.input_audio.append` |
| `UpdateSession` | `session.update` (delegation only) + `session.instructions.append` |
| `InsertConversationItem` (function) | `response.item.create` with `function_call_output` |
| `InsertConversationItem` (assistant) | `session.commentary.append` |
| `InsertConversationItem` (user) | `response.item.create` with a typed `message` |
| `TriggerModelInference` | `session.commentary.append` + `response.create` |
| `Disconnect` | `session.close` |

| Callback | Live wire event |
| --- | --- |
| `onModelReady` | `session.started` |
| `onModelAudioDeltaReceived` | `session.output_audio.delta` |
| `onModelAudioTranscriptDone` / `onModelResponseDone` | `session.output_transcript.delta`, flushed on silence |
| `onInputAudioTranscriptionDone` | `session.input_transcript.delta`, flushed on silence |
| `onModelResponseDone` (tool call) | `response.event` → `response.output_item.done` |
| `onConversationItemCreated` | `session.delegation.created` (client delegation only) |

## Greeting the caller

The model opens the call rather than waiting to be spoken to. On `session.started`, once the
backend handler is configured, the greeting is sent as `session.commentary.append` - the channel
the model speaks from, and a one-shot one, unlike `session.instructions.append` which would leave
a standing order to greet for the rest of the call.

The agent's own `.welcome` template wins, rendered exactly as the text chat renders it, so an
agent keeps the opening line it already has. Rich content is reduced to its wording, since only
text can be spoken. Agents without a template fall back to `OpenAi:Live:Greeting`, and
`GreetOnStart: false` turns the whole thing off.

Nothing is written to the conversation here: the model speaks the greeting, its transcript comes
back, and the normal turn flush records it like anything else it said.

## Two prompts, not one

Live splits the prompt across its two halves, and so does this provider:

| Prompt | Goes to | Content |
| --- | --- | --- |
| `session.instructions` | voice model | how the conversation sounds, and when to delegate |
| `delegation.responses.instructions` | backend model | the BotSharp agent instruction and its tools |

The voice prompt defaults to `LivePromptConstants.DefaultVoiceInstruction`, which follows the
structure in OpenAI's [prompting guide](https://developers.openai.com/api/docs/guides/live-prompting):
personality, backchannel policy, interruption policy, delegation policy, response style. Keep it
short - the guide is explicit that reasoning and procedures belong in the backend prompt.

Override it per deployment without touching code:

```jsonc
"OpenAi": { "Live": { "VoiceInstructions": "You are Ada, the friendly voice of ..." } }
```

The agent instruction is never appended to the voice model. It reaches the backend through
`session.start`, and is refreshed with `session.update` on `delegation.responses` whenever
`UpdateSession` runs - after a function call or an agent transfer.

## Client delegation

With `"DelegationType": "client"` the voice model delegates to BotSharp instead of to a managed
Responses backend. `session.delegation.created` carries only an id and an offset, never the task
text, so the utterance comes from the transcript stream and the two are paired up in whichever
order they arrive - the model normally delegates before the user has stopped talking.

Once paired, `RoutingService.InstructLoop` (routing agents) or `InstructDirect` runs the turn and
the answer is returned with `session.commentary.append`, which the model paraphrases aloud.
Function results produced along the way go back as `session.thinking.append`.

Two windows govern the pairing:

| Setting | Default | Meaning |
| --- | --- | --- |
| `DelegationWaitMs` | 8000 | how long a delegation waits for the user to finish |
| `UtteranceClaimMs` | 1500 | how long a finished turn waits to be claimed before it is merely recorded |

An unclaimed turn means the voice model answered without help; it is still written to the
conversation record, just without running the backend.

In this mode there is no `delegation.responses` to carry the agent instruction, and none is
needed: `InstructLoop` applies it locally. The voice model still runs on the voice prompt alone.

Conversation history holds one user turn (persisted by the routing call) and one assistant turn
(the spoken transcript). The backend's raw answer is internal and is not persisted, matching how
responses delegation behaves.

## TriggerModelInference speaks, it does not re-instruct

Every caller of `TriggerModelInference(text)` passes a one-off line for the current turn, never
standing policy - `Say to user: "..."` from the conversation hook, `Response based on the user
input: ...` from Twilio. `session.instructions.append` would make those permanent, so they go to
`session.commentary.append`, which the model paraphrases aloud and then leaves behind.

A wrapper such as `Say to user: "your order shipped"` is unwrapped first, otherwise the model
reads the instruction out. Anything that is not a short prefix plus a fully quoted line passes
through untouched.

The agent instruction is the one thing filtered out here: the hook hands it back to mean
"continue", and speaking it aloud would read the whole agent prompt to the caller.

## Append budget

Every `session.*.append` is capped at 500 tokens server side, while the session instructions
they build up hold 16,384. Appends concatenate, so over-budget content is **split on sentence
boundaries** across up to 5 appends and reassembles into the original - nothing is truncated in
the normal case. `MaxAppendTokens` (default 450) sets the per-append budget.

Only content too large for 5 appends loses anything, and the channels differ there:

- **commentary** and **thinking** keep what fits, marked with `[...]` so the model can see it
  is partial.
- **instructions** are dropped entirely with an error logged. Half a directive still reads as a
  directive - "do not mention the discount to the user" cut short inverts its meaning - so
  leaving the model on its existing instructions is the safer failure.

The budget is counted in tokens rather than characters because the two diverge by script: Latin
prose is roughly four characters per token, CJK closer to one. A character cap sized for English
overshoots the server limit badly for Chinese, Japanese and Korean.

## Behaviour that differs from the realtime provider

- **No turn completion event.** Live streams transcript deltas and never marks the end of a turn,
  so turns are closed by **alternation**: each speaker's buffer is flushed when the other one
  starts talking. That writes a turn the moment it is genuinely over, and a thinking pause mid
  sentence no longer splits one reply into two.
  Idle timers remain only as a backstop for the last turn of a session, when nobody speaks after
  it - `TranscriptIdleMs` for the model, the longer `InputTranscriptIdleMs` for the user, since
  model audio arms the former but nothing arms the latter between words. `AudioIdleMs` still
  drives `onModelAudioResponseDone`.
- **No interruption callback.** Live is full duplex and arbitrates barge-in inside the model,
  so `onInterruptionDetected` is never raised and `CancelModelResponse` is a no-op. Client-side
  playback buffers are deliberately not cleared.
- **`RemoveConversationItem` is unsupported.** The server owns the live history.
- **`audio` has no `input` section.** Live accepts only `audio.format` and `audio.output.voice`;
  noise reduction and turn detection are handled inside the model, unlike the realtime API.
  The server rejects the entire session on an unknown field, so keep this object minimal.
- **Only `delegation.responses` is mutable.** Everything else in the session object is fixed at
  `session.start`; changing the delegation mode requires a new session.
- **Billing is per second, not per token.** `TokenStatsModel` reports the backend Responses
  usage; the voice minutes are reported separately by `session.closed`.
- `RealtimeModelSettings.ModelResponseTimeoutSeconds` has no effect here, since it is defined in
  terms of the realtime `response.done` lifecycle that Live does not have.
