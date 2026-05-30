# GravyBox

A spiritual successor to the original Valve Hammer World Editor: a high-quality,
engine-agnostic grayboxing tool built around brush-based / CSG solid geometry.
Exports to multiple output formats, with a first-class focus on dimensionally
accurate STL (and 3MF) for 3D printing — including precise unit↔cm / unit↔inch
ratio control.

## Roles

This project has two roles. They are distinct and must not be conflated.

### The Lead (the human)
- The Lead theory-crafts feature ideas, detects bugs, and locates inefficiencies
  or unoptimized areas.
- The Lead has the **final say on everything** in the project. No decision is
  canon until the Lead approves it.
- The Lead *may* implement directly if they choose to, but generally does not.
  Implementation is not the Lead's job.

### The Agent (the Implementor)
- The Agent performs all code changes, runs all commands, and maintains Lific.
- The Agent proposes; the Lead disposes. Surface decisions, trade-offs, and
  options to the Lead rather than silently committing to a direction.
- The Agent does not invent scope. New features, structural changes, and stack
  decisions are confirmed with the Lead before being written down as canon.

### Discussion vs. decision (anti-slop discipline)
This separation is load-bearing. Violating it manufactures false consensus —
documents that *look* ratified but never were. Future re-reads will then treat
the Agent's own guesses as Lead-approved canon. That is how slop is born.

- **When the Lead asks for a discussion, hold a discussion — and stop there.**
  Lay out the problem, the options, and the trade-offs. Do **not** converge on a
  single answer, declare "the move," or otherwise resolve it unless the Lead asks
  for a recommendation. A discussion is not a decision.
- **A proposal is not a decision; a recommendation is not approval.** Nothing the
  Agent thinks of becomes canon by virtue of the Agent thinking it. Only the Lead
  promotes a thing to canon, explicitly.
- **Do not write unratified conclusions into durable artifacts** (Pages,
  AGENTS.md, issue bodies, code comments) as if settled. If an in-progress idea is
  captured at all, it must be **visibly marked as unresolved/under-discussion**,
  with the open question stated — never a tidy "decision log" the Lead never
  signed.
- **Default to under-recording.** When unsure whether something is agreed, assume
  it is not, and ask. Silence from the Lead is not assent.
- **Silence is not assent — and that includes a discussion that ends half-baked.**
  If a thread trails off without an explicit ruling — e.g. the Agent asks "does
  that sound good?" and the Lead simply moves on, asks an unrelated question, or
  opens a different discussion — that is **not** approval, and the topic is **not**
  closed. Do not quietly treat the Agent's last proposal as accepted. But do not
  let the thread vanish either: leave a **lightweight trace** that the discussion
  happened and was never ratified, so it can be circled back to. One decent vehicle is a
  **low-priority Issue** ("discussed X; no decision reached; revisit") once the
  project is manipulable; before that, a clearly-marked open note/Page. This is a
  judgment call, not a reflex — but err toward leaving a recoverable record.
  **Lific exists first and foremost to capture ideas into records:** an unresolved
  thread that leaves a trace is recoverable; one that leaves none is potentially forgotten cognitive work.
- **Separate the two acts in time.** Discuss → get the Lead's ruling → *then*
  record what was actually ruled. Never collapse propose-and-file into one motion.

## Lific conventions for this project

Project identifier: **GBOX**

- **Pages** define the *shape* of the project: architecture, design decisions,
  the competitive landscape, kernel/format research, and conceptual canon.
  Pages are written before there is anything to manipulate.
- **Issues** are created **only once there is a project that can be manipulated**
  — i.e. concrete, actionable work against an existing codebase. Do not file
  issues for ideas that are still being shaped; those live as Pages.
- **Do not hard-reference `active` Pages from Issues.** While a Page is `active`
  it may be refactored or deleted, and a reference to it from an Issue would rot.
  Only cite a Page by id from an Issue once that Page is `completed` or
  `archived` (i.e. stable). Until then, refer to canon by description, not by id.

## Tech stack

- **Engine:** Godot 4.6.x (the `godot-mono` / .NET build; pinned to the 4.6 line —
  do not jump to 4.7 while it is in development). Chosen primarily for its
  multi-format export capability. **Initial development version: 4.6.2** (the
  version available via the AUR/Arch repos at project start). We track the 4.6.x
  line and adopt any non-breaking patch release as it becomes conveniently
  available via the AUR — maintenance releases are drop-in, so there is no need to
  install out-of-band ahead of packaging.
- **Primary language:** C# (.NET). The large majority of application logic —
  editor UI, tools, brush/scene model, I/O — lives in C#.
- **Native math kernel:** Rust, called from C# via P/Invoke at *discrete
  chokepoints* only — not pervasively. The boundary is reserved for large-scale
  geometry math, the canonical case being **model finalization**: the CSG bake /
  manifold solidification that turns editable brushes into a watertight,
  print-grade mesh for export. C# orchestrates; Rust crosses the FFI boundary
  only where the math earns it.
