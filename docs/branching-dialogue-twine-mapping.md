# Branching Dialogue ↔ Twine/Twee Mapping (Future Hook)

This MVP stores branching dialogue in Storylines-native JSON, but the model is intentionally shaped for future Twine interoperability.

## Current contract (MVP)

- Graph: `BranchingDialogueGraphData`
  - `id`
  - `chapterId`
  - `startNodeId`
  - `nodes[]`
- Node: `BranchingDialogueNodeData`
  - `id`, `title`, `speaker`, `text`
  - `choices[]`
  - optional map metadata: `positionX`, `positionY`
  - optional `tags[]`, `metadata`
- Choice: `BranchingDialogueChoiceData`
  - `id`, `text`, `targetNodeId`
  - optional `conditions[]`, `metadata`

## Planned Twine/Twee mapping

- Storylines node ↔ Twine passage
  - `node.id` ↔ passage identity metadata
  - `node.title` ↔ passage name
  - `node.text` ↔ passage body
- Storylines choice target ↔ Twine link
  - `choice.text` + `choice.targetNodeId` ↔ `[[choice.text->PassageName]]`
- Storylines map position ↔ Twine passage metadata
  - `positionX`, `positionY` ↔ Twee passage metadata (position hints)
- Storylines tags/metadata ↔ passage tags/custom metadata

## Non-MVP constraints

- No Twine parser/compiler is shipped in MVP.
- No guarantee yet on preserving arbitrary Twee macros.
- Conditions are placeholder-level only (no scripting engine in MVP).

## Migration strategy when import/export ships

1. Resolve graph chapter scope (`chapterId`) to target story/chapter export context.
2. Build deterministic passage names (prefer title, fallback to node id).
3. Emit links from choices; validate all target references.
4. Persist round-trip metadata for unknown fields in `metadata` where possible.
