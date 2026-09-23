# OpenAI Live provider (`gpt-live-1`)

Implements `ILiveCompletion` against OpenAI's Live endpoint, `wss://api.openai.com/v1/live/sessions`.
It shares the **`openai`** provider key with the realtime and chat providers; what separates it is
the interface, not the name.

`ILiveCompletion` derives from `IRealTimeCompletion`, so a live session drives the same hub,
middleware and hooks as a realtime one. The separate interface is what keeps the two families
apart in DI: the live provider registers as `ILiveCompletion` only, so
`GetServices<IRealTimeCompletion>()` never returns it and `gpt-realtime` cannot be replaced by
accident. A live session happens only when an agent asks for one.

Reference: https://developers.openai.com/api/docs/guides/live

## Enabling it

Live is chosen per agent, through `llm_config.live` - a sibling of `llm_config.realtime`, not a
variation of it:

```jsonc
// agents/<agent>/agent.json
"llm_config": {
  "live": {
    "provider": "openai",
    "model": "gpt-live-1"
  }
}
```

`RealtimeHub` resolves it like this:

| Agent config | Family | Resolved from |
| --- | --- | --- |
| `llm_config.live` set | live | `GetServices<ILiveCompletion>()` |
| `llm_config.realtime` set | realtime | `GetServices<IRealTimeCompletion>()` |
| neither | realtime | `RealtimeModel` settings, else `openai`/`gpt-realtime` |

Both families answer to the provider name `openai`, so the name alone decides nothing: the family
comes from which agent config is set, and the name is then looked up in that family's list only.
`RealtimeModel` therefore always means realtime, and a name with no provider in that list throws
with the family and provider named.

Deployment-wide defaults for the live session itself:

```jsonc
// appsettings.json
"OpenAi": {
  "Live": {
    "Voice": "marin",                // Live has its own voice set; "alloy" is rejected
    "BackendModel": "gpt-5.6-luna",  // model that does the reasoning and runs tools
    "TranscriptIdleMs": 800,
    "AudioIdleMs": 500
  }
}
```

Audio format still comes from `RealtimeModel` (`g711_ulaw` for telephony, `pcm16` for browser
audio), as does `MaxResponseOutputTokens`; those settings are shared by both families.

Credentials come from the `gpt-live-1` model entry under the `openai` provider in `LlmProviders`,
next to the realtime and chat models, so an existing API key is not duplicated. The entry carries
`"Type": "live"` and the `Live` capability, which keeps it out of realtime model lookups:

```jsonc
{ "Id": "gpt-live", "Name": "gpt-live-1", "ApiKey": "", "Type": "live", "Capabilities": [ "Live" ] }
```

Add `"Endpoint"` to that entry to send the socket somewhere else - a proxy, or a regional host.
Left out, it is `wss://api.openai.com/v1/live/sessions`; a value that is not an absolute URI is
logged and ignored rather than failing the call.

## How the Live protocol maps onto the completion interface

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
| `onConversationItemCreated` | `session.delegation.created` |

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

Override it per agent with a `live` channel instruction - the same mechanism as any other
channel override, so it needs no code and no setting:

```
agents/<agent>/instructions/instruction.live.liquid
```

It is rendered as a liquid template like the default instruction. An agent with no such file
keeps `DefaultVoiceInstruction`.

The agent instruction is never appended to the voice model. It reaches the backend through
`session.start`, and is refreshed with `session.update` on `delegation.responses` whenever
`UpdateSession` runs - after a function call or an agent transfer.

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

## Nothing runs on the receive loop

The socket has a single consumer, and anything awaited inside it stops the socket being read.
On a half duplex session that costs nothing, because the model is silent while a tool runs. On
Live it is the whole problem: the model keeps talking through a delegation, so audio frames pile
up unread and the caller hears the reply cut in half - "Okay, checking" ... ten seconds ... "that
now."

So the receive loop parses an event and moves on. Conversation work - invoking a tool, recording
a turn - goes to a `SerialWorkQueue`, which runs it on one background worker in the order it was
queued. Serial rather than fire and forget, because two tool calls must not interleave their
session updates, and because the conversation state this work touches is not thread safe.

What stays on the loop is what must not queue behind a tool call: audio deltas and live
transcript deltas, both of which are only a write to the caller's socket.

Turn boundaries are still decided on the loop. The flushes take the buffer there, synchronously,
and queue only its delivery - otherwise a turn queued behind a slow tool would pick up the words
the model spoke after it.

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
- **Only responses delegation is supported.** Live also offers a "client" mode, where the
  application answers delegations itself; it is not implemented here.
- **Only `delegation.responses` is mutable.** Everything else in the session object is fixed at
  `session.start`; changing the delegation mode requires a new session.
- **Billing is per second, not per token.** `TokenStatsModel` reports the backend Responses
  usage; the voice minutes are reported separately by `session.closed`.
- `RealtimeModelSettings.ModelResponseTimeoutSeconds` has no effect here, since it is defined in
  terms of the realtime `response.done` lifecycle that Live does not have.
