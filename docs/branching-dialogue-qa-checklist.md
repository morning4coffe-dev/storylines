# Branching Dialogue MVP — Manual QA Checklist

## Authoring flow

- [ ] Open a project with existing chapters.
- [ ] Open **Branching** from the command bar.
- [ ] Pick a chapter and create nodes.
- [ ] Add choices with auto destination node creation.
- [ ] Edit node title/speaker/text and save edits.
- [ ] Set start node and validate graph.
- [ ] Save project, close, reopen, verify graph round-trips.

## Simulation

- [ ] Start simulation from start node.
- [ ] Traverse multiple paths.
- [ ] Verify breadcrumb updates.
- [ ] Verify dead-end state appears correctly.
- [ ] Restart and confirm state resets.

## Backward compatibility

- [ ] Open legacy projects without branching data.
- [ ] Save and reopen; ensure no chapter/character data loss.

## Accessibility

- [ ] Verify icon-only controls expose automation names/tooltips.
- [ ] Verify keyboard-only flow works for node list, inspector, simulator choices.
- [ ] Verify focus order is logical in compact and wide windows.

## Responsive

- [ ] Test at narrow width (< 800px) and wide width.
- [ ] Ensure list/inspector remain usable and map mode remains optional.

## Performance smoke

- [ ] Create a graph with 100+ nodes and choices.
- [ ] Verify list filtering remains responsive.
- [ ] Verify map redraw and drag interactions remain stable.
